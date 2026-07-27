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
        DrawMoveTrendChartSurface(bounds, popup);
        if (popup)
        {
            DrawPopupInformationChecks(session, bounds, mousePoint);
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

        if (!popup)
        {
            DrawCgosTrendModeButton(MoveTrendScoreButtonBounds(bounds), "SCORE", session.MoveTrendDisplayMode == MoveTrendDisplayMode.Score, mousePoint);
            DrawCgosTrendModeButton(MoveTrendBothButtonBounds(bounds), "BOTH", session.MoveTrendDisplayMode == MoveTrendDisplayMode.Both, mousePoint);
            DrawCgosTrendModeButton(MoveTrendWinRateButtonBounds(bounds), "WIN RATE", session.MoveTrendDisplayMode == MoveTrendDisplayMode.WinRate, mousePoint);
        }

        var plot = popup
            ? new Rectangle(bounds.X + 72, bounds.Y + 92, bounds.Width - 144, bounds.Height - 260)
            : new Rectangle(bounds.X + 64, bounds.Y + 62, bounds.Width - 104, bounds.Height - 106);
        FillRect(plot, popup ? new Color(26, 38, 70, 92) : new Color(26, 38, 70, 205));
        DrawRect(plot, 1, popup ? new Color(84, 119, 123) : new Color(113, 140, 166));
        var centerY = plot.Center.Y;
        var drawWinRate = popup
            ? session.IsPopupWinRateVisible
            : session.MoveTrendDisplayMode is MoveTrendDisplayMode.Both or MoveTrendDisplayMode.WinRate;
        var drawScore = popup
            ? session.IsPopupScoreVisible
            : session.MoveTrendDisplayMode is MoveTrendDisplayMode.Both or MoveTrendDisplayMode.Score;

        for (var step = -2; step <= 2; step++)
        {
            var y = centerY - step * plot.Height / 4;
            var scoreAxisBounds = popup
                ? new Rectangle(bounds.X + 4, y - 20, 68, 40)
                : new Rectangle(bounds.X, y - 22, 34, 44);
            var winRateAxisBounds = popup
                ? new Rectangle(plot.Right + 4, y - 20, 76, 40)
                : new Rectangle(plot.Right + 2, y - 22, 36, 44);
            DrawLine(new Vector2(plot.Left, y), new Vector2(plot.Right, y), step == 0 ? 2 : 1,
                step == 0 ? new Color(211, 226, 219, 165) : new Color(104, 139, 143, 70));
            if (drawScore)
            {
                DrawRotatedTrendAxisText(
                    step switch
                    {
                        > 0 => $"+{step * 10}",
                        < 0 => $"+{-step * 10}",
                        _ => "EVEN",
                    },
                    scoreAxisBounds,
                    new Color(105, 232, 224),
                    popup ? 0.58f : 0.44f,
                    plot,
                    step);
            }
            if (drawWinRate)
            {
                DrawRotatedTrendAxisText(
                    step switch { 2 => "100%", 1 => "50%", 0 => "EVEN", -1 => "50%", _ => "100%" },
                    winRateAxisBounds,
                    new Color(105, 232, 224),
                    popup ? 0.58f : 0.44f,
                    plot,
                    step);
            }
        }

        var points = MoveTrendSeriesBuilder.Build(moves);
        var maximumMove = popup ? Math.Max(1, moves.Count) : Math.Max(100, points.Count);
        if (drawWinRate)
        {
            var alpha = popup
                ? session.IsPopupScoreVisible ? (byte)68 : (byte)205
                : session.MoveTrendDisplayMode == MoveTrendDisplayMode.Both ? (byte)68 : (byte)205;
            DrawCgosWinRateSeries(points, GoStone.Black, maximumMove, plot, new Color((byte)56, (byte)220, (byte)216, alpha));
            DrawCgosWinRateSeries(points, GoStone.White, maximumMove, plot, new Color((byte)248, (byte)239, (byte)215, alpha));
        }

        if (drawScore)
        {
            DrawCgosScoreBars(points, maximumMove, plot);
        }

        DrawMoveTrendAdvantageSections(bounds, plot, popup);
        DrawCgosTrendMoveTicks(maximumMove, plot);
        DrawCommentMoveMarkers(moves, maximumMove, plot);
        DrawCgosCurrentTrendPoint(
            points,
            maximumMove,
            plot,
            currentMoveNumber,
            drawScore,
            drawWinRate);
        if (popup && session.IsPopupCommentVisible)
        {
            DrawPopupCommentOverlay(moves, session, mousePoint, currentMoveNumber);
        }
    }

    private void DrawRotatedTrendAxisText(
        string text,
        Rectangle bounds,
        Color color,
        float scale,
        Rectangle plot,
        int axisStep)
    {
        var textSize = _font.MeasureString(text);
        if (textSize.X <= 0f || textSize.Y <= 0f) return;

        // 上下端はプロットの内側へ揃え、中間目盛りはグリッド線へセンタリングする。
        var rotatedHeight = textSize.X * scale;
        var centerY = axisStep switch
        {
            2 => plot.Top + rotatedHeight / 2f,
            -2 => plot.Bottom - rotatedHeight / 2f,
            _ => bounds.Center.Y,
        };
        var center = new Vector2(bounds.Center.X, centerY);
        var origin = textSize / 2f;
        _spriteBatch.DrawString(
            _font,
            text,
            center + new Vector2(1, 1),
            new Color(0, 0, 0, 135),
            -MathHelper.PiOver2,
            origin,
            scale,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0f);
        _spriteBatch.DrawString(
            _font,
            text,
            center,
            color,
            -MathHelper.PiOver2,
            origin,
            scale,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0f);
    }

    /// <summary>
    /// ポップアップとサイドパネルで共有するチャート外装。
    /// サイズと不透明度だけを変え、色と階層は同じデザインに揃える。
    /// </summary>
    private void DrawMoveTrendChartSurface(Rectangle bounds, bool popup)
    {
        FillRect(
            new Rectangle(bounds.X + (popup ? 10 : 5), bounds.Y + (popup ? 12 : 6), bounds.Width, bounds.Height),
            new Color(0, 0, 0, popup ? 92 : 105));
        FillRect(bounds, popup ? new Color(42, 55, 92, 108) : new Color(35, 48, 78, 232));
        DrawRect(bounds, popup ? 4 : 2, new Color(151, 170, 224, popup ? 230 : 205));
        DrawRect(
            new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8),
            1,
            new Color(85, 112, 151, popup ? 120 : 145));
    }

    /// <summary>
    /// 黒有利・白有利の領域表示を大小チャートで共有する。
    /// </summary>
    private void DrawMoveTrendAdvantageSections(Rectangle bounds, Rectangle plot, bool popup)
    {
        const int labelWidth = 24;
        const int plotOverlap = 2;
        var sectionX = plot.X + plotOverlap;
        DrawVerticalResultSection(
            new Rectangle(sectionX, plot.Y, plot.Right - sectionX, plot.Height / 2),
            string.Empty,
            new Color(10, 20, 34),
            new Color(220, 232, 238),
            labelWidth,
            labelGap: 0);
        DrawVerticalResultSection(
            new Rectangle(sectionX, plot.Center.Y, plot.Right - sectionX, plot.Height / 2),
            string.Empty,
            new Color(210, 224, 232),
            new Color(24, 38, 52),
            labelWidth,
            labelGap: 0);

        var labelBounds = new Rectangle(sectionX - labelWidth, plot.Y, labelWidth, plot.Height);
        const string label = "WHITE  \u2190  ADVANTAGE  \u2192  BLACK";
        const float scale = 0.34f;
        var textSize = _font.MeasureString(label);
        var center = new Vector2(labelBounds.Center.X, labelBounds.Center.Y);
        var origin = textSize / 2f;
        _spriteBatch.DrawString(
            _font,
            label,
            center + new Vector2(2, 2),
            new Color(0, 0, 0, 175),
            -MathHelper.PiOver2,
            origin,
            scale,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0f);
        _spriteBatch.DrawString(
            _font,
            label,
            center,
            new Color(105, 232, 224),
            -MathHelper.PiOver2,
            origin,
            scale,
            Microsoft.Xna.Framework.Graphics.SpriteEffects.None,
            0f);
    }

    private void DrawPopupInformationChecks(
        GoAppSession session,
        Rectangle bounds,
        Point mousePoint)
    {
        DrawPopupInformationCheck(
            GetPopupScoreToggleBounds(bounds),
            "SCORE",
            session.IsPopupScoreVisible,
            mousePoint);
        DrawPopupInformationCheck(
            GetPopupWinRateToggleBounds(bounds),
            "WIN RATE",
            session.IsPopupWinRateVisible,
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
        FillRect(overlay, new Color(10, 18, 31, 170));
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
            var x = (int)CgosTrendX(move, maximumMove, plot);
            if (move == 0)
            {
                x += popup ? 18 : 10;
            }
            DrawFittedText(
                move.ToString(CultureInfo.InvariantCulture),
                popup ? new Rectangle(x - 32, plot.Bottom + 4, 64, 34) : new Rectangle(x - 20, plot.Bottom + 4, 40, 20),
                new Color(105, 232, 224),
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
        int? currentMoveNumber,
        bool showScore,
        bool showWinRate)
    {
        if (points.Count == 0 || (!showScore && !showWinRate)) return;
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
            ? new Rectangle(
                Math.Clamp(x + 16, plot.Left + 440, plot.Right - 300),
                plot.Top + 76,
                284,
                showScore && showWinRate ? 94 : 54)
            : new Rectangle(Math.Clamp(x + 10, plot.Left + 280, plot.Right - 158), plot.Top + 44, 150, 54);
        FillRect(tooltip, new Color(16, 26, 31, 242));
        DrawRect(tooltip, 1, new Color(113, 153, 154));
        if (showScore)
        {
            DrawFittedText(
                score,
                popup ? new Rectangle(tooltip.X + 12, tooltip.Y + 8, tooltip.Width - 24, 34) : new Rectangle(tooltip.X + 8, tooltip.Y + 5, tooltip.Width - 16, 19),
                Color.White,
                popup ? 0.38f : 0.2f);
        }
        if (showWinRate)
        {
            DrawFittedText(
                winrate,
                popup
                    ? new Rectangle(tooltip.X + 12, tooltip.Y + (showScore ? 50 : 8), tooltip.Width - 24, 34)
                    : new Rectangle(tooltip.X + 8, tooltip.Y + (showScore ? 28 : 5), tooltip.Width - 16, 19),
                new Color(126, 225, 215),
                popup ? 0.38f : 0.2f);
        }
    }

    private static float CgosTrendX(int moveNumber, int maximumMove, Rectangle plot) =>
        plot.Left + Math.Clamp(moveNumber, 0, maximumMove) / (float)Math.Max(1, maximumMove) * plot.Width;

    private static Rectangle MoveTrendScoreButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(582, chartBounds.Y + 18, 160, 52)
            : new(chartBounds.Right - 360, chartBounds.Y + 12, 104, 36);

    private static Rectangle MoveInformationTrendButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000
            ? new(bounds.X + 24, bounds.Y + 18, 180, 52)
            : new(bounds.X + 16, bounds.Y + 12, 104, 36);

    private static Rectangle MoveInformationCommentButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000
            ? new(bounds.X + 212, bounds.Y + 18, 240, 52)
            : new(bounds.X + 122, bounds.Y + 12, 142, 36);

    internal static Rectangle GetPopupScoreToggleBounds(Rectangle bounds) =>
        new(400, 55, 150, 48);

    internal static Rectangle GetPopupWinRateToggleBounds(Rectangle bounds) =>
        new(558, 55, 190, 48);

    internal static Rectangle GetPopupCommentToggleBounds(Rectangle bounds) =>
        new(1120, ReviewChartPopupCommentOverlayBounds.Y - 56, 210, 48);

    private static Rectangle MoveTrendBothButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(752, chartBounds.Y + 18, 160, 52)
            : new(chartBounds.Right - 254, chartBounds.Y + 12, 104, 36);

    private static Rectangle MoveTrendWinRateButtonBounds(Rectangle chartBounds) =>
        chartBounds.Width > 1000
            ? new(922, chartBounds.Y + 18, 186, 52)
            : new(chartBounds.Right - 148, chartBounds.Y + 12, 132, 36);
}
