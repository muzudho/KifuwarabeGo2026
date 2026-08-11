namespace KifuwarabeGo2026.Gui.Application;

/// <summary>レビュー中の SGF コメント編集を担当します。</summary>
public sealed partial class GoAppSession
{
    public bool TrySetReviewComment(int moveIndex, string comment)
    {
        if (CurrentMode.Kind != GoAppModeKind.Reviewing || _reviewGameRecord is null)
            return false;
        if (!_reviewGameRecord.TrySetComment(moveIndex, comment))
            return false;

        CurrentGameRecord.TrySetComment(moveIndex, comment);
        HasUnsavedReviewCommentChanges = true;
        ResetCommentPage();
        return true;
    }
}
