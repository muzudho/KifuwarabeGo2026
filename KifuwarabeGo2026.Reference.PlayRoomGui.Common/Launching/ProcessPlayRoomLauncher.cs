namespace KifuwarabeGo2026.PlayRoom.Launching;

using System.Diagnostics;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

/// <summary>保存した起動要求をコマンドラインで子Play Room Hostへ渡します。</summary>
public sealed class ProcessPlayRoomLauncher : IPlayRoomProcessLauncher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Func<PlayRoomLaunchRequest, ProcessStartInfo> _startInfoFactory;
    private readonly string _requestDirectory;
    private readonly TimeSpan _readyTimeout;

    public ProcessPlayRoomLauncher(
        Func<PlayRoomLaunchRequest, ProcessStartInfo> startInfoFactory,
        string? requestDirectory = null,
        TimeSpan? readyTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(startInfoFactory);
        _startInfoFactory = startInfoFactory;
        _requestDirectory = string.IsNullOrWhiteSpace(requestDirectory)
            ? Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026", "PlayRoomLaunchRequests")
            : Path.GetFullPath(requestDirectory);
        _readyTimeout = readyTimeout ?? TimeSpan.FromSeconds(10);
        if (_readyTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(readyTimeout));
    }

    public async Task<PlayRoomProcessCompletionResult> LaunchAsync(
        PlayRoomLaunchRequest request,
        IProgress<PlayRoomProcessReadyNotification>? readyProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Version != 1)
            return Failed(request.RequestId, "unsupported-launch-version", "Only play-room launch version 1 is supported.");
        if (string.IsNullOrWhiteSpace(request.RequestId))
            return Failed("", "missing-request-id", "A play-room request ID is required.");

        string? requestPath = null;
        Process? process = null;
        Task<string>? standardErrorTask = null;
        var wasReady = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_requestDirectory);
            requestPath = Path.Combine(_requestDirectory, $"{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(
                requestPath,
                JsonSerializer.Serialize(request, JsonOptions),
                cancellationToken).ConfigureAwait(false);

            var startInfo = _startInfoFactory(request)
                ?? throw new InvalidOperationException("The Play Room Host start information factory returned null.");
            startInfo.ArgumentList.Add("--launch-request");
            startInfo.ArgumentList.Add(requestPath);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The Play Room Host process could not be started.");
            standardErrorTask = process.StandardError.ReadToEndAsync();

            string? readyLine;
            using (var readyCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                readyCancellation.CancelAfter(_readyTimeout);
                try
                {
                    readyLine = await process.StandardOutput.ReadLineAsync(readyCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    TryKill(process);
                    await process.WaitForExitAsync().ConfigureAwait(false);
                    return Failed(
                        request.RequestId,
                        "play-room-host-ready-timeout",
                        $"The Play Room Host did not report readiness within {_readyTimeout.TotalSeconds:0.###} seconds.",
                        diagnostic: await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
                }
            }

            if (readyLine is null)
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return new(
                    PlayRoomProcessCompletionStatus.ExitedAbnormally,
                    request.RequestId,
                    process.ExitCode,
                    "play-room-host-exited-before-ready",
                    $"The Play Room Host exited with code {process.ExitCode} before reporting readiness.",
                    Diagnostic: await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
            }

            HostReadyMessage? ready;
            try { ready = JsonSerializer.Deserialize<HostReadyMessage>(readyLine, JsonOptions); }
            catch (JsonException exception)
            {
                TryKill(process);
                await process.WaitForExitAsync().ConfigureAwait(false);
                return Failed(
                    request.RequestId,
                    "invalid-play-room-host-ready",
                    exception.Message,
                    diagnostic: await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
            }
            if (ready is not { Ready: true } ||
                !string.Equals(ready.RequestId, request.RequestId, StringComparison.Ordinal))
            {
                TryKill(process);
                await process.WaitForExitAsync().ConfigureAwait(false);
                return Failed(
                    request.RequestId,
                    "invalid-play-room-host-ready",
                    "The Play Room Host returned an invalid or mismatched readiness notification.",
                    diagnostic: await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
            }

            wasReady = true;
            readyProgress?.Report(new(request.RequestId, ready.Code ?? "ready", ready.Message ?? "The Play Room Host is ready."));
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false);
            return process.ExitCode == 0
                ? new(PlayRoomProcessCompletionStatus.ExitedNormally, request.RequestId, process.ExitCode, WasReady: true, Diagnostic: diagnostic)
                : new(
                    PlayRoomProcessCompletionStatus.ExitedAbnormally,
                    request.RequestId,
                    process.ExitCode,
                    "play-room-host-exited-abnormally",
                    $"The Play Room Host exited with code {process.ExitCode} after reporting readiness.",
                    WasReady: true,
                    Diagnostic: diagnostic);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new(
                PlayRoomProcessCompletionStatus.Cancelled,
                request.RequestId,
                ErrorCode: "play-room-host-cancelled",
                Message: "The Play Room Host execution was cancelled.",
                WasReady: wasReady,
                Diagnostic: await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            TryKill(process);
            if (process is not null)
            {
                try { await process.WaitForExitAsync().ConfigureAwait(false); }
                catch (InvalidOperationException) { }
            }
            return Failed(
                request.RequestId,
                "play-room-host-start-failed",
                exception.Message,
                wasReady,
                await ReadDiagnosticAsync(standardErrorTask).ConfigureAwait(false));
        }
        finally
        {
            process?.Dispose();
            if (requestPath is not null)
            {
                try { File.Delete(requestPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static PlayRoomProcessCompletionResult Failed(
        string requestId,
        string errorCode,
        string message,
        bool wasReady = false,
        string? diagnostic = null) =>
        new(
            PlayRoomProcessCompletionStatus.StartFailed,
            requestId,
            ErrorCode: errorCode,
            Message: message,
            WasReady: wasReady,
            Diagnostic: diagnostic);

    private static async Task<string?> ReadDiagnosticAsync(Task<string>? standardErrorTask)
    {
        if (standardErrorTask is null) return null;
        var diagnostic = (await standardErrorTask.ConfigureAwait(false)).Trim();
        if (diagnostic.Length == 0) return null;
        const int maximumLength = 4000;
        return diagnostic.Length <= maximumLength ? diagnostic : diagnostic[..maximumLength] + "…";
    }

    private static void TryKill(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }

    private sealed record HostReadyMessage(bool Ready, string? Code, string? Message, string? RequestId);
}
