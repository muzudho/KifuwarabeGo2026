namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>全モードで共有する盤面、手番、局面履歴の基本状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public GoStone CurrentTurn { get; private set; } = GoStone.Black;
    public int PlayedMoveCount { get; private set; }
    public int NextMoveNumber => PlayedMoveCount + 1;

    private void ClearBoard()
    {
        _matchSession = null;
        _board = new GoBoard(BoardSize);
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
    }

    /// <summary>空の休憩盤へ戻します。</summary>
    public void ReturnToSetup()
    {
        ClearBoard();
        ChangeMode(GoAppModeKind.Resting);
    }

    private void PassTurn() =>
        CurrentTurn = CurrentTurn == GoStone.Black ? GoStone.White : GoStone.Black;

    private void ResetPositionHistory()
    {
        _positionHashes.Clear();
        _positionHashes.Add(_board.CurrentHash);
    }

    private static GoStone OppositeOf(GoStone stone) =>
        stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private static string StoneName(GoStone stone) =>
        stone == GoStone.Black ? "BLACK" : "WHITE";
}
