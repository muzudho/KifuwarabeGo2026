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

    public ProcessPlayRoomLauncher(
        Func<PlayRoomLaunchRequest, ProcessStartInfo> startInfoFactory,
        string? requestDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(startInfoFactory);
        _startInfoFactory = startInfoFactory;
        _requestDirectory = string.IsNullOrWhiteSpace(requestDirectory)
            ? Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026", "PlayRoomLaunchRequests")
            : Path.GetFullPath(requestDirectory);
    }

    public async Task<PlayRoomProcessCompletionResult> LaunchAsync(
        PlayRoomLaunchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Version != 1)
            return Failed(request.RequestId, "unsupported-launch-version", "Only play-room launch version 1 is supported.");
        if (string.IsNullOrWhiteSpace(request.RequestId))
            return Failed("", "missing-request-id", "A play-room request ID is required.");

        string? requestPath = null;
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

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("The Play Room Host process could not be started.");
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0
                ? new(PlayRoomProcessCompletionStatus.ExitedNormally, request.RequestId, process.ExitCode)
                : new(
                    PlayRoomProcessCompletionStatus.ExitedAbnormally,
                    request.RequestId,
                    process.ExitCode,
                    "play-room-host-exited-abnormally",
                    $"The Play Room Host exited with code {process.ExitCode}.");
        }
        catch (OperationCanceledException)
        {
            return new(
                PlayRoomProcessCompletionStatus.Cancelled,
                request.RequestId,
                ErrorCode: "play-room-host-cancelled",
                Message: "The Play Room Host execution was cancelled.");
        }
        catch (Exception exception)
        {
            return Failed(request.RequestId, "play-room-host-start-failed", exception.Message);
        }
        finally
        {
            if (requestPath is not null)
            {
                try { File.Delete(requestPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static PlayRoomProcessCompletionResult Failed(string requestId, string errorCode, string message) =>
        new(PlayRoomProcessCompletionStatus.StartFailed, requestId, ErrorCode: errorCode, Message: message);
}
