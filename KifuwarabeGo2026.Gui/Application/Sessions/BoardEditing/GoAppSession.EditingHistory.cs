namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;

/// <summary>
/// 通常の盤面編集と変化図編集で共有する編集履歴を管理します。
/// どちらの編集モードでも、盤上の石を直接変更した操作だけをこの履歴に積みます。
/// </summary>
public sealed partial class GoAppSession
{
    private readonly Stack<BoardEditingChange[]> _boardEditingUndoHistory = new();
    private readonly Stack<BoardEditingChange[]> _boardEditingRedoHistory = new();

    private void ResetEditedPositionState()
    {
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
    }

    private void ClearBoardEditingHistory()
    {
        _boardEditingUndoHistory.Clear();
        _boardEditingRedoHistory.Clear();
    }

    private readonly record struct BoardEditingChange(int X, int Y, GoStone OldStone, GoStone NewStone);
}
