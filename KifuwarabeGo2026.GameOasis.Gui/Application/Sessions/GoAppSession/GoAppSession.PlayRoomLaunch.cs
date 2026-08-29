namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using System;

public sealed partial class GoAppSession
{
    /// <summary>Lobby状態を参照せず、公開起動要求から解釈した囲碁開始Planを適用します。</summary>
    public bool TryApplyPlayRoomLaunchPlan(GoPlayRoomLaunchPlan plan, out string warning)
    {
        ArgumentNullException.ThrowIfNull(plan);
        warning = "";

        BoardSize = plan.BoardSize;
        _currentTournamentRules.BoardSize = plan.BoardSize;
        _currentTournamentRules.Komi = plan.Komi;
        var totalSeconds = checked((int)Math.Min(plan.MainTime.TotalSeconds, 999 * 3600 + 59 * 60 + 59));
        _currentTournamentRules.MainTimeMinutes = totalSeconds / 60;
        _currentTournamentRules.MainTimeSeconds = totalSeconds % 60;
        ClearBoard();
        CurrentTurn = plan.StartingPlayer;
        foreach (var setupStone in plan.SetupStones)
        {
            if (!_board.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"The setup stone at ({setupStone.Point.X},{setupStone.Point.Y}) could not be applied.";
                ClearBoard();
                return false;
            }
        }

        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
        return true;
    }
}
