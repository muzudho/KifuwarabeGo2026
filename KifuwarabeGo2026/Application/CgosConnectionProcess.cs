namespace KifuwarabeGo2026.Application;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public sealed class CgosConnectionProcess : IDisposable
{
    private readonly object _outputLock = new();
    private readonly Queue<string> _recentOutput = new();
    private readonly HashSet<string> _seenLogLines = new(StringComparer.Ordinal);
    private Process? _process;
    private DateTime _startedAt;
    private string _activeCgosLogPath = "";
    private string _status = "READY";

    public bool IsRunning => _process is { HasExited: false };

    public int? ExitCode => _process is { HasExited: true } process ? process.ExitCode : null;

    public string LogDirectory { get; private set; } = "";

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

    public string Start(CgosConnectionProfile profile, CgosConnectionAccountKind accountKind, GtpEngineProfile engineProfile)
    {
        if (IsRunning)
        {
            return "RUNNING";
        }

        DisposeProcess();
        ClearOutput();
        _seenLogLines.Clear();
        _activeCgosLogPath = "";
        _startedAt = DateTime.Now;
        _status = "STARTING";

        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Communication.Cgos", "KifuwarabeGo2026.Communication.Cgos.csproj");
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException("CGOS communication project was not found.", projectPath);
        }

        LogDirectory = Path.Combine(repositoryRoot, "Logs", "Cgos");
        Directory.CreateDirectory(LogDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(profile.Host);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(profile.Port.ToString());
        if (accountKind == CgosConnectionAccountKind.Both)
        {
            startInfo.ArgumentList.Add("--both");
        }
        else
        {
            startInfo.ArgumentList.Add("--account");
            startInfo.ArgumentList.Add(accountKind == CgosConnectionAccountKind.White ? "white" : "black");
        }
        startInfo.ArgumentList.Add("--log-directory");
        startInfo.ArgumentList.Add(LogDirectory);
        var engineCommand = CreateEngineCommand(engineProfile);
        startInfo.ArgumentList.Add("--engine-command");
        startInfo.ArgumentList.Add(engineCommand);

        _process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };
        _process.OutputDataReceived += (_, e) => AddOutput(e.Data);
        _process.ErrorDataReceived += (_, e) => AddOutput(e.Data);

        if (!_process.Start())
        {
            DisposeProcess();
            throw new InvalidOperationException("Could not start CGOS communication process.");
        }

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        AddOutput($"# Started CGOS communication process. pid={_process.Id}");
        AddOutput("# Engine command: " + engineCommand);
        _status = "STARTING";
        return _status;
    }

    public string RefreshStatus()
    {
        if (_process is null)
        {
            return _status;
        }

        PollCgosLogFiles();
        if (_process.HasExited)
        {
            _status = _process.ExitCode == 0 ? "EXITED 0" : "ERROR";
            return _status;
        }

        _status = DeriveRunningStatus(GetRecentOutput());
        return _status;
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        if (!_process.HasExited)
        {
            try
            {
                _process.StandardInput.WriteLine("quit");
                _process.StandardInput.Flush();
            }
            catch (InvalidOperationException)
            {
            }
            catch (IOException)
            {
            }

            if (!_process.WaitForExit(3000))
            {
                _process.Kill(entireProcessTree: true);
            }
        }

        PollCgosLogFiles();
        AddOutput("# Stop requested.");
        _status = "STOPPED";
        DisposeProcess();
    }

    public string OpenLog(string app)
    {
        PollCgosLogFiles();

        var defaultLogDirectory = Path.Combine(FindRepositoryRoot(), "Logs", "Cgos");
        var logDirectory = string.IsNullOrWhiteSpace(LogDirectory) ? defaultLogDirectory : LogDirectory;
        Directory.CreateDirectory(logDirectory);

        var targetPath = !string.IsNullOrWhiteSpace(_activeCgosLogPath) && File.Exists(_activeCgosLogPath)
            ? _activeCgosLogPath
            : GetLatestCgosLogPath(logDirectory);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            targetPath = logDirectory;
        }

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
        AddOutput($"# Opened CGOS log with {fileName}: {openedName}");
        return "OPENED LOG";
    }

    public void Dispose()
    {
        Stop();
    }

    private void DisposeProcess()
    {
        _process?.Dispose();
        _process = null;
    }

    private void ClearOutput()
    {
        lock (_outputLock)
        {
            _recentOutput.Clear();
        }
    }

    private void PollCgosLogFiles()
    {
        if (string.IsNullOrWhiteSpace(LogDirectory) || !Directory.Exists(LogDirectory))
        {
            return;
        }

        var paths = GetCgosLogFilesToPoll();
        foreach (var path in paths)
        {
            try
            {
                foreach (var line in File.ReadLines(path))
                {
                    var key = path + "\n" + line;
                    if (_seenLogLines.Add(key))
                    {
                        AddOutput(line);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private IReadOnlyList<string> GetCgosLogFilesToPoll()
    {
        if (!string.IsNullOrWhiteSpace(_activeCgosLogPath) && File.Exists(_activeCgosLogPath))
        {
            return [_activeCgosLogPath];
        }

        var candidates = new List<FileInfo>();
        foreach (var path in Directory.EnumerateFiles(LogDirectory, "cgos-*.log"))
        {
            try
            {
                var file = new FileInfo(path);
                if (file.LastWriteTime >= _startedAt.AddSeconds(-10))
                {
                    candidates.Add(file);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
            }
        }

        var latest = candidates
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();
        if (latest is null)
        {
            return [];
        }

        _activeCgosLogPath = latest.FullName;
        AddOutput("# Reading CGOS log: " + latest.Name);
        return [_activeCgosLogPath];
    }

    private static string GetLatestCgosLogPath(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            return "";
        }

        try
        {
            return Directory.EnumerateFiles(logDirectory, "cgos-*.log")
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

    private void AddOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (_outputLock)
        {
            _recentOutput.Enqueue(FormatDisplayLine(line.Trim()));
            while (_recentOutput.Count > 8)
            {
                _recentOutput.Dequeue();
            }
        }
    }

    private static string DeriveRunningStatus(IReadOnlyList<string> output)
    {
        foreach (var line in output.Reverse())
        {
            if (line.Contains("CGOS error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Unsupported CGOS command", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Could not connect", StringComparison.OrdinalIgnoreCase))
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

    private static string FormatDisplayLine(string line)
    {
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
}
