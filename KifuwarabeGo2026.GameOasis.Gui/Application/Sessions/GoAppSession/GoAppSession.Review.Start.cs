namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using System;

/// <summary>レビュー開始と、保持済み棋譜からの復帰を担当します。</summary>
public sealed partial class GoAppSession
{
    /// <summary>
    /// 指定した棋譜をレビュー用の完全レコードとして保持し、0 手目から表示します。
    /// </summary>
    public bool StartReviewingGameRecord(GoGameRecord record, out string warning)
    {
        ArgumentNullException.ThrowIfNull(record);

        _beforeReviewGameRecord = CurrentGameRecord.Clone();
        _reviewGameRecord = record.Clone();
        HasUnsavedReviewCommentChanges = false;
        ReviewMoveIndex = 0;
        IsReviewResultPosition = false;
        if (!ApplyReviewPosition(record.Moves.Count, out warning))
        {
            ClearFailedReviewStart();
            return false;
        }

        if (!ApplyReviewPosition(0, out warning))
        {
            ClearFailedReviewStart();
            return false;
        }

        // ルートコメントがある棋譜は、最初にその解説を見せる。
        MoveInformationDisplayMode = string.IsNullOrWhiteSpace(_reviewGameRecord.RootComment)
            ? MoveInformationDisplayMode.Trend
            : MoveInformationDisplayMode.Comment;
        ChangeMode(GoAppModeKind.Reviewing);
        return true;
    }

    public bool StartReviewingStoredGameRecord(out string warning)
    {
        warning = "";
        if (_reviewGameRecord is null)
        {
            warning = "No SGF review record is loaded.";
            return false;
        }

        _beforeReviewGameRecord = CurrentGameRecord.Clone();
        IsReviewResultPosition = false;
        if (!ApplyReviewPosition(Math.Clamp(ReviewMoveIndex, 0, ReviewMoveCount), out warning))
        {
            _beforeReviewGameRecord = null;
            return false;
        }

        ChangeMode(GoAppModeKind.Reviewing);
        return true;
    }

    private void ClearFailedReviewStart()
    {
        _beforeReviewGameRecord = null;
        _reviewGameRecord = null;
        ReviewMoveIndex = 0;
        IsReviewResultPosition = false;
    }
}
