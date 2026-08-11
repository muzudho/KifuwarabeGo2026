namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using System;
using CgosFlowKind = KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget.CgosConnectionFlowKind;

/// <summary>着手情報、コメント、チャートの表示状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public MoveTrendDisplayMode MoveTrendDisplayMode { get; private set; } = MoveTrendDisplayMode.Both;
    public MoveInformationDisplayMode MoveInformationDisplayMode { get; private set; } = MoveInformationDisplayMode.Trend;
    public int CommentPageIndex { get; private set; }
    public int CommentPageCount { get; private set; } = 1;
    public bool IsReviewChartPopupOpen { get; private set; }
    public bool IsPopupScoreVisible { get; private set; } = true;
    public bool IsPopupWinRateVisible { get; private set; } = true;
    public bool IsPopupCommentVisible { get; private set; } = true;
    public bool IsLiveChartAutoUpdateEnabled { get; private set; } = true;
    private int? _liveChartFrozenMoveCount;

    public bool CanOpenLocalChartPopup => CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver;

    public void SetMoveTrendDisplayMode(MoveTrendDisplayMode mode) => MoveTrendDisplayMode = mode;

    public void SetMoveInformationDisplayMode(MoveInformationDisplayMode mode)
    {
        MoveInformationDisplayMode = mode;
        ResetCommentPage();
    }

    public void TogglePopupScoreVisibility() => IsPopupScoreVisible = !IsPopupScoreVisible;
    public void TogglePopupWinRateVisibility() => IsPopupWinRateVisible = !IsPopupWinRateVisible;

    public void TogglePopupCommentVisibility()
    {
        IsPopupCommentVisible = !IsPopupCommentVisible;
        ResetCommentPage();
    }

    public void ChangeCommentPage(int step)
    {
        CommentPageIndex = Math.Clamp(CommentPageIndex + step, 0, Math.Max(0, CommentPageCount - 1));
    }

    public void ResetCommentPage()
    {
        CommentPageIndex = 0;
        CommentPageCount = 1;
    }

    public void UpdateCommentPageCount(int pageCount)
    {
        CommentPageCount = Math.Max(1, pageCount);
        CommentPageIndex = Math.Clamp(CommentPageIndex, 0, CommentPageCount - 1);
    }

    public void OpenReviewChartPopup()
    {
        if (CurrentMode.Kind == GoAppModeKind.Reviewing)
            IsReviewChartPopupOpen = true;
    }

    public void OpenLocalChartPopup()
    {
        if (CanOpenLocalChartPopup)
            IsReviewChartPopupOpen = true;
    }

    public int GetLiveChartVisibleMoveCount(int currentMoveCount) =>
        IsLiveChartAutoUpdateEnabled
            ? currentMoveCount
            : Math.Min(_liveChartFrozenMoveCount ?? currentMoveCount, currentMoveCount);

    public void ToggleLiveChartAutoUpdate(int currentMoveCount)
    {
        IsLiveChartAutoUpdateEnabled = !IsLiveChartAutoUpdateEnabled;
        _liveChartFrozenMoveCount = IsLiveChartAutoUpdateEnabled ? null : Math.Max(0, currentMoveCount);
    }

    public void ResetLiveChartAutoUpdate()
    {
        IsLiveChartAutoUpdateEnabled = true;
        _liveChartFrozenMoveCount = null;
    }

    public void OpenCgosLiveChartPopup()
    {
        if (UseKind == GoAppUseKind.CgosClient &&
            CgosConnectionFlowKind is CgosFlowKind.Watching or CgosFlowKind.Result)
        {
            IsReviewChartPopupOpen = true;
        }
    }

    public void CloseReviewChartPopup() => IsReviewChartPopupOpen = false;
}
