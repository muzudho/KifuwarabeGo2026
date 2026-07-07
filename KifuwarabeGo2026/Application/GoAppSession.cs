namespace KifuwarabeGo2026.Application;

using System;
using System.Collections.Generic;

public sealed class GoAppSession
{
    private GoStone[,] _stones;

    private readonly Dictionary<GoAppModeKind, GoAppMode> _modes = new()
    {
        [GoAppModeKind.Playing] = new PlayingMode(),
        [GoAppModeKind.BoardEditing] = new BoardEditingMode(),
        [GoAppModeKind.Reviewing] = new ReviewingMode(),
        [GoAppModeKind.Resting] = new RestingMode(),
    };

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _stones = new GoStone[BoardSize, BoardSize];
    }

    public GoAppMode CurrentMode { get; private set; }

    public int BoardSize { get; private set; } = 19;

    public GoStone CurrentTurn { get; private set; } = GoStone.Black;

    public void ChangeMode(GoAppModeKind modeKind)
    {
        CurrentMode = _modes[modeKind];
    }

    public void StartPlaying()
    {
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
        if (!IsOnBoard(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Point is outside the board.");
        }

        return _stones[x, y];
    }

    public bool TryPlaceStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || !IsOnBoard(x, y) || _stones[x, y] != GoStone.Empty)
        {
            return false;
        }

        _stones[x, y] = CurrentTurn;
        PassTurn();
        return true;
    }

    public bool Pass()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        PassTurn();
        return true;
    }

    private void ClearBoard()
    {
        _stones = new GoStone[BoardSize, BoardSize];
        CurrentTurn = GoStone.Black;
    }

    private bool IsOnBoard(int x, int y) => x >= 0 && x < BoardSize && y >= 0 && y < BoardSize;

    private void PassTurn()
    {
        CurrentTurn = CurrentTurn == GoStone.Black ? GoStone.White : GoStone.Black;
    }
}
