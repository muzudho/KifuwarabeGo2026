namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using System;
using System.Collections.Generic;

/// <summary>GoAppSession の SGF 棋譜レビュー責務をまとめます。</summary>
public sealed partial class GoAppSession
{
    // SGF 全体を保持するレビュー対象と、レビュー開始前へ戻すための退避コピー。
    // 盤面そのものとは別に、棋譜の履歴・コメントを失わず扱うために保持します。
    private GoGameRecord? _reviewGameRecord;
    private GoGameRecord? _beforeReviewGameRecord;
    /// <summary>レビュー中にコメントを変更し、まだ SGF 出力していない状態です。</summary>
    public bool HasUnsavedReviewCommentChanges { get; private set; }

    public bool HasReviewGameRecord => _reviewGameRecord is not null;
    public int ReviewMoveIndex { get; private set; }
    public int ReviewMoveCount => _reviewGameRecord?.Moves.Count ?? 0;
    public IReadOnlyList<GoGameMove> ReviewMoves =>
        _reviewGameRecord is null ? Array.Empty<GoGameMove>() : _reviewGameRecord.Moves;
    /// <summary>レビュー対象 SGF の 0 手目コメントです。</summary>
    public string ReviewRootComment => _reviewGameRecord?.RootComment ?? CurrentGameRecord.RootComment;
    public GoGameMove? ReviewCurrentMove =>
        _reviewGameRecord is not null && ReviewMoveIndex > 0 && ReviewMoveIndex <= _reviewGameRecord.Moves.Count
            ? _reviewGameRecord.Moves[ReviewMoveIndex - 1] : null;
    public string ReviewBlackPlayerName =>
        string.IsNullOrWhiteSpace(_reviewGameRecord?.BlackPlayerName) ? "BLACK" : _reviewGameRecord.BlackPlayerName;
    public string ReviewWhitePlayerName =>
        string.IsNullOrWhiteSpace(_reviewGameRecord?.WhitePlayerName) ? "WHITE" : _reviewGameRecord.WhitePlayerName;

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
