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
    private Process? _process;

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
        AddOutput($"Started CGOS communication process. pid={_process.Id}");
        AddOutput("Engine command: " + engineCommand);
        return "RUNNING";
    }

    public string RefreshStatus()
    {
        if (_process is null)
        {
            return "READY";
        }

        return _process.HasExited ? $"EXITED {_process.ExitCode}" : "RUNNING";
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

        DisposeProcess();
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

    private void AddOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (_outputLock)
        {
            _recentOutput.Enqueue(line.Trim());
            while (_recentOutput.Count > 8)
            {
                _recentOutput.Dequeue();
            }
        }
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
