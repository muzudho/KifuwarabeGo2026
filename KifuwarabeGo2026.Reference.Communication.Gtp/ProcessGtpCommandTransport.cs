namespace KifuwarabeGo2026.Reference.Communication.Gtp;

using System.Diagnostics;
using System.Text;

/// <summary>外部 GTP エンジンを起動するための設定です。</summary>
public sealed record GtpProcessOptions(
    string FileName,
    IReadOnlyList<string>? Arguments = null,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string?>? EnvironmentVariables = null);

/// <summary>外部プロセスの標準入出力を使う GTP トランスポートです。</summary>
public sealed class ProcessGtpCommandTransport : IGtpCommandTransport, IAsyncDisposable
{
    private readonly Process _process;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private readonly Task _standardErrorDrain;
    private bool _disposed;
    private Exception? _terminalError;

    private ProcessGtpCommandTransport(Process process)
    {
        _process = process;
        _standardErrorDrain = DrainStandardErrorAsync(process.StandardError);
    }

    public static ProcessGtpCommandTransport Start(GtpProcessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.FileName))
            throw new ArgumentException("A GTP engine executable is required.", nameof(options));

        var startInfo = new ProcessStartInfo
        {
            FileName = options.FileName,
            WorkingDirectory = options.WorkingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in options.Arguments ?? []) startInfo.ArgumentList.Add(argument);
        foreach (var pair in options.EnvironmentVariables ?? new Dictionary<string, string?>())
            startInfo.Environment[pair.Key] = pair.Value;

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start GTP engine '{options.FileName}'.");
        }
        return new ProcessGtpCommandTransport(process);
    }

    public async ValueTask<GtpCommandResponse> SendAsync(string command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(command) || command.Contains('\r') || command.Contains('\n'))
            throw new ArgumentException("A GTP command must be one non-empty line.", nameof(command));

        await _commandLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_terminalError is not null)
                throw new InvalidOperationException("The GTP transport can no longer be used after an interrupted or malformed exchange.", _terminalError);
            if (_process.HasExited)
                throw new EndOfStreamException($"GTP engine exited with code {_process.ExitCode}.");
            try
            {
                await _process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken);
                await _process.StandardInput.FlushAsync(cancellationToken);
                return await ReadResponseAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or OperationCanceledException)
            {
                _terminalError = exception;
                if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                throw;
            }
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _commandLock.WaitAsync();
        try
        {
            if (_disposed) return;
            _disposed = true;
            if (!_process.HasExited)
            {
                try
                {
                    await _process.StandardInput.WriteLineAsync("quit");
                    await _process.StandardInput.FlushAsync();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _process.WaitForExitAsync(timeout.Token);
                }
                catch (Exception exception) when (exception is IOException or InvalidOperationException or OperationCanceledException)
                {
                    if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                }
            }
            await _standardErrorDrain;
            _process.Dispose();
        }
        finally
        {
            _commandLock.Release();
            _commandLock.Dispose();
        }
    }

    private async ValueTask<GtpCommandResponse> ReadResponseAsync(CancellationToken cancellationToken)
    {
        var firstLine = await _process.StandardOutput.ReadLineAsync(cancellationToken);
        if (firstLine is null)
            throw new EndOfStreamException("GTP engine closed standard output before returning a response.");
        if (firstLine.Length == 0 || firstLine[0] is not ('=' or '?'))
            throw new InvalidDataException($"Invalid GTP response header: '{firstLine}'.");

        var payload = new StringBuilder(firstLine[1..].TrimStart());
        while (true)
        {
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null)
                throw new EndOfStreamException("GTP engine closed standard output inside a response.");
            if (line.Length == 0) break;
            if (payload.Length > 0) payload.Append('\n');
            payload.Append(line);
        }
        return new GtpCommandResponse(firstLine[0] == '=', payload.ToString());
    }

    private static async Task DrainStandardErrorAsync(StreamReader standardError)
    {
        while (await standardError.ReadLineAsync() is not null) { }
    }
}
