namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>通常の盤面編集における開始前レコードと編集状態を管理します。</summary>
public sealed partial class GoAppSession
{
    private GoGameRecord? _beforeBoardEditingRecord;

    public GoStone BoardEditingStone { get; private set; } = GoStone.Black;
    public bool CanUndoBoardEditing => _boardEditingUndoHistory.Count > 0;
    public bool CanRedoBoardEditing => _boardEditingRedoHistory.Count > 0;

    public void StartBoardEditing()
    {
        _beforeBoardEditingRecord = CurrentGameRecord.Clone();
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
        ChangeMode(GoAppModeKind.BoardEditing);
        ActivateWindow(ActiveWindowId.BoardEditing);
    }

    public void FinishBoardEditing()
    {
        _beforeBoardEditingRecord = null;
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ChangeMode(GoAppModeKind.Resting);
        DeactivateWindow(ActiveWindowId.BoardEditing);
    }

    public void CancelBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing ||
            _beforeBoardEditingRecord is not { } record)
        {
            return;
        }

        _beforeBoardEditingRecord = null;
        LoadGameRecordAsInitialPosition(record, out _);
        DeactivateWindow(ActiveWindowId.BoardEditing);
    }
}
