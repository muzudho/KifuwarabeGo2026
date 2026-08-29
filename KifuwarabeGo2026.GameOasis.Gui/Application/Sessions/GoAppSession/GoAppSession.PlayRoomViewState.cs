namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

public sealed partial class GoAppSession
{
    /// <summary>現在の開始後状態を、囲碁Play Room GUI所有の表示状態として切り出します。</summary>
    public GoPlayRoomViewState CreatePlayRoomViewState()
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
        var timelineIndex = activity == GoPlayRoomActivity.Reviewing ? ReviewTimelineIndex : PlayedMoveCount;
        var timelineMaximum = activity == GoPlayRoomActivity.Reviewing ? ReviewTimelineMaximum : PlayedMoveCount;

        return GoPlayRoomViewState.Capture(
            activity,
            BoardSize,
            GetStone,
            CurrentTurn,
            PlayedMoveCount,
            BlackAgehama,
            WhiteAgehama,
            KoPoint,
            Winner,
            GameOverReason,
            timelineIndex,
            timelineMaximum);
    }
}
