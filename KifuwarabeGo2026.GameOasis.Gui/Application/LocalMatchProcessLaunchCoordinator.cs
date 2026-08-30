namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using System.Threading.Tasks;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;

/// <summary>Lobbyの画面ループを止めずにLocal Match子Hostの完了を受け取ります。</summary>
public sealed class LocalMatchProcessLaunchCoordinator
{
    private readonly IPlayRoomProcessLauncher _launcher;
    private Task<PlayRoomProcessCompletionResult>? _launchTask;

    public LocalMatchProcessLaunchCoordinator(IPlayRoomProcessLauncher launcher)
    {
        ArgumentNullException.ThrowIfNull(launcher);
        _launcher = launcher;
    }

    public bool IsRunning => _launchTask is not null;

    public PlayRoomLaunchResult Start(PlayRoomLaunchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_launchTask is not null)
            return PlayRoomLaunchResult.Rejected(
                request.RequestId,
                "play-room-host-already-running",
                "A Local Match Play Room is already running.");

        _launchTask = _launcher.LaunchAsync(request);
        return PlayRoomLaunchResult.Started(request.RequestId, request.RequestId);
    }

    public bool TryTakeCompletion(out PlayRoomProcessCompletionResult? completion)
    {
        var task = _launchTask;
        if (task is null || !task.IsCompleted)
        {
            completion = null;
            return false;
        }

        _launchTask = null;
        if (task.IsCompletedSuccessfully)
        {
            completion = task.Result;
            return true;
        }

        var exception = task.Exception?.GetBaseException();
        completion = new(
            PlayRoomProcessCompletionStatus.StartFailed,
            "",
            ErrorCode: "play-room-host-task-failed",
            Message: exception?.Message ?? "The Local Match Play Room task failed.");
        return true;
    }
}
