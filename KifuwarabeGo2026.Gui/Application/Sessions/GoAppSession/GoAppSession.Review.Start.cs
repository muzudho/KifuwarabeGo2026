namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>レビュー開始と、保持済み棋譜からの復帰を担当します。</summary>
public sealed partial class GoAppSession
{
    public bool StartReviewingStoredGameRecord(out string warning)
    {
        warning = "";
        if (_reviewGameRecord is null)
        {
            warning = "No SGF review record is loaded.";
            return false;
        }

        _beforeReviewGameRecord = CurrentGameRecord.Clone();
        if (!ApplyReviewPosition(Math.Clamp(ReviewMoveIndex, 0, ReviewMoveCount), out warning))
        {
            _beforeReviewGameRecord = null;
            return false;
        }

        ChangeMode(GoAppModeKind.Reviewing);
        return true;
    }
}
