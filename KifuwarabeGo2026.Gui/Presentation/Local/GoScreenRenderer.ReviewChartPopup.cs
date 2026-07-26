namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle ReviewChartPopupBounds = new(56, 42, 1808, 996);
    private static readonly Rectangle ReviewChartPopupChartBounds = new(100, 115, 1720, 850);
    private static readonly Rectangle ReviewChartPopupCloseButtonBounds = new(1660, 55, 160, 48);
    private static readonly Rectangle ReviewChartPopupBackToLiveButtonBounds = new(1026, 55, 216, 48);
    private static readonly Rectangle ReviewChartPopupAutoUpdateBounds = new(1260, 55, 300, 48);
    private static readonly Rectangle ReviewChartPopupSeekBounds = new(180, 994, 1560, 28);
    private static readonly Rectangle ReviewChartPopupPlotBounds = new(
        ReviewChartPopupChartBounds.X + 72,
        ReviewChartPopupChartBounds.Y + 92,
        ReviewChartPopupChartBounds.Width - 144,
        ReviewChartPopupChartBounds.Height - 260);

    public static bool GetReviewChartPopupOpenHit(Point point) =>
        ReviewTrendChartBounds.Contains(point);

    public static bool GetLocalLiveChartPopupOpenHit(Point point) =>
        LocalTrendChartBounds.Contains(point);

    public static bool GetLocalGameOverChartPopupOpenHit(Point point) =>
        LocalGameOverTrendChartBounds.Contains(point);

    public static bool GetCgosLiveChartPopupOpenHit(Point point) =>
        CgosTrendChartBounds.Contains(point);

    public static bool GetReviewChartPopupCloseHit(Point point) =>
        ReviewChartPopupCloseButtonBounds.Contains(point);

    public static bool GetReviewChartPopupBackToLiveHit(Point point) =>
        ReviewChartPopupBackToLiveButtonBounds.Contains(point);

    public static bool GetReviewChartPopupAutoUpdateHit(Point point) =>
        ReviewChartPopupAutoUpdateBounds.Contains(point);

    public static bool GetReviewChartPopupTrendToggleHit(Point point) =>
        GetPopupTrendToggleBounds(ReviewChartPopupChartBounds).Contains(point);

    public static bool GetReviewChartPopupCommentToggleHit(Point point) =>
        GetPopupCommentToggleBounds(ReviewChartPopupChartBounds).Contains(point);

    public static MoveTrendDisplayMode? GetReviewChartPopupTrendDisplayModeButtonHit(Point point) =>
        GetMoveTrendDisplayModeButtonHit(point, ReviewChartPopupChartBounds);

    public static int? GetReviewChartPopupCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, ReviewChartPopupCommentOverlayBounds);

    private static Rectangle ReviewChartPopupCommentOverlayBounds =>
        new(1120, 205, 680, 740);

    public static int? GetReviewChartPopupSeekMove(Point point, int moveCount)
    {
        var navigationBounds = ReviewChartPopupSeekBounds.Contains(point)
            ? ReviewChartPopupSeekBounds
            : ReviewChartPopupPlotBounds.Contains(point) ? ReviewChartPopupPlotBounds : (Rectangle?)null;
        if (navigationBounds is not { } bounds || moveCount <= 0) return null;
        var ratio = Math.Clamp(
            (point.X - bounds.Left) / (double)Math.Max(1, bounds.Width),
            0d,
            1d);
        return Math.Clamp((int)Math.Round(ratio * moveCount), 0, moveCount);
    }

    public static int? GetReviewChartPopupStepButtonHit(Point point)
    {
        for (var index = 0; index < ReviewStepButtonValues.Length; index++)
        {
            if (ReviewChartPopupStepButtonBounds(index).Contains(point))
            {
                return ReviewStepButtonValues[index];
            }
        }
        return null;
    }

    private void DrawReviewChartPopup(GoAppSession session, Point mousePoint)
    {
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(65, 80, 125, 58));
        FillRect(ReviewChartPopupBounds, new Color(54, 69, 112, 108));
        DrawRect(ReviewChartPopupBounds, 4, new Color(158, 177, 229, 230));
        DrawText("KIFU NAVIGATION", new Vector2(92, 58), new Color(238, 242, 255), 0.62f);
        DrawCommandButton(
            ReviewChartPopupCloseButtonBounds,
            "CLOSE",
            false,
            mousePoint,
            scale: 0.38f);

        DrawMoveTrendChart(
            session,
            session.ReviewMoves,
            ReviewChartPopupChartBounds,
            mousePoint,
            session.ReviewMoveIndex,
            popup: true);

        DrawReviewChartPopupStepButtons(
            session.ReviewMoveIndex,
            session.ReviewMoveCount,
            mousePoint);
        DrawReviewChartPopupSeekBar(session);
    }

    private void DrawLocalChartPopup(GoAppSession session, Point mousePoint) =>
        DrawReadOnlyChartPopup(
            session,
            session.CurrentGameRecord.Moves,
            mousePoint,
            session.CurrentMode.Kind == GoAppModeKind.GameOver
                ? "GAME RESULT TREND"
                : session.IsLocalReplayMode ? "REPLAY GAME TREND" : "LIVE GAME TREND",
            session.LocalDisplayMoveIndex,
            seekable: session.CurrentMode.Kind == GoAppModeKind.Playing,
            showBackToLive: session.CurrentMode.Kind == GoAppModeKind.Playing,
            backToLiveEnabled: session.IsLocalReplayMode);

    private void DrawCgosLiveChartPopup(
        GoAppSession session,
        CgosGameObservation observation,
        Point mousePoint) =>
        DrawReadOnlyChartPopup(
            session,
            observation.Moves,
            mousePoint,
            observation.IsReplayMode ? "CGOS REPLAY TREND" : "CGOS LIVE TREND",
            observation.DisplayMoveIndex,
            seekable: !observation.IsFinished,
            showBackToLive: !observation.IsFinished,
            backToLiveEnabled: observation.IsReplayMode);

    private void DrawReadOnlyChartPopup(
        GoAppSession session,
        IReadOnlyList<GoGameMove> moves,
        Point mousePoint,
        string title,
        int? selectedMoveIndex = null,
        bool seekable = false,
        bool showBackToLive = false,
        bool backToLiveEnabled = false)
    {
        var visibleMoves = new MovePrefixView(
            moves,
            showBackToLive
                ? session.GetLiveChartVisibleMoveCount(moves.Count)
                : moves.Count);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(65, 80, 125, 58));
        FillRect(ReviewChartPopupBounds, new Color(54, 69, 112, 108));
        DrawRect(ReviewChartPopupBounds, 4, new Color(158, 177, 229, 230));
        DrawText(title, new Vector2(92, 58), new Color(238, 242, 255), 0.62f);
        DrawCommandButton(
            ReviewChartPopupCloseButtonBounds,
            "CLOSE",
            false,
            mousePoint,
            scale: 0.38f);
        if (showBackToLive)
        {
            DrawLiveChartAutoUpdateCheckBox(session, mousePoint);
            DrawCommandButton(
                ReviewChartPopupBackToLiveButtonBounds,
                "BACK TO LIVE",
                false,
                mousePoint,
                enabled: backToLiveEnabled,
                scale: 0.34f);
        }

        DrawMoveTrendChart(
            session,
            visibleMoves,
            ReviewChartPopupChartBounds,
            mousePoint,
            Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
            popup: true);

        if (seekable)
        {
            DrawReviewChartPopupStepButtons(
                Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
                visibleMoves.Count,
                mousePoint);
            DrawChartPopupSeekBar(
                Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
                visibleMoves.Count);
        }
        else
        {
            DrawCenteredText(
                $"{moves.Count} MOVES   VIEW ONLY",
                new Vector2(ReviewChartPopupBounds.Center.X, ReviewChartPopupBounds.Bottom - 30),
                new Color(238, 242, 255),
                0.48f);
        }
    }

    private void DrawReviewChartPopupStepButtons(
        int currentMoveIndex,
        int moveCount,
        Point mousePoint)
    {
        DrawMoveNavigationButtons(
            currentMoveIndex,
            moveCount,
            mousePoint,
            ReviewChartPopupStepButtonBounds);
    }

    private static Rectangle ReviewChartPopupStepButtonBounds(int index) =>
        new(130 + index * 112, 910, 102, 48);

    private void DrawLiveChartAutoUpdateCheckBox(GoAppSession session, Point mousePoint)
    {
        var hovered = ReviewChartPopupAutoUpdateBounds.Contains(mousePoint);
        FillRect(
            ReviewChartPopupAutoUpdateBounds,
            hovered ? new Color(47, 65, 91, 230) : new Color(31, 45, 70, 220));
        DrawRect(ReviewChartPopupAutoUpdateBounds, 2, new Color(137, 160, 205));
        var checkBounds = new Rectangle(
            ReviewChartPopupAutoUpdateBounds.X + 12,
            ReviewChartPopupAutoUpdateBounds.Y + 10,
            28,
            28);
        FillRect(checkBounds, new Color(17, 24, 48, 245));
        DrawRect(checkBounds, 2, new Color(176, 194, 242));
        if (session.IsLiveChartAutoUpdateEnabled)
        {
            DrawLine(
                new Vector2(checkBounds.X + 6, checkBounds.Y + 15),
                new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7),
                4,
                new Color(91, 218, 211));
            DrawLine(
                new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7),
                new Vector2(checkBounds.Right - 5, checkBounds.Y + 6),
                4,
                new Color(91, 218, 211));
        }
        DrawFittedText(
            "AUTO UPDATE",
            new Rectangle(
                checkBounds.Right + 10,
                ReviewChartPopupAutoUpdateBounds.Y + 6,
                ReviewChartPopupAutoUpdateBounds.Width - 60,
                ReviewChartPopupAutoUpdateBounds.Height - 12),
            Color.White,
            0.34f);
    }

    private readonly struct MovePrefixView(
        IReadOnlyList<GoGameMove> moves,
        int count) : IReadOnlyList<GoGameMove>
    {
        public int Count { get; } = Math.Clamp(count, 0, moves.Count);
        public GoGameMove this[int index] =>
            index >= 0 && index < Count
                ? moves[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<GoGameMove> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return moves[index];
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    private void DrawReviewChartPopupSeekBar(GoAppSession session)
    {
        DrawChartPopupSeekBar(session.ReviewMoveIndex, session.ReviewMoveCount);
    }

    private void DrawChartPopupSeekBar(int moveIndex, int moveCount)
    {
        FillRect(ReviewChartPopupSeekBounds, new Color(17, 24, 48, 225));
        DrawRect(ReviewChartPopupSeekBounds, 2, new Color(176, 194, 242));
        var ratio = moveCount == 0
            ? 0f
            : moveIndex / (float)moveCount;
        var x = ReviewChartPopupSeekBounds.Left + ratio * ReviewChartPopupSeekBounds.Width;
        FillRect(
            new Rectangle(
                ReviewChartPopupSeekBounds.Left,
                ReviewChartPopupSeekBounds.Y + 7,
                Math.Max(1, (int)(x - ReviewChartPopupSeekBounds.Left)),
                14),
            new Color(91, 218, 211, 220));
        DrawCircle(new Vector2(x, ReviewChartPopupSeekBounds.Center.Y), 13, new Color(255, 225, 128));
        DrawCenteredText(
            $"{moveIndex} / {moveCount}   CLICK OR DRAG TO SEEK",
            new Vector2(ReviewChartPopupSeekBounds.Center.X, ReviewChartPopupSeekBounds.Y - 25),
            new Color(238, 242, 255),
            0.48f);
    }
}
