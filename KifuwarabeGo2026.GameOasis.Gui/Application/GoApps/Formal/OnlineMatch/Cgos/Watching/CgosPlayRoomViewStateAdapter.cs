namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using System;

/// <summary>CGOS観戦状態を囲碁Play Room GUIの表示境界へ投影します。</summary>
public static class CgosPlayRoomViewStateAdapter
{
    public static GoPlayRoomViewState Create(CgosGameObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var activity = observation.IsReplayMode
            ? GoPlayRoomActivity.Reviewing
            : observation.IsFinished
                ? GoPlayRoomActivity.GameOver
                : observation.IsStarted
                    ? GoPlayRoomActivity.Playing
                    : GoPlayRoomActivity.Resting;
        var displayMoveIndex = observation.DisplayMoveIndex;
        var lastMovePoint = displayMoveIndex > 0 && displayMoveIndex <= observation.Moves.Count
            ? observation.Moves[displayMoveIndex - 1].Point
            : null;

        return GoPlayRoomViewState.Capture(
            activity,
            observation.BoardSize,
            observation.GetStone,
            observation.CurrentTurn,
            observation.MoveCount,
            observation.BlackAgehama,
            observation.WhiteAgehama,
            observation.IsReplayMode ? null : observation.KoPoint,
            null,
            observation.IsFinished ? observation.Result : "",
            displayMoveIndex,
            observation.MoveCount,
            lastMovePoint);
    }
}
