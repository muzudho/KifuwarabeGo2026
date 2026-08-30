namespace KifuwarabeGo2026.PlayRoom.Launching;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

public enum PlayRoomProcessCompletionStatus
{
    ExitedNormally,
    ExitedAbnormally,
    StartFailed,
    Cancelled,
}

/// <summary>別プロセスのPlay Roomが終了したときにLobbyへ返す結果です。</summary>
public sealed record PlayRoomProcessCompletionResult(
    PlayRoomProcessCompletionStatus Status,
    string RequestId,
    int? ExitCode = null,
    string? ErrorCode = null,
    string? Message = null,
    bool WasReady = false,
    string? Diagnostic = null)
{
    public bool IsNormalExit => Status == PlayRoomProcessCompletionStatus.ExitedNormally;
}

/// <summary>子Hostが起動要求を受理し、画面ループを開始できる状態になった通知です。</summary>
public sealed record PlayRoomProcessReadyNotification(string RequestId, string Code, string Message);

/// <summary>Lobbyが具象Hostを知らずにPlay Roomを別プロセスで実行する境界です。</summary>
public interface IPlayRoomProcessLauncher
{
    Task<PlayRoomProcessCompletionResult> LaunchAsync(
        PlayRoomLaunchRequest request,
        IProgress<PlayRoomProcessReadyNotification>? readyProgress = null,
        CancellationToken cancellationToken = default);
}
