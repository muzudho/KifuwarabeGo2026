namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;

/// <summary>通常の盤面編集における石の配置、全消去、アンドゥ／リドゥを担当します。</summary>
public sealed partial class GoAppSession
{
    public void SetBoardEditingStone(GoStone stone)
    {
        if (stone is not (GoStone.Empty or GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Board editing stone is out of range.");

        BoardEditingStone = stone;
    }

    public bool TryEditBoardStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing)
            return false;

        var oldStone = _board.GetStone(x, y);
        if (oldStone == BoardEditingStone || !_board.TrySetEditedStone(x, y, BoardEditingStone))
            return false;

        _boardEditingUndoHistory.Push([new BoardEditingChange(x, y, oldStone, BoardEditingStone)]);
        _boardEditingRedoHistory.Clear();
        ResetEditedPositionState();
        return true;
    }

    public bool ClearBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing)
            return false;

        var changes = new List<BoardEditingChange>();
        for (var y = 0; y < BoardSize; y++)
        for (var x = 0; x < BoardSize; x++)
        {
            var stone = _board.GetStone(x, y);
            if (stone != GoStone.Empty)
                changes.Add(new BoardEditingChange(x, y, stone, GoStone.Empty));
        }

        if (changes.Count == 0)
            return false;

        foreach (var change in changes)
            _board.TrySetEditedStone(change.X, change.Y, GoStone.Empty);

        _boardEditingUndoHistory.Push(changes.ToArray());
        _boardEditingRedoHistory.Clear();
        ResetEditedPositionState();
        return true;
    }

    public bool UndoBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing || _boardEditingUndoHistory.Count == 0)
            return false;

        var changes = _boardEditingUndoHistory.Pop();
        foreach (var change in changes)
            _board.TrySetEditedStone(change.X, change.Y, change.OldStone);

        _boardEditingRedoHistory.Push(changes);
        ResetEditedPositionState();
        return true;
    }

    public bool RedoBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing || _boardEditingRedoHistory.Count == 0)
            return false;

        var changes = _boardEditingRedoHistory.Pop();
        foreach (var change in changes)
            _board.TrySetEditedStone(change.X, change.Y, change.NewStone);

        _boardEditingUndoHistory.Push(changes);
        ResetEditedPositionState();
        return true;
    }
}
