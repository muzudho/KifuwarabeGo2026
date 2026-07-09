namespace KifuwarabeGo2026.Application;

using KifuwarabeGo2026.Domain;
using System;
using System.Collections.Generic;

public sealed class GoAppSession
{
    private GoBoard _board;

    private readonly Dictionary<GoAppModeKind, GoAppMode> _modes = new()
    {
        [GoAppModeKind.Playing] = new PlayingMode(),
        [GoAppModeKind.GameOver] = new GameOverMode(),
        [GoAppModeKind.BoardEditing] = new BoardEditingMode(),
        [GoAppModeKind.Reviewing] = new ReviewingMode(),
        [GoAppModeKind.Resting] = new RestingMode(),
    };

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
    }

    public GoAppMode CurrentMode { get; private set; }

    public int BoardSize { get; private set; } = 19;

    public GoStone CurrentTurn { get; private set; } = GoStone.Black;

    public int BlackAgehama { get; private set; }

    public int WhiteAgehama { get; private set; }

    public GoPoint? KoPoint { get; private set; }

    public int ConsecutivePasses { get; private set; }

    public void ChangeMode(GoAppModeKind modeKind)
    {
        CurrentMode = _modes[modeKind];
    }

    public void StartPlaying()
    {
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            ClearBoard();
        }

        ChangeMode(GoAppModeKind.Playing);
    }

    public void ChangeBoardSize(int boardSize)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }

        if (BoardSize == boardSize)
        {
            return;
        }

        BoardSize = boardSize;
        ClearBoard();
    }

    public GoStone GetStone(int x, int y)
    {
        return _board.GetStone(x, y);
    }

    /// <summary>
    /// 石を置けるか試すぜ（＾▽＾）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool TryPlaceStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || !_board.TryPlaceStone(x, y, CurrentTurn, KoPoint, out var capturedStones, out var nextKoPoint))
        {
            return false;
        }

        if (CurrentTurn == GoStone.Black)
        {
            BlackAgehama += capturedStones;
        }
        else
        {
            WhiteAgehama += capturedStones;
        }

        KoPoint = nextKoPoint;
        ConsecutivePasses = 0;
        PassTurn();
        return true;
    }

    public bool Pass()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        KoPoint = null;
        ConsecutivePasses++;
        PassTurn();
        if (ConsecutivePasses >= 2)
        {
            ChangeMode(GoAppModeKind.GameOver);
        }

        return true;
    }

    private void ClearBoard()
    {
        _board = new GoBoard(BoardSize);
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        KoPoint = null;
        ConsecutivePasses = 0;
    }

    private void PassTurn()
    {
        CurrentTurn = CurrentTurn == GoStone.Black ? GoStone.White : GoStone.Black;
    }
}
