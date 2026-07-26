namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle CgosTrendChartBounds = new(1144, 498, 668, 342);
    private static readonly Rectangle LocalTrendChartBounds = new(1144, 466, 668, 424);
    private static readonly Rectangle LocalGameOverTrendChartBounds = new(1144, 376, 668, 466);
    private static readonly Rectangle ReviewTrendChartBounds = new(1144, 548, 668, 290);

    public static MoveTrendDisplayMode? GetCgosTrendDisplayModeButtonHit(Point point)
    {
        return GetMoveTrendDisplayModeButtonHit(point, CgosTrendChartBounds);
    }

    public static MoveInformationDisplayMode? GetCgosMoveInformationDisplayModeButtonHit(Point point) =>
        GetMoveInformationDisplayModeButtonHit(point, CgosTrendChartBounds);

    public static MoveInformationDisplayMode? GetLocalMoveInformationDisplayModeButtonHit(Point point) =>
        GetMoveInformationDisplayModeButtonHit(point, LocalTrendChartBounds);

    public static MoveInformationDisplayMode? GetLocalGameOverMoveInformationDisplayModeButtonHit(Point point) =>
        GetMoveInformationDisplayModeButtonHit(point, LocalGameOverTrendChartBounds);

    public static MoveInformationDisplayMode? GetReviewMoveInformationDisplayModeButtonHit(Point point) =>
        GetMoveInformationDisplayModeButtonHit(point, ReviewTrendChartBounds);

    public static MoveTrendDisplayMode? GetLocalTrendDisplayModeButtonHit(Point point)
    {
        return GetMoveTrendDisplayModeButtonHit(point, LocalTrendChartBounds);
    }

    public static MoveTrendDisplayMode? GetLocalGameOverTrendDisplayModeButtonHit(Point point)
    {
        return GetMoveTrendDisplayModeButtonHit(point, LocalGameOverTrendChartBounds);
    }

    public static MoveTrendDisplayMode? GetReviewTrendDisplayModeButtonHit(Point point) =>
        GetMoveTrendDisplayModeButtonHit(point, ReviewTrendChartBounds);

    private static MoveTrendDisplayMode? GetMoveTrendDisplayModeButtonHit(Point point, Rectangle chartBounds)
    {
        if (MoveTrendScoreButtonBounds(chartBounds).Contains(point)) return MoveTrendDisplayMode.Score;
        if (MoveTrendBothButtonBounds(chartBounds).Contains(point)) return MoveTrendDisplayMode.Both;
        if (MoveTrendWinRateButtonBounds(chartBounds).Contains(point)) return MoveTrendDisplayMode.WinRate;
        return null;
    }

    private void DrawCgosTrendChart(GoAppSession session, CgosGameObservation observation, Point mousePoint) =>
        DrawMoveTrendChart(session, observation.Moves, CgosTrendChartBounds, mousePoint);

    private void DrawLocalTrendChart(GoAppSession session, Point mousePoint) =>
        DrawMoveTrendChart(session, session.CurrentGameRecord.Moves, LocalTrendChartBounds, mousePoint);

    private void DrawLocalGameOverTrendChart(GoAppSession session, Point mousePoint) =>
        DrawMoveTrendChart(session, session.CurrentGameRecord.Moves, LocalGameOverTrendChartBounds, mousePoint);

    private void DrawReviewTrendChart(GoAppSession session, Point mousePoint) =>
        DrawMoveTrendChart(
            session,
            session.ReviewMoves,
            ReviewTrendChartBounds,
            mousePoint,
            session.ReviewMoveIndex);

    private void DrawMoveTrendChart(
        GoAppSession session,
        IReadOnlyList<GoGameMove> moves,
        Rectangle bounds,
        Point mousePoint,
        int? currentMoveNumber = null,
        bool popup = false)
    {
        FillRect(bounds, popup ? new Color(42, 55, 92, 108) : new Color(25, 48, 57, 246));
        DrawRect(bounds, popup ? 4 : 2, popup ? new Color(151, 170, 224) : new Color(72, 115, 121));
        if (popup)
        {
            DrawPopupInformationChecks(session, bounds, mousePoint);
            if (!session.IsPopupTrendVisible)
            {
                if (session.IsPopupCommentVisible)
                {
                    DrawPopupCommentOverlay(moves, session, mousePoint, currentMoveNumber);
                }
                return;
            }
        }
        else
        {
            DrawMoveInformationTabs(session, moves, bounds, mousePoint);

            if (session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment)
            {
                DrawMoveCommentContent(moves, bounds, session, mousePoint, currentMoveNumber);
                return;
            }
        }

        DrawCgosTrendModeButton(MoveTrendScoreButtonBounds(bounds), "SCORE", session.MoveTrendDisplayMode == MoveTrendDisplayMode.Score, mousePoint);
        DrawCgosTrendModeButton(MoveTrendBothButtonBounds(bounds), "BOTH", session.MoveTrendDisplayMode == MoveTrendDisplayMode.Both, mousePoint);
        DrawCgosTrendModeButton(MoveTrendWinRateButtonBounds(bounds), "WIN RATE", session.MoveTrendDisplayMode == MoveTrendDisplayMode.WinRate, mousePoint);

        var plot = popup
            ? new Rectangle(bounds.X + 72, bounds.Y + 92, bounds.Width - 144, bounds.Height - 260)
            : new Rectangle(bounds.X + 52, bounds.Y + 62, bounds.Width - 106, bounds.Height - 106);
        FillRect(plot, popup ? new Color(26, 38, 70, 92) : new Color(31, 57, 65));
        DrawRect(plot, 1, new Color(84, 119, 123));
        var centerY = plot.Center.Y;

        for (var step = -2; step <= 2; step++)
        {
            var y = centerY - step * plot.Height / 4;
            var scoreAxisBounds = popup
                ? new Rectangle(bounds.X + 4, y - 20, 68, 40)
                : new Rectangle(bounds.X + 8, y - 12, 42, 24);
            var winRateAxisBounds = popup
                ? new Rectangle(plot.Right + 4, y - 20, 76, 40)
                : new Rectangle(plot.Right + 5, y - 12, 47, 24);
            DrawLine(new Vector2(plot.Left, y), new Vector2(plot.Right, y), step == 0 ? 2 : 1,
                step == 0 ? new Color(211, 226, 219, 165) : new Color(104, 139, 143, 70));
            DrawFittedText(
                step switch
                {
                    > 0 => $"B+{step * 10}",
                    < 0 => $"W+{-step * 10}",
                    _ => "EVEN",
                },
                scoreAxisBounds,
                new Color(205, 217, 214),
                popup ? 0.46f : 0.25f);
            DrawFittedText(
                step switch { 2 => "B100%", 1 => "B50%", 0 => "EVEN", -1 => "W50%", _ => "W100%" },
                winRateAxisBounds,
                new Color(165, 193, 194),
                popup ? 0.4f : 0.2f);
        }

        var points = MoveTrendSeriesBuilder.Build(moves);
        var maximumMove = popup ? Math.Max(1, moves.Count) : Math.Max(100, points.Count);
        if (session.MoveTrendDisplayMode is MoveTrendDisplayMode.Both or MoveTrendDisplayMode.WinRate)
        {
            var alpha = session.MoveTrendDisplayMode == MoveTrendDisplayMode.Both ? (byte)68 : (byte)205;
            DrawCgosWinRateSeries(points, GoStone.Black, maximumMove, plot, new Color((byte)56, (byte)220, (byte)216, alpha));
            DrawCgosWinRateSeries(points, GoStone.White, maximumMove, plot, new Color((byte)248, (byte)239, (byte)215, alpha));
        }

        if (session.MoveTrendDisplayMode is MoveTrendDisplayMode.Both or MoveTrendDisplayMode.Score)
        {
            DrawCgosScoreBars(points, maximumMove, plot);
        }

        if (popup)
        {
            DrawVerticalResultSection(
                new Rectangle(bounds.X + 4, plot.Y, plot.Right - bounds.X - 4, plot.Height / 2),
                "BLACK ADVANTAGE",
                new Color(18, 86, 91));
            DrawVerticalResultSection(
                new Rectangle(bounds.X + 4, plot.Center.Y, plot.Right - bounds.X - 4, plot.Height / 2),
                "WHITE ADVANTAGE",
                new Color(91, 76, 52));
        }
        else
        {
            DrawCgosAdvantageLabel(new Rectangle(plot.X + 12, plot.Y + 8, 254, 30), black: true);
            DrawCgosAdvantageLabel(new Rectangle(plot.X + 12, plot.Bottom - 38, 254, 30), black: false);
        }
        DrawCgosTrendMoveTicks(maximumMove, plot);
        DrawCommentMoveMarkers(moves, maximumMove, plot);
        DrawCgosCurrentTrendPoint(points, maximumMove, plot, currentMoveNumber);
        if (popup && session.IsPopupCommentVisible)
        {
            DrawPopupCommentOverlay(moves, session, mousePoint, currentMoveNumber);
        }
    }

    private void DrawPopupInformationChecks(
        GoAppSession session,
        Rectangle bounds,
        Point mousePoint)
    {
        DrawPopupInformationCheck(
            GetPopupTrendToggleBounds(bounds),
            "CHART",
            session.IsPopupTrendVisible,
            mousePoint);
        DrawPopupInformationCheck(
            GetPopupCommentToggleBounds(bounds),
            "COMMENTS",
            session.IsPopupCommentVisible,
            mousePoint);
    }

    private void DrawPopupInformationCheck(
        Rectangle bounds,
        string label,
        bool isChecked,
        Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, hovered ? new Color(43, 67, 83, 238) : new Color(24, 43, 57, 232));
        DrawRect(bounds, 2, isChecked ? new Color(91, 218, 211) : new Color(91, 117, 128));
        var checkBounds = new Rectangle(bounds.X + 12, bounds.Y + 12, 28, 28);
        FillRect(checkBounds, new Color(14, 23, 35, 245));
        DrawRect(checkBounds, 2, new Color(176, 194, 212));
        if (isChecked)
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
            label,
            new Rectangle(bounds.X + 50, bounds.Y + 7, bounds.Width - 60, bounds.Height - 14),
            Color.White,
            0.34f);
    }

    private void DrawPopupCommentOverlay(
        IReadOnlyList<GoGameMove> moves,
        GoAppSession session,
        Point mousePoint,
        int? currentMoveNumber)
    {
        var overlay = ReviewChartPopupCommentOverlayBounds;
        FillRect(overlay, new Color(10, 18, 31, 218));
        DrawRect(overlay, 3, new Color(255, 215, 92, 210));
        DrawMoveCommentContent(moves, overlay, session, mousePoint, currentMoveNumber);
    }

    private static MoveInformationDisplayMode? GetMoveInformationDisplayModeButtonHit(Point point, Rectangle bounds)
    {
        if (MoveInformationTrendButtonBounds(bounds).Contains(point)) return MoveInformationDisplayMode.Trend;
        if (MoveInformationCommentButtonBounds(bounds).Contains(point)) return MoveInformationDisplayMode.Comment;
        return null;
    }

    private void DrawMoveInformationTabs(
        GoAppSession session,
        IReadOnlyList<GoGameMove> moves,
        Rectangle bounds,
        Point mousePoint)
    {
        DrawCgosTrendModeButton(
            MoveInformationTrendButtonBounds(bounds),
            "TREND",
            session.MoveInformationDisplayMode == MoveInformationDisplayMode.Trend,
            mousePoint);
        var hasComment = HasMoveComment(moves);
        DrawCgosTrendModeButton(
            MoveInformationCommentButtonBounds(bounds),
            hasComment ? "COMMENT *" : "COMMENT",
            session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment,
            mousePoint);
    }

    private void DrawCommentMoveMarkers(
        IReadOnlyList<GoGameMove> moves,
        int maximumMove,
        Rectangle plot)
    {
        for (var index = 0; index < moves.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(moves[index].Comment)) continue;
            var x = (int)CgosTrendX(index + 1, maximumMove, plot);
            DrawCenteredText(
                "*",
                new Vector2(x, plot.Bottom - 48),
                new Color(255, 215, 92),
                0.35f);
        }
    }

    private void DrawCgosTrendModeButton(Rectangle bounds, string label, bool selected, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(24, 91, 94) : hovered ? new Color(42, 65, 70) : new Color(22, 37, 43));
        DrawRect(bounds, selected ? 2 : 1, selected ? new Color(60, 232, 221) : new Color(76, 104, 108));
        DrawFittedText(label, new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, bounds.Height - 12),
            selected ? new Color(105, 247, 232) : new Color(211, 216, 210), bounds.Height >= 50 ? 0.42f : 0.27f);
    }

    private void DrawCgosWinRateSeries(
        IReadOnlyList<MoveTrendPoint> points,
        GoStone reporter,
        int maximumMove,
        Rectangle plot,
        Color color)
    {
        Vector2? previous = null;
        foreach (var point in points)
        {
            if (point.Reporter != reporter) continue;
            if (point.BlackPerspectiveWinAdvantage is not { } advantage)
            {
                previous = null;
                continue;
            }

            var current = new Vector2(
                CgosTrendX(point.MoveNumber, maximumMove, plot),
                plot.Center.Y - (float)Math.Clamp(advantage, -1.0, 1.0) * plot.Height / 2f);
            if (previous is { } start)
            {
                DrawLine(start, current, 5, new Color(color.R, color.G, color.B, (byte)Math.Max(20, color.A / 3)));
                DrawLine(start, current, 2, color);
            }

            DrawCircle(current, 4, color);
            previous = current;
        }
    }

    private void DrawCgosScoreBars(IReadOnlyList<MoveTrendPoint> points, int maximumMove, Rectangle plot)
    {
        var barWidth = Math.Clamp(plot.Width / Math.Max(maximumMove, 1), 2, 7);
        foreach (var point in points)
        {
            if (point.BlackPerspectiveScore is not { } score) continue;

            var x = (int)CgosTrendX(point.MoveNumber, maximumMove, plot);
            var valueY = plot.Center.Y - (float)Math.Clamp(score / 20.0, -1.0, 1.0) * plot.Height / 2f;
            var top = (int)Math.Min(valueY, plot.Center.Y);
            var height = Math.Max(2, (int)Math.Abs(valueY - plot.Center.Y));
            var bar = new Rectangle(x - barWidth / 2, top, barWidth, height);
            var black = point.Reporter == GoStone.Black;
            FillRect(bar, black ? new Color(8, 13, 17, 230) : new Color(245, 237, 213, 232));
            DrawRect(bar, 1, black ? new Color(57, 211, 205, 205) : new Color(86, 93, 91, 220));
        }
    }

    private void DrawCgosAdvantageLabel(Rectangle bounds, bool black)
    {
        FillRect(bounds, black ? new Color(18, 43, 49, 230) : new Color(51, 62, 61, 230));
        DrawRect(bounds, 1, black ? new Color(75, 190, 187) : new Color(191, 181, 146));
        DrawCircle(new Vector2(bounds.X + 17, bounds.Center.Y), 8, black ? new Color(4, 7, 10) : new Color(245, 239, 218));
        DrawFittedText(
            black ? "黒有利  BLACK ADVANTAGE" : "白有利  WHITE ADVANTAGE",
            new Rectangle(bounds.X + 32, bounds.Y + 5, bounds.Width - 40, bounds.Height - 10),
            black ? new Color(100, 229, 218) : new Color(247, 231, 184),
            bounds.Height >= 48 ? 0.42f : 0.23f);
    }

    private void DrawCgosTrendMoveTicks(int maximumMove, Rectangle plot)
    {
        var popup = plot.Width > 1000;
        foreach (var move in new[] { 0, 25, 50, 75, 100 })
        {
            if (move > maximumMove) continue;
            var x = (int)CgosTrendX(Math.Max(1, move), maximumMove, plot);
            DrawFittedText(
                move.ToString(CultureInfo.InvariantCulture),
                popup ? new Rectangle(x - 32, plot.Bottom + 4, 64, 34) : new Rectangle(x - 20, plot.Bottom + 4, 40, 20),
                new Color(180, 200, 198),
                popup ? 0.38f : 0.2f);
        }

        DrawFittedText(
            "MOVE",
            popup ? new Rectangle(plot.Center.X - 60, plot.Bottom + 34, 120, 38) : new Rectangle(plot.Center.X - 36, plot.Bottom + 23, 72, 20),
            new Color(76, 222, 213),
            popup ? 0.42f : 0.23f);
    }

    private void DrawCgosCurrentTrendPoint(
        IReadOnlyList<MoveTrendPoint> points,
        int maximumMove,
        Rectangle plot,
        int? currentMoveNumber)
    {
        if (points.Count == 0) return;
        MoveTrendPoint? selectedPoint = null;
        foreach (var candidate in points)
        {
            if (currentMoveNumber is not null && candidate.MoveNumber > currentMoveNumber.Value) break;
            selectedPoint = candidate;
        }
        if (selectedPoint is not { } point) return;
        var x = (int)CgosTrendX(point.MoveNumber, maximumMove, plot);
        DrawLine(new Vector2(x, plot.Top), new Vector2(x, plot.Bottom), 2, new Color(54, 229, 218, 190));

        var reporter = point.Reporter == GoStone.Black ? "B" : "W";
        var score = point.BlackPerspectiveScore is { } scoreValue
            ? $"{reporter} SCORE {scoreValue.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)}"
            : $"{reporter} SCORE -";
        var winrate = point.BlackPerspectiveWinAdvantage is { } advantage
            ? $"BLACK WIN {((advantage + 1.0) / 2.0).ToString("P1", CultureInfo.InvariantCulture)}"
            : "BLACK WIN -";
        var popup = plot.Width > 1000;
        var tooltip = popup
            ? new Rectangle(Math.Clamp(x + 16, plot.Left + 440, plot.Right - 300), plot.Top + 76, 284, 94)
            : new Rectangle(Math.Clamp(x + 10, plot.Left + 280, plot.Right - 158), plot.Top + 44, 150, 54);
        FillRect(tooltip, new Color(16, 26, 31, 242));
        DrawRect(tooltip, 1, new Color(113, 153, 154));
        DrawFittedText(
            score,
            popup ? new Rectangle(tooltip.X + 12, tooltip.Y + 8, tooltip.Width - 24, 34) : new Rectangle(tooltip.X + 8, tooltip.Y + 5, tooltip.Width - 16, 19),
            Color.White,
            popup ? 0.38f : 0.2f);
        DrawFittedText(
            winrate,
            popup ? new Rectangle(tooltip.X + 12, tooltip.Y + 50, tooltip.Width - 24, 34) : new Rectangle(tooltip.X + 8, tooltip.Y + 28, tooltip.Width - 16, 19),
            new Color(126, 225, 215),
            popup ? 0.38f : 0.2f);
    }

    private static float CgosTrendX(int moveNumber, int maximumMove, Rectangle plot) =>
        plot.Left + (Math.Clamp(moveNumber, 1, maximumMove) - 1f) / Math.Max(1, maximumMove - 1) * plot.Width;

    private static Rectangle MoveTrendScoreButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(chartBounds.Right - 570, chartBounds.Y + 18, 160, 52)
            : new(chartBounds.Right - 360, chartBounds.Y + 12, 104, 36);

    private static Rectangle MoveInformationTrendButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000
            ? new(bounds.X + 24, bounds.Y + 18, 180, 52)
            : new(bounds.X + 16, bounds.Y + 12, 104, 36);

    private static Rectangle MoveInformationCommentButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000
            ? new(bounds.X + 212, bounds.Y + 18, 240, 52)
            : new(bounds.X + 122, bounds.Y + 12, 142, 36);

    internal static Rectangle GetPopupTrendToggleBounds(Rectangle bounds) =>
        new(450, 55, 170, 48);

    internal static Rectangle GetPopupCommentToggleBounds(Rectangle bounds) =>
        new(628, 55, 210, 48);

    private static Rectangle MoveTrendBothButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(chartBounds.Right - 400, chartBounds.Y + 18, 160, 52)
            : new(chartBounds.Right - 254, chartBounds.Y + 12, 104, 36);

    private static Rectangle MoveTrendWinRateButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(chartBounds.Right - 230, chartBounds.Y + 18, 200, 52)
            : new(chartBounds.Right - 148, chartBounds.Y + 12, 132, 36);
}
