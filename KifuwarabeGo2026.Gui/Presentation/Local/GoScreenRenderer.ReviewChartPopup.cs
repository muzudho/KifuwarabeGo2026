namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle ReviewChartPopupBounds = new(56, 42, 1808, 996);
    private static readonly Rectangle ReviewChartPopupChartBounds = new(100, 115, 1720, 850);
    private static readonly Rectangle ReviewChartPopupCloseButtonBounds = new(1660, 55, 160, 48);
    private static readonly Rectangle ReviewChartPopupSeekBounds = new(180, 912, 1560, 28);
    private static readonly Rectangle ReviewChartPopupPlotBounds = new(
        ReviewChartPopupChartBounds.X + 72,
        ReviewChartPopupChartBounds.Y + 92,
        ReviewChartPopupChartBounds.Width - 144,
        ReviewChartPopupChartBounds.Height - 190);

    public static bool GetReviewChartPopupOpenHit(Point point) =>
        ReviewTrendChartBounds.Contains(point);

    public static bool GetReviewChartPopupCloseHit(Point point) =>
        ReviewChartPopupCloseButtonBounds.Contains(point);

    public static MoveInformationDisplayMode? GetReviewChartPopupInformationDisplayModeButtonHit(Point point) =>
        GetMoveInformationDisplayModeButtonHit(point, ReviewChartPopupChartBounds);

    public static MoveTrendDisplayMode? GetReviewChartPopupTrendDisplayModeButtonHit(Point point) =>
        GetMoveTrendDisplayModeButtonHit(point, ReviewChartPopupChartBounds);

    public static int? GetReviewChartPopupCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, ReviewChartPopupChartBounds);

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

    private void DrawReviewChartPopup(GoAppSession session, Point mousePoint)
    {
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(65, 80, 125, 132));
        FillRect(ReviewChartPopupBounds, new Color(54, 69, 112, 184));
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

        if (session.MoveInformationDisplayMode == MoveInformationDisplayMode.Trend)
        {
            DrawReviewChartPopupSeekBar(session);
        }
    }

    private void DrawReviewChartPopupSeekBar(GoAppSession session)
    {
        FillRect(ReviewChartPopupSeekBounds, new Color(17, 24, 48, 225));
        DrawRect(ReviewChartPopupSeekBounds, 2, new Color(176, 194, 242));
        var ratio = session.ReviewMoveCount == 0
            ? 0f
            : session.ReviewMoveIndex / (float)session.ReviewMoveCount;
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
            $"{session.ReviewMoveIndex} / {session.ReviewMoveCount}   CLICK OR DRAG TO SEEK",
            new Vector2(ReviewChartPopupSeekBounds.Center.X, ReviewChartPopupSeekBounds.Y - 18),
            new Color(238, 242, 255),
            0.32f);
    }
}
