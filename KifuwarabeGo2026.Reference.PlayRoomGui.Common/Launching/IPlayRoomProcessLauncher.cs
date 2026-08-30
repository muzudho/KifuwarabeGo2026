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
    string? Message = null)
{
    public bool IsNormalExit => Status == PlayRoomProcessCompletionStatus.ExitedNormally;
}

/// <summary>Lobbyが具象Hostを知らずにPlay Roomを別プロセスで実行する境界です。</summary>
public interface IPlayRoomProcessLauncher
{
    Task<PlayRoomProcessCompletionResult> LaunchAsync(
        PlayRoomLaunchRequest request,
        CancellationToken cancellationToken = default);
}
