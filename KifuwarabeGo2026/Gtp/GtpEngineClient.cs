namespace KifuwarabeGo2026.Gtp;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class GtpEngineClient : IAsyncDisposable
{
    private readonly GtpEngineSettings _settings;
    private readonly TimeSpan _commandTimeout;
    private Process? _process;
    private StreamWriter? _logWriter;

    public GtpEngineClient(GtpEngineSettings settings, TimeSpan? commandTimeout = null)
    {
        _settings = settings;
        _commandTimeout = commandTimeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process is not null)
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _settings.ExecutablePath,
            Arguments = _settings.Arguments,
            WorkingDirectory = _settings.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start GTP engine: {_settings.ExecutablePath}");

        if (_settings.EnableGtpLog)
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            var logStream = new FileStream(
                Path.Combine(AppContext.BaseDirectory, "logs", "gtp.log"),
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            _logWriter = new StreamWriter(logStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true,
            };
            await _logWriter.WriteLineAsync(FormatLogLine($"# start {_settings.Name} {DateTimeOffset.Now:O}")).WaitAsync(_commandTimeout, cancellationToken);
        }
    }

    public async Task<GtpResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_process is null)
        {
            throw new InvalidOperationException("GTP engine has not been started.");
        }

        if (_process.HasExited)
        {
            throw new InvalidOperationException($"GTP engine exited with code {_process.ExitCode}.");
        }

        await LogAsync($"-> {command}", cancellationToken);
        await _process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken).WaitAsync(_commandTimeout, cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken).WaitAsync(_commandTimeout, cancellationToken);

        var response = await ReadResponseAsync(_process.StandardOutput, cancellationToken);
        await LogAsync($"<- {(response.IsSuccess ? "=" : "?")} {response.Payload}".TrimEnd(), cancellationToken);
        return response;
    }

    public async Task SendCommandExpectSuccessAsync(string command, CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync(command, cancellationToken);
        response.ThrowIfError(command);
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    await SendCommandAsync("quit");
                }
            }
            catch
            {
                // Shutdown should not hide the original GUI-side failure.
            }

            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
            _process = null;
        }

        if (_logWriter is not null)
        {
            await _logWriter.DisposeAsync();
            _logWriter = null;
        }
    }

    private async Task<GtpResponse> ReadResponseAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        string? firstLine = null;
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).AsTask().WaitAsync(_commandTimeout, cancellationToken);
            if (line is null)
            {
                throw new EndOfStreamException("GTP engine closed stdout.");
            }

            if (line.Length == 0 && firstLine is null)
            {
                continue;
            }

            if (line.Length == 0)
            {
                break;
            }

            firstLine ??= line;
        }

        if (firstLine is null)
        {
            throw new InvalidOperationException("GTP engine returned an empty response.");
        }

        var success = firstLine.StartsWith("=", StringComparison.Ordinal);
        if (!success && !firstLine.StartsWith("?", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid GTP response: {firstLine}");
        }

        return new GtpResponse(success, firstLine[1..].Trim());
    }

    private async Task LogAsync(string line, CancellationToken cancellationToken)
    {
        if (_logWriter is not null)
        {
            await _logWriter.WriteLineAsync(FormatLogLine(line).AsMemory(), cancellationToken).WaitAsync(_commandTimeout, cancellationToken);
        }
    }

    private string FormatLogLine(string line) => string.IsNullOrWhiteSpace(_settings.LogPrefix)
        ? line
        : $"{_settings.LogPrefix} {line}";
}
