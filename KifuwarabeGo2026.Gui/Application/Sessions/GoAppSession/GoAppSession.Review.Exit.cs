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

    /// <summary>
    /// レビュー位置を採用せず、レビュー中の棋譜を保持したまま休憩画面へ戻ります。
    /// </summary>
    public void ReturnFromReviewingToResting()
    {
        if (CurrentMode.Kind != GoAppModeKind.Reviewing || _reviewGameRecord is null)
            return;

        var reviewRecord = _reviewGameRecord.Clone();
        var reviewMoveIndex = ReviewMoveIndex;

        // コメントを編集したレビュー棋譜を次回の REVIEW にも引き継ぐ。
        // 保存済みの SGF と次回のレビュー表示が食い違わないよう、開始前の一時盤には戻さない。
        if (LoadGameRecordAsInitialPosition(reviewRecord, out _))
        {
            CurrentGameRecord = reviewRecord.Clone();
        }
        else
        {
            ChangeMode(GoAppModeKind.Resting);
        }

        _reviewGameRecord = reviewRecord;
        ReviewMoveIndex = reviewMoveIndex;
        _beforeReviewGameRecord = null;
    }

    /// <summary>読み込み済みのレビュー棋譜を破棄して、空の休憩盤へ戻します。</summary>
    public void ClearSgfGameRecord()
    {
        _reviewGameRecord = null;
        ReviewMoveIndex = 0;
        IsReviewResultPosition = false;
        ClearBoard();
        ChangeMode(GoAppModeKind.Resting);
    }
}
