namespace KifuwarabeGo2026.Gui.Application;

/// <summary>レビュー終了時の棋譜・盤面状態の確定を担当します。</summary>
public sealed partial class GoAppSession
{
    public void FinishReviewing()
    {
        _beforeReviewGameRecord = null;
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ChangeMode(GoAppModeKind.Resting);
    }
}
