namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

public sealed partial class GoAppSession
{
    /// <summary>現在の開始後状態を、囲碁Play Room GUI所有の表示状態として切り出します。</summary>
    public GoPlayRoomViewState CreatePlayRoomViewState(bool displayedPosition = false)
    {
        var activity = CurrentMode.Kind switch
        {
            GoAppModeKind.Playing => GoPlayRoomActivity.Playing,
            GoAppModeKind.GameOver => GoPlayRoomActivity.GameOver,
            GoAppModeKind.BoardEditing => GoPlayRoomActivity.BoardEditing,
            GoAppModeKind.VariationEditing => GoPlayRoomActivity.VariationEditing,
            GoAppModeKind.Reviewing => GoPlayRoomActivity.Reviewing,
            _ => GoPlayRoomActivity.Resting,
        };
        var timelineIndex = activity switch
        {
            GoPlayRoomActivity.Reviewing => ReviewTimelineIndex,
            GoPlayRoomActivity.GameOver => LocalReviewTimelineIndex,
            GoPlayRoomActivity.Playing => LocalDisplayMoveIndex,
            _ => PlayedMoveCount,
        };
        var timelineMaximum = activity switch
        {
            GoPlayRoomActivity.Reviewing => ReviewTimelineMaximum,
            GoPlayRoomActivity.GameOver => LocalReviewTimelineMaximum,
            GoPlayRoomActivity.Playing => CurrentGameRecord.Moves.Count,
            _ => PlayedMoveCount,
        };
        var showReplayPosition = displayedPosition && IsLocalReplayMode;
        var lastMovePoint = activity == GoPlayRoomActivity.Reviewing
            ? ReviewCurrentMove?.Point
            : LocalDisplayMoveIndex > 0 && LocalDisplayMoveIndex <= CurrentGameRecord.Moves.Count
                ? CurrentGameRecord.Moves[LocalDisplayMoveIndex - 1].Point
                : null;

        return GoPlayRoomViewState.Capture(
            activity,
            BoardSize,
            displayedPosition ? GetDisplayStone : GetStone,
            CurrentTurn,
            PlayedMoveCount,
            BlackAgehama,
            WhiteAgehama,
            showReplayPosition ? null : KoPoint,
            Winner,
            GameOverReason,
            timelineIndex,
            timelineMaximum,
            lastMovePoint);
    }
}
