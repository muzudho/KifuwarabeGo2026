namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;

/// <summary>変化図の着手、盤面編集、アンドゥ、コメント編集を担当します。</summary>
public sealed partial class GoAppSession
{
    public bool TryPlaceVariationStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing || VariationEditingStone is not null)
            return false;

        var trialBoard = _board.Clone();
        if (!trialBoard.TryPlaceStone(x, y, CurrentTurn, KoPoint, out _, out _) ||
            _positionHashes.Contains(trialBoard.CurrentHash) ||
            !_board.TryPlaceStone(x, y, CurrentTurn, KoPoint, out var capturedStones, out var nextKoPoint))
        {
            return false;
        }

        var placedBy = CurrentTurn;
        CurrentGameRecord.Moves.Add(new GoGameMove(placedBy, new GoPoint(x, y), "", null, null));
        if (placedBy == GoStone.Black)
            BlackAgehama += capturedStones;
        else
            WhiteAgehama += capturedStones;

        _positionHashes.Add(_board.CurrentHash);
        KoPoint = nextKoPoint;
        ConsecutivePasses = 0;
        PlayedMoveCount++;
        PassTurn();
        return true;
    }

    public void SetVariationEditingStone(GoStone? stone)
    {
        if (stone is not (null or GoStone.Empty or GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Variation editing stone is out of range.");

        VariationEditingStone = stone;
    }

    public bool TryEditVariationStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing || VariationEditingStone is not { } editingStone)
            return false;

        var oldStone = _board.GetStone(x, y);
        if (oldStone == editingStone || !_board.TrySetEditedStone(x, y, editingStone))
            return false;

        _boardEditingUndoHistory.Push([new BoardEditingChange(x, y, oldStone, editingStone)]);
        _boardEditingRedoHistory.Clear();
        ResetVariationEditedPosition(CurrentGameRecord.Clone());
        return true;
    }

    public bool ClearVariationBoard()
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing)
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
        ResetVariationEditedPosition(CurrentGameRecord.Clone());
        return true;
    }

    public bool PassVariation()
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing || VariationEditingStone is not null)
            return false;

        CurrentGameRecord.Moves.Add(new GoGameMove(CurrentTurn, null, "", null, null));
        KoPoint = null;
        ConsecutivePasses++;
        PlayedMoveCount++;
        PassTurn();
        return true;
    }

    public bool UndoVariation()
    {
        if (!CanUndoVariation)
            return false;

        if (VariationMoveCount > 0)
        {
            var record = CurrentGameRecord.Clone();
            record.Moves.RemoveAt(record.Moves.Count - 1);
            return LoadRecordPosition(record, record.Moves.Count, out _);
        }

        var changes = _boardEditingUndoHistory.Pop();
        foreach (var change in changes)
            _board.TrySetEditedStone(change.X, change.Y, change.OldStone);

        ResetVariationEditedPosition(CurrentGameRecord.Clone());
        return true;
    }

    /// <summary>変化図編集盤の出力対象棋譜にコメントを設定します。</summary>
    public bool TrySetVariationComment(int moveIndex, string comment)
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing ||
            !CurrentGameRecord.TrySetComment(moveIndex, comment))
        {
            return false;
        }

        ResetCommentPage();
        return true;
    }

    private void ResetVariationEditedPosition(GoGameRecord metadata)
    {
        ResetEditedPositionState();
        CopyGameRecordMetadata(metadata, CurrentGameRecord);
        CurrentGameRecord.Result = "";
        _variationSourceMoveIndex = 0;
        HasVariationCustomPosition = true;
    }
}
