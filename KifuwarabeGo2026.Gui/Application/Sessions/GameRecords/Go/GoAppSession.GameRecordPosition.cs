namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>棋譜を初期局面として盤面へ適用し、休憩状態へ戻します。</summary>
public sealed partial class GoAppSession
{
    public bool LoadGameRecordAsInitialPosition(GoGameRecord record, out string warning)
    {
        ArgumentNullException.ThrowIfNull(record);

        var loadedBoard = new GoBoard(record.BoardSize);
        foreach (var setupStone in record.SetupStones)
        {
            if (!loadedBoard.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"Invalid SGF setup stone at {setupStone.Point.X + 1},{setupStone.Point.Y + 1}.";
                return false;
            }
        }

        GoPoint? replayKoPoint = null;
        foreach (var move in record.Moves)
        {
            if (move.Point is not { } point)
            {
                replayKoPoint = null;
                continue;
            }

            if (!loadedBoard.TryPlaceStone(point.X, point.Y, move.Stone, replayKoPoint, out _, out var nextKoPoint))
            {
                warning = $"Illegal SGF move at {point.X + 1},{point.Y + 1}.";
                return false;
            }

            replayKoPoint = nextKoPoint;
        }

        BoardSize = record.BoardSize;
        _currentTournamentRules.BoardSize = BoardSize;
        _currentTournamentRules.Komi = record.Komi;
        TournamentRulesSaveMessage = "UNSAVED";
        _board = loadedBoard;
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
        _reviewGameRecord = null;
        ReviewMoveIndex = 0;
        ChangeMode(GoAppModeKind.Resting);
        warning = "";
        return true;
    }
}
