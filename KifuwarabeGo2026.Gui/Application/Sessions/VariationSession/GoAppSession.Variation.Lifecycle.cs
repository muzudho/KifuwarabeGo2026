namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using System;

/// <summary>変化図編集の開始、採用用レコード作成、破棄を担当します。</summary>
public sealed partial class GoAppSession
{
    public bool StartVariationEditing(
        GoGameRecord sourceRecord,
        int sourceMoveIndex,
        GoAppModeKind returnMode,
        out string warning)
    {
        ArgumentNullException.ThrowIfNull(sourceRecord);
        var clampedMoveIndex = Math.Clamp(sourceMoveIndex, 0, sourceRecord.Moves.Count);
        if (!LoadRecordPosition(sourceRecord, clampedMoveIndex, out warning))
            return false;

        _variationSourceRecord = sourceRecord.Clone();
        _variationSourceMoveIndex = clampedMoveIndex;
        _variationReturnMode = returnMode;
        VariationEditingStone = null;
        HasVariationCustomPosition = false;
        CanAdoptVariationPosition = false;
        ClearBoardEditingHistory();
        CurrentGameRecord.Result = "";
        Winner = null;
        GameOverReason = "";
        ChangeMode(GoAppModeKind.VariationEditing);
        return true;
    }

    public void EnableVariationPositionAdoption()
    {
        if (CurrentMode.Kind == GoAppModeKind.VariationEditing)
            CanAdoptVariationPosition = true;
    }

    public GoGameRecord CreateCurrentPositionAsSetupRecord()
    {
        var metadata = CurrentGameRecord.Clone();
        var record = CreateGameRecordFromCurrentPosition();
        CopyGameRecordMetadata(metadata, record);
        record.Result = "";
        return record;
    }

    public void DiscardVariationEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.VariationEditing)
            return;

        var sourceRecord = _variationSourceRecord;
        var sourceMoveIndex = _variationSourceMoveIndex;
        var returnMode = _variationReturnMode;
        _variationSourceRecord = null;

        if (UseKind == GoAppUseKind.LocalPlay && sourceRecord is not null &&
            LoadRecordPosition(sourceRecord, sourceRecord.Moves.Count, out _))
        {
            CurrentGameRecord = sourceRecord.Clone();
            ChangeMode(returnMode);
            SeekLocalReplay(sourceMoveIndex);
            return;
        }

        ChangeMode(GoAppModeKind.Resting);
    }
}
