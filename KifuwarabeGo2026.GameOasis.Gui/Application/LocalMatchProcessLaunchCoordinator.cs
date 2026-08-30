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
    private PlayRoomProcessReadyNotification? _ready;

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

        _ready = null;
        _launchTask = _launcher.LaunchAsync(request, new ReadyProgress(this));
        return PlayRoomLaunchResult.Deferred(request.RequestId, "The Local Match Play Room is starting.");
    }

    public bool TryTakeReady(out PlayRoomProcessReadyNotification? ready)
    {
        lock (this)
        {
            ready = _ready;
            _ready = null;
            return ready is not null;
        }
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

    private sealed class ReadyProgress(LocalMatchProcessLaunchCoordinator owner) : IProgress<PlayRoomProcessReadyNotification>
    {
        public void Report(PlayRoomProcessReadyNotification value)
        {
            lock (owner) owner._ready = value;
        }
    }
}
