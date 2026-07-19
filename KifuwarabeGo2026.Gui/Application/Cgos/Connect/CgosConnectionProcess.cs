namespace KifuwarabeGo2026.Gui.Application.Cgos.Connect;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// ［ＣＧＯＳへの接続画面］の処理
/// </summary>
public sealed class CgosConnectionProcess : IDisposable
{
    private readonly string _logFolderName;
    private readonly object _outputLock = new();
    private readonly Queue<string> _recentOutput = new();
    private readonly Queue<string> _pendingOutput = new();
    private readonly List<string> _adminWaitingPlayers = new();
    private readonly List<Process> _processes = new();
    private DateTime _startedAt;
    private string _guiLogPath = "";
    private string _standardErrorLogPath = "";
    private string _status = "READY";

    public bool IsRunning => _processes.Any(process => !process.HasExited);

    public string LogDirectory { get; private set; } = "";

    public CgosConnectionProcess(string logFolderName = "")
    {
        _logFolderName = logFolderName.Trim();
    }

    public string LatestOutput
    {
        get
        {
            lock (_outputLock)
            {
                return _recentOutput.LastOrDefault() ?? "";
            }
        }
    }

    public IReadOnlyList<string> GetRecentOutput()
    {
        lock (_outputLock)
        {
            return _recentOutput.ToArray();
        }
    }

    /// <summary>
    /// GUI がまだ処理していない通信出力を取り出します。
    /// </summary>
    public IReadOnlyList<string> DrainOutput()
    {
        lock (_outputLock)
        {
            var output = _pendingOutput.ToArray();
            _pendingOutput.Clear();
            return output;
        }
    }

    public IReadOnlyList<string> GetAdminWaitingPlayers()
    {
        lock (_outputLock)
        {
            return _adminWaitingPlayers.ToArray();
        }
    }

    public string Start(CgosConnectionProfile profile, GtpEngineProfile? blackEngineProfile, GtpEngineProfile? whiteEngineProfile)
    {
        if (IsRunning)
        {
            return "RUNNING";
        }

        if (blackEngineProfile is null && whiteEngineProfile is null)
        {
            throw new InvalidOperationException("Select at least one CGOS engine.");
        }

        DisposeProcess();
        ClearOutput();
        _guiLogPath = "";
        _standardErrorLogPath = "";
        _startedAt = DateTime.Now;
        _status = "STARTING";

        var repositoryRoot = FindRepositoryRoot();
        var executablePath = GetCgosCommunicationExecutablePath(repositoryRoot);
        var runLabel = blackEngineProfile is not null && whiteEngineProfile is null
            ? "BlackPlayer"
            : whiteEngineProfile is not null && blackEngineProfile is null
                ? "WhitePlayer"
                : "Players";
        LogDirectory = Path.Combine(repositoryRoot, "Logs", "Cgos", runLabel);
        Directory.CreateDirectory(LogDirectory);
        _guiLogPath = Path.Combine(LogDirectory, $"gui-{runLabel.ToLowerInvariant()}-{_startedAt:yyyyMMdd-HHmmss}.log");
        _standardErrorLogPath = Path.Combine(LogDirectory, $"standard-error-{runLabel.ToLowerInvariant()}-{_startedAt:yyyyMMdd-HHmmss}.log");
        ValidateCgosCommunicationExecutable(executablePath);

        try
        {
            if (blackEngineProfile is not null)
            {
                StartProcess(profile, "black", blackEngineProfile, repositoryRoot, executablePath);
            }

            if (whiteEngineProfile is not null)
            {
                StartProcess(profile, "white", whiteEngineProfile, repositoryRoot, executablePath);
            }
        }
        catch
        {
            Stop();
            throw;
        }

        _status = "STARTING";
        return _status;
    }

    public string StartAdmin(CgosConnectionProfile profile)
    {
        if (IsRunning)
        {
            return "RUNNING";
        }

        DisposeProcess();
        ClearOutput();
        _guiLogPath = "";
        _standardErrorLogPath = "";
        _startedAt = DateTime.Now;
        _status = "ADMIN STARTING";

        var repositoryRoot = FindRepositoryRoot();
        var executablePath = GetCgosCommunicationExecutablePath(repositoryRoot);

        LogDirectory = Path.Combine(repositoryRoot, "Logs", "Cgos", "Admin");
        Directory.CreateDirectory(LogDirectory);
        _guiLogPath = Path.Combine(LogDirectory, $"gui-admin-{_startedAt:yyyyMMdd-HHmmss}.log");
        _standardErrorLogPath = Path.Combine(LogDirectory, $"standard-error-admin-{_startedAt:yyyyMMdd-HHmmss}.log");
        ValidateCgosCommunicationExecutable(executablePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(profile.Host);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(profile.Port.ToString());
        startInfo.ArgumentList.Add("--admin");
        startInfo.ArgumentList.Add("--log-directory");
        startInfo.ArgumentList.Add(LogDirectory);
        AddParentProcessArguments(startInfo);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += (_, e) => AddOutput(e.Data);
        process.ErrorDataReceived += (_, e) => AddOutput(e.Data, isError: true);

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Could not start CGOS admin process.");
        }

        _processes.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        AddOutput($"# Started CGOS admin process. pid={process.Id}");
        AddOutput("# Communication executable: " + executablePath);
        return _status;
    }

    public string SendCommand(string command)
    {
        if (!IsRunning)
        {
            return "ADMIN STOPPED";
        }

        var process = _processes.FirstOrDefault(process => !process.HasExited);
        if (process is null)
        {
            return "ADMIN STOPPED";
        }

        if (command.Trim().Equals("who", StringComparison.OrdinalIgnoreCase))
        {
            lock (_outputLock)
            {
                _adminWaitingPlayers.Clear();
            }
        }

        process.StandardInput.WriteLine(command);
        process.StandardInput.Flush();
        AddOutput("# Sent admin command: " + command);
        return "SENT " + command.ToUpperInvariant();
    }

    private void StartProcess(CgosConnectionProfile profile, string account, GtpEngineProfile engineProfile, string repositoryRoot, string executablePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(profile.Host);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(profile.Port.ToString());
        startInfo.ArgumentList.Add("--account");
        startInfo.ArgumentList.Add(account);
        startInfo.ArgumentList.Add("--log-directory");
        startInfo.ArgumentList.Add(LogDirectory);
        var engineCommand = CreateEngineCommand(engineProfile);
        startInfo.ArgumentList.Add("--engine-command");
        startInfo.ArgumentList.Add(engineCommand);
        foreach (var option in engineProfile.GuiOptions)
        {
            startInfo.ArgumentList.Add("--engine-option");
            startInfo.ArgumentList.Add($"{option.Key}={option.Value}");
        }
        AddParentProcessArguments(startInfo);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        process.OutputDataReceived += (_, e) => AddOutput(e.Data);
        process.ErrorDataReceived += (_, e) => AddOutput(e.Data, isError: true);

        if (!process.Start())
        {
            process.Dispose();
            DisposeProcess();
            throw new InvalidOperationException("Could not start CGOS communication process.");
        }

        _processes.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        AddOutput($"# Started CGOS {account} communication process. pid={process.Id}");
        AddOutput("# Communication executable: " + executablePath);
        AddOutput("# Engine command: " + engineCommand);
    }

    public string RefreshStatus()
    {
        if (_processes.Count == 0)
        {
            return _status;
        }

        if (_processes.All(process => process.HasExited))
        {
            _status = _processes.All(process => process.ExitCode == 0) ? "EXITED 0" : "ERROR";
            return _status;
        }

        _status = DeriveRunningStatus(GetRecentOutput());
        return _status;
    }

    public void Stop()
    {
        if (_processes.Count == 0)
        {
            return;
        }

        foreach (var process in _processes)
        {
            if (process.HasExited)
            {
                continue;
            }

            try
            {
                process.StandardInput.WriteLine("quit");
                process.StandardInput.Flush();
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }

            if (!process.WaitForExit(3000))
            {
                process.Kill(entireProcessTree: true);
            }
        }

        AddOutput("# Stop requested.");
        _status = "STOPPED";
        DisposeProcess();
    }

    public string OpenLog(string app, bool openStandardError)
    {
        var targetPath = GetLogTargetPath(openStandardError, allowGuiFallback: !openStandardError);

        var opensFile = File.Exists(targetPath);
        var fileName = opensFile
            ? string.IsNullOrWhiteSpace(app) ? "notepad" : app.Trim()
            : "explorer";
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add(targetPath);
        Process.Start(startInfo);

        var openedName = File.Exists(targetPath) ? Path.GetFileName(targetPath) : targetPath;
        AddOutput($"# Opened {(openStandardError ? "standard error" : "CGOS")} log with {fileName}: {openedName}");
        return openStandardError ? "OPENED STDERR LOG" : "OPENED LOG";
    }

    public string TailLogWithPowerShell(bool openStandardError)
    {
        var targetPath = GetLogTargetPath(openStandardError, allowGuiFallback: true);
        if (!File.Exists(targetPath))
        {
            targetPath = EnsureGuiLogFile();
        }

        var escapedPath = targetPath.Replace("'", "''", StringComparison.Ordinal);
        var command = $"$Host.UI.RawUI.WindowTitle = 'CGOS log tail'; Get-Content -LiteralPath '{escapedPath}' -Wait";
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add("-NoExit");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);
        Process.Start(startInfo);

        AddOutput("# Tailing CGOS log with PowerShell: " + Path.GetFileName(targetPath));
        return openStandardError ? "TAIL STDERR LOG" : "TAIL LOG";
    }

    public void Dispose()
    {
        Stop();
    }

    private void DisposeProcess()
    {
        foreach (var process in _processes)
        {
            process.Dispose();
        }

        _processes.Clear();
    }

    /// <summary>
    /// 出力のクリアー
    /// </summary>
    private void ClearOutput()
    {
        lock (_outputLock)
        {
            _recentOutput.Clear();
            _pendingOutput.Clear();
            _adminWaitingPlayers.Clear();
        }
    }

    private static string GetLatestCgosLogPath(string logDirectory)
    {
        return GetCgosLogPaths(logDirectory).FirstOrDefault() ?? "";
    }

    private static IReadOnlyList<string> GetCgosLogPaths(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(logDirectory, "cgos-*.log")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTime)
                .Take(4)
                .Select(file => file.FullName)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return [];
        }
    }

    private string GetStandardErrorLogPath(string logDirectory)
    {
        if (!string.IsNullOrWhiteSpace(_standardErrorLogPath) && File.Exists(_standardErrorLogPath))
        {
            return _standardErrorLogPath;
        }

        return GetLatestStandardErrorLogPath(logDirectory);
    }

    private static string GetLatestStandardErrorLogPath(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            return "";
        }

        try
        {
            return Directory.EnumerateFiles(logDirectory, "standard-error-*.log")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault()
                ?.FullName ?? "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return "";
        }
    }

    /// <summary>
    /// 出力の追加
    /// </summary>
    /// <param name="line"></param>
    /// <param name="isError"></param>
    private void AddOutput(string? line, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var displayLine = FormatDisplayLine(line.Trim(), isError);
        lock (_outputLock)
        {
            TryAddAdminWaitingPlayer(displayLine);
            if (isError)
            {
                AppendStandardErrorLog(displayLine);
            }

            AppendGuiLog(displayLine);
            _recentOutput.Enqueue(displayLine);
            _pendingOutput.Enqueue(displayLine);
            while (_recentOutput.Count > 8)
            {
                _recentOutput.Dequeue();
            }
        }
    }

    private void TryAddAdminWaitingPlayer(string displayLine)
    {
        var messageStart = displayLine.IndexOf("] > ", StringComparison.Ordinal);
        if (messageStart < 0)
        {
            return;
        }

        var parts = displayLine[(messageStart + 4)..]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || !parts[1].Equals("waiting", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_adminWaitingPlayers.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
        {
            _adminWaitingPlayers.Add(parts[0]);
        }
    }

    private string EnsureGuiLogFile()
    {
        if (string.IsNullOrWhiteSpace(_guiLogPath))
        {
            Directory.CreateDirectory(LogDirectory);
            var label = string.IsNullOrWhiteSpace(_logFolderName) ? "cgos" : _logFolderName.ToLowerInvariant();
            _guiLogPath = Path.Combine(LogDirectory, $"gui-{label}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        }

        AppendGuiLog("# Log file opened before a CGOS communication log was available.");
        return _guiLogPath;
    }

    private string GetLogTargetPath(bool openStandardError, bool allowGuiFallback)
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            LogDirectory = GetDefaultLogDirectory();
        }

        Directory.CreateDirectory(LogDirectory);

        var targetPath = openStandardError
            ? GetStandardErrorLogPath(LogDirectory)
            : GetCurrentRunCgosLogPath(LogDirectory);

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            targetPath = openStandardError
                ? GetLatestStandardErrorLogPath(LogDirectory)
                : GetLatestCgosLogPath(LogDirectory);
        }

        if (string.IsNullOrWhiteSpace(targetPath) && allowGuiFallback)
        {
            targetPath = EnsureGuiLogFile();
        }

        return string.IsNullOrWhiteSpace(targetPath) ? LogDirectory : targetPath;
    }

    private string GetCurrentRunCgosLogPath(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            return "";
        }

        try
        {
            return Directory.EnumerateFiles(logDirectory, "cgos-*.log")
                .Select(path => new FileInfo(path))
                .Where(file => _startedAt == default || file.LastWriteTime >= _startedAt.AddSeconds(-1))
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault()
                ?.FullName ?? "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return "";
        }
    }

    private void AppendGuiLog(string displayLine)
    {
        if (string.IsNullOrWhiteSpace(_guiLogPath))
        {
            return;
        }

        try
        {
            File.AppendAllText(
                _guiLogPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {displayLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void AppendStandardErrorLog(string displayLine)
    {
        if (string.IsNullOrWhiteSpace(_standardErrorLogPath))
        {
            return;
        }

        try
        {
            File.AppendAllText(
                _standardErrorLogPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {displayLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string DeriveRunningStatus(IReadOnlyList<string> output)
    {
        foreach (var line in output.Reverse())
        {
            if (line.Contains("CGOS error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("[StandardError]", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("[Error]", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Unsupported CGOS command", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Could not connect", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TCP connect timed out", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TCP connect failed", StringComparison.OrdinalIgnoreCase))
            {
                return "ERROR";
            }

            if (line.Contains("CGOS connection closed", StringComparison.OrdinalIgnoreCase))
            {
                return "CLOSED";
            }

            if (line.Contains("Generated ", StringComparison.OrdinalIgnoreCase))
            {
                return "GENMOVE DONE";
            }

            if (line.Contains("> genmove", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< genmove", StringComparison.OrdinalIgnoreCase))
            {
                return "GENMOVE";
            }

            if (line.Contains("> play", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< play", StringComparison.OrdinalIgnoreCase))
            {
                return "PLAY";
            }

            if (line.Contains("Setup game", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("> setup", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< setup", StringComparison.OrdinalIgnoreCase))
            {
                return "SETUP";
            }

            if (line.Contains("Game over", StringComparison.OrdinalIgnoreCase))
            {
                return "GAME OVER";
            }

            if (line.Contains("> username", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("> password", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< (password)", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< username", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< password", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("> (password)", StringComparison.OrdinalIgnoreCase))
            {
                return "LOGIN";
            }

            if (line.Contains("Admin command input is ready", StringComparison.OrdinalIgnoreCase))
            {
                return "ADMIN READY";
            }

            if (line.Contains("Sent admin command", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< who", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< match", StringComparison.OrdinalIgnoreCase))
            {
                return "ADMIN COMMAND";
            }

            if (line.Contains("> protocol", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("< protocol", StringComparison.OrdinalIgnoreCase))
            {
                return "PROTOCOL";
            }

            if (line.Contains("Connecting to", StringComparison.OrdinalIgnoreCase))
            {
                return "CONNECTING";
            }

            if (line.Contains("Started CGOS communication process", StringComparison.OrdinalIgnoreCase))
            {
                return "STARTING";
            }
        }

        return "RUNNING";
    }

    private static string FormatDisplayLine(string line, bool isError)
    {
        if (isError)
        {
            return "# [StandardError] " + StripDisplayPrefix(line);
        }

        var messageStart = line.IndexOf("] ", StringComparison.Ordinal);
        if (messageStart >= 0 && messageStart + 2 < line.Length)
        {
            var prefix = line[..(messageStart + 2)];
            var message = line[(messageStart + 2)..];
            if (message.StartsWith("# ", StringComparison.Ordinal) ||
                message.StartsWith("< ", StringComparison.Ordinal) ||
                message.StartsWith("> ", StringComparison.Ordinal))
            {
                return prefix + message;
            }

            return prefix + "# " + message;
        }

        if (line.StartsWith("# ", StringComparison.Ordinal) ||
            line.StartsWith("< ", StringComparison.Ordinal) ||
            line.StartsWith("> ", StringComparison.Ordinal))
        {
            return line;
        }

        return "# " + line;
    }

    private static string StripDisplayPrefix(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("# [StandardError] ", StringComparison.Ordinal))
        {
            return trimmed["# [StandardError] ".Length..];
        }

        if (trimmed.StartsWith("# [Error] ", StringComparison.Ordinal))
        {
            return trimmed["# [Error] ".Length..];
        }

        if (trimmed.StartsWith("# ", StringComparison.Ordinal) ||
            trimmed.StartsWith("< ", StringComparison.Ordinal) ||
            trimmed.StartsWith("> ", StringComparison.Ordinal))
        {
            return trimmed[2..].TrimStart();
        }

        return trimmed;
    }

    private static string CreateEngineCommand(GtpEngineProfile profile)
    {
        var command = QuoteCommandPart(profile.ExecutablePath);
        if (!string.IsNullOrWhiteSpace(profile.Arguments))
        {
            command += " " + profile.Arguments.Trim();
        }

        return command;
    }

    private static string QuoteCommandPart(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "\"\"";
        }

        var trimmed = text.Trim();
        return trimmed.Contains(' ') || trimmed.Contains('\t')
            ? "\"" + trimmed.Replace("\"", "\"\"") + "\""
            : trimmed;
    }

    private void ValidateCgosCommunicationExecutable(string executablePath)
    {
        var directory = Path.GetDirectoryName(executablePath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(executablePath);
        var requiredPaths = new[]
        {
            executablePath,
            Path.Combine(directory, fileNameWithoutExtension + ".dll"),
            Path.Combine(directory, fileNameWithoutExtension + ".deps.json"),
            Path.Combine(directory, fileNameWithoutExtension + ".runtimeconfig.json"),
        };

        var missingPaths = requiredPaths
            .Where(path => !File.Exists(path))
            .ToArray();
        if (missingPaths.Length == 0)
        {
            return;
        }

        AddOutput("# CGOS communication executable is incomplete.");
        foreach (var missingPath in missingPaths)
        {
            AddOutput("# Missing CGOS communication runtime file: " + missingPath);
        }

        throw new FileNotFoundException(
            "CGOS communication executable is incomplete. Build KifuwarabeGo2026.Gui.Communication.Cgos first. Missing: " +
            string.Join(", ", missingPaths.Select(Path.GetFileName)),
            missingPaths[0]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private string GetDefaultLogDirectory()
    {
        var baseDirectory = Path.Combine(FindRepositoryRoot(), "Logs", "Cgos");
        return string.IsNullOrWhiteSpace(_logFolderName)
            ? baseDirectory
            : Path.Combine(baseDirectory, _logFolderName);
    }

    private static string GetCgosCommunicationExecutablePath(string repositoryRoot)
    {
        var bundledExecutablePath = Path.Combine(
            AppContext.BaseDirectory,
            "Tools",
            "Cgos",
            "KifuwarabeGo2026.Gui.Communication.Cgos.exe");
        if (File.Exists(bundledExecutablePath))
        {
            return bundledExecutablePath;
        }

#if DEBUG
        const string buildConfiguration = "Debug";
#else
        const string buildConfiguration = "Release";
#endif
        return Path.Combine(
            repositoryRoot,
            "KifuwarabeGo2026.Gui.Communication.Cgos",
            "bin",
            buildConfiguration,
            "net8.0",
            "KifuwarabeGo2026.Gui.Communication.Cgos.exe");
    }

    private static void AddParentProcessArguments(ProcessStartInfo startInfo)
    {
        using var currentProcess = Process.GetCurrentProcess();
        startInfo.ArgumentList.Add("--parent-process-id");
        startInfo.ArgumentList.Add(currentProcess.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--parent-process-start-time");
        startInfo.ArgumentList.Add(currentProcess.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
