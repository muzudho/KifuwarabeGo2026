namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>変化図編集の開始位置、編集状態、採用可否を保持します。</summary>
public sealed partial class GoAppSession
{
    private GoGameRecord? _variationSourceRecord;
    private int _variationSourceMoveIndex;
    private GoAppModeKind _variationReturnMode = GoAppModeKind.Resting;

    public int VariationSourceMoveIndex => _variationSourceMoveIndex;
    public int VariationMoveCount => Math.Max(0, CurrentGameRecord.Moves.Count - _variationSourceMoveIndex);
    public bool CanUndoVariation =>
        CurrentMode.Kind == GoAppModeKind.VariationEditing &&
        (VariationMoveCount > 0 || _boardEditingUndoHistory.Count > 0);
    public GoStone? VariationEditingStone { get; private set; }
    public bool HasVariationCustomPosition { get; private set; }
    public bool CanAdoptVariationPosition { get; private set; }
}
