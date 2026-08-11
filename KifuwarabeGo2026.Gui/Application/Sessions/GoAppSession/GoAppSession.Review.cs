namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using System;

/// <summary>GoAppSession の SGF 棋譜レビュー責務をまとめます。</summary>
public sealed partial class GoAppSession
{
    // SGF 全体を保持するレビュー対象と、レビュー開始前へ戻すための退避コピー。
    // 盤面そのものとは別に、棋譜の履歴・コメントを失わず扱うために保持します。
    private GoGameRecord? _reviewGameRecord;
    private GoGameRecord? _beforeReviewGameRecord;

    public bool MoveReview(int step, out string warning)
    {
        warning = "";
        if (CurrentMode.Kind != GoAppModeKind.Reviewing || _reviewGameRecord is null) return false;

        var moved = ApplyReviewPosition(Math.Clamp(ReviewMoveIndex + step, 0, ReviewMoveCount), out warning);
        if (moved)
        {
            CommentPageIndex = 0;
            CommentPageCount = 1;
        }
        return moved;
    }

    public void MarkReviewCommentsSaved() => HasUnsavedReviewCommentChanges = false;
}
