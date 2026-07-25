namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle CgosTrendChartBounds = new(1144, 498, 668, 342);
    private static readonly Rectangle CgosTrendPlotBounds = new(1196, 560, 562, 236);
    private static readonly Rectangle CgosTrendScoreButtonBounds = new(1452, 510, 104, 36);
    private static readonly Rectangle CgosTrendBothButtonBounds = new(1558, 510, 104, 36);
    private static readonly Rectangle CgosTrendWinRateButtonBounds = new(1664, 510, 132, 36);

    public static CgosTrendDisplayMode? GetCgosTrendDisplayModeButtonHit(Point point)
    {
        if (CgosTrendScoreButtonBounds.Contains(point)) return CgosTrendDisplayMode.Score;
        if (CgosTrendBothButtonBounds.Contains(point)) return CgosTrendDisplayMode.Both;
        if (CgosTrendWinRateButtonBounds.Contains(point)) return CgosTrendDisplayMode.WinRate;
        return null;
    }

    private void DrawCgosTrendChart(GoAppSession session, CgosGameObservation observation, Point mousePoint)
    {
        var bounds = CgosTrendChartBounds;
        FillRect(bounds, new Color(25, 48, 57, 246));
        DrawRect(bounds, 2, new Color(72, 115, 121));
        DrawText("POSITION TREND", new Vector2(bounds.X + 16, bounds.Y + 12), new Color(82, 225, 216), 0.42f);

        DrawCgosTrendModeButton(CgosTrendScoreButtonBounds, "SCORE", session.CgosTrendDisplayMode == CgosTrendDisplayMode.Score, mousePoint);
        DrawCgosTrendModeButton(CgosTrendBothButtonBounds, "BOTH", session.CgosTrendDisplayMode == CgosTrendDisplayMode.Both, mousePoint);
        DrawCgosTrendModeButton(CgosTrendWinRateButtonBounds, "WIN RATE", session.CgosTrendDisplayMode == CgosTrendDisplayMode.WinRate, mousePoint);

        var plot = CgosTrendPlotBounds;
        FillRect(plot, new Color(31, 57, 65));
        DrawRect(plot, 1, new Color(84, 119, 123));
        var centerY = plot.Center.Y;

        for (var step = -2; step <= 2; step++)
        {
            var y = centerY - step * plot.Height / 4;
            DrawLine(new Vector2(plot.Left, y), new Vector2(plot.Right, y), step == 0 ? 2 : 1,
                step == 0 ? new Color(211, 226, 219, 165) : new Color(104, 139, 143, 70));
            DrawFittedText(
                (step * 10).ToString("+0;-0;0", CultureInfo.InvariantCulture),
                new Rectangle(bounds.X + 8, y - 12, 42, 24),
                new Color(205, 217, 214),
                0.25f);
            DrawFittedText(
                step switch { 2 => "+100%", 1 => "+50%", 0 => "EVEN", -1 => "-50%", _ => "-100%" },
                new Rectangle(plot.Right + 5, y - 12, 47, 24),
                new Color(165, 193, 194),
                0.2f);
        }

        var points = CgosTrendSeriesBuilder.Build(observation.Moves);
        var maximumMove = Math.Max(100, points.Count);
        if (session.CgosTrendDisplayMode is CgosTrendDisplayMode.Both or CgosTrendDisplayMode.WinRate)
        {
            var alpha = session.CgosTrendDisplayMode == CgosTrendDisplayMode.Both ? (byte)68 : (byte)205;
            DrawCgosWinRateSeries(points, GoStone.Black, maximumMove, plot, new Color((byte)56, (byte)220, (byte)216, alpha));
            DrawCgosWinRateSeries(points, GoStone.White, maximumMove, plot, new Color((byte)248, (byte)239, (byte)215, alpha));
        }

        if (session.CgosTrendDisplayMode is CgosTrendDisplayMode.Both or CgosTrendDisplayMode.Score)
        {
            DrawCgosScoreBars(points, maximumMove, plot);
        }

        DrawCgosAdvantageLabel(new Rectangle(plot.X + 12, plot.Y + 8, 254, 30), black: true);
        DrawCgosAdvantageLabel(new Rectangle(plot.X + 12, plot.Bottom - 38, 254, 30), black: false);
        DrawCgosTrendMoveTicks(maximumMove, plot);
        DrawCgosCurrentTrendPoint(points, maximumMove, plot);
    }

    private void DrawCgosTrendModeButton(Rectangle bounds, string label, bool selected, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(24, 91, 94) : hovered ? new Color(42, 65, 70) : new Color(22, 37, 43));
        DrawRect(bounds, selected ? 2 : 1, selected ? new Color(60, 232, 221) : new Color(76, 104, 108));
        DrawFittedText(label, new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, bounds.Height - 12),
            selected ? new Color(105, 247, 232) : new Color(211, 216, 210), 0.27f);
    }

    private void DrawCgosWinRateSeries(
        IReadOnlyList<CgosTrendPoint> points,
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

    private void DrawCgosScoreBars(IReadOnlyList<CgosTrendPoint> points, int maximumMove, Rectangle plot)
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
            0.23f);
    }

    private void DrawCgosTrendMoveTicks(int maximumMove, Rectangle plot)
    {
        foreach (var move in new[] { 0, 25, 50, 75, 100 })
        {
            if (move > maximumMove) continue;
            var x = (int)CgosTrendX(Math.Max(1, move), maximumMove, plot);
            DrawFittedText(move.ToString(CultureInfo.InvariantCulture), new Rectangle(x - 20, plot.Bottom + 4, 40, 20),
                new Color(180, 200, 198), 0.2f);
        }

        DrawFittedText("MOVE", new Rectangle(plot.Center.X - 36, plot.Bottom + 23, 72, 20), new Color(76, 222, 213), 0.23f);
    }

    private void DrawCgosCurrentTrendPoint(IReadOnlyList<CgosTrendPoint> points, int maximumMove, Rectangle plot)
    {
        if (points.Count == 0) return;
        var point = points[^1];
        var x = (int)CgosTrendX(point.MoveNumber, maximumMove, plot);
        DrawLine(new Vector2(x, plot.Top), new Vector2(x, plot.Bottom), 2, new Color(54, 229, 218, 190));

        var reporter = point.Reporter == GoStone.Black ? "B" : "W";
        var score = point.BlackPerspectiveScore is { } scoreValue
            ? $"{reporter} SCORE {scoreValue.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)}"
            : $"{reporter} SCORE -";
        var winrate = point.BlackPerspectiveWinAdvantage is { } advantage
            ? $"BLACK WIN {((advantage + 1.0) / 2.0).ToString("P1", CultureInfo.InvariantCulture)}"
            : "BLACK WIN -";
        var tooltip = new Rectangle(Math.Clamp(x + 10, plot.Left + 280, plot.Right - 158), plot.Top + 44, 150, 54);
        FillRect(tooltip, new Color(16, 26, 31, 242));
        DrawRect(tooltip, 1, new Color(113, 153, 154));
        DrawFittedText(score, new Rectangle(tooltip.X + 8, tooltip.Y + 5, tooltip.Width - 16, 19), Color.White, 0.2f);
        DrawFittedText(winrate, new Rectangle(tooltip.X + 8, tooltip.Y + 28, tooltip.Width - 16, 19), new Color(126, 225, 215), 0.2f);
    }

    private static float CgosTrendX(int moveNumber, int maximumMove, Rectangle plot) =>
        plot.Left + (Math.Clamp(moveNumber, 1, maximumMove) - 1f) / Math.Max(1, maximumMove - 1) * plot.Width;
}
