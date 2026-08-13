namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared.RenBoundaries;
using KifuwarabeGo2026.Shared.BoardLens.Strong;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    private readonly RenBoundaryPoints _renBoundaryPoints = new();

    private void DrawRenBoundaryLens(GoRenParseResult renParse, RenParseDisplayMode displayMode, Vector2 start, float cell)
    {
        if (displayMode == RenParseDisplayMode.Nobi) return;
        var includesEmpty = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.AdjacentEmptyArea;
        var includesOpponent = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryOpponentCount or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var showsAdjacentArea = displayMode is RenParseDisplayMode.AdjacentEmptyArea or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var accent = includesEmpty && includesOpponent ? new Color(255, 210, 96) : includesEmpty ? new Color(126, 255, 188) : new Color(255, 144, 126);
        var deferredStrongMetrics = new List<(int RenNumber, int Value, Color Color, Color Outline)>();
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone == GoStone.Empty) continue;
            var boundary = _renBoundaryPoints.Collect(renParse, ren, includesEmpty, includesOpponent);
            var legThickness = MathHelper.Clamp(cell * 0.035f, 2f, 4f); var legColor = RenGraphCellColor(ren.Stone);
            if (showsAdjacentArea)
            {
                DrawAdjacentRenRelationships(renParse, boundary.Contacts, boundary.AdjacentRenNumbers, legColor, legThickness, start, cell);
                var adjacentArea = SumAdjacentRenAreas(renParse, boundary.AdjacentRenNumbers);
                var value = displayMode == RenParseDisplayMode.Strong ? StrongAnalyzer.Analyze(renParse, ren.Number).Value : adjacentArea;
                if (displayMode == RenParseDisplayMode.Strong) { deferredStrongMetrics.Add((ren.Number, value, legColor, RenGraphCellColor(OpponentOf(ren.Stone)))); continue; }
                DrawRenMetricNumber(ren, value, RenMetricUnit.PointCount, displayMode == RenParseDisplayMode.AdjacentEmptyArea ? RenGraphCellColor(GoStone.Empty) : RenGraphCellColor(OpponentOf(ren.Stone)), start, cell);
                continue;
            }
            var originalRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f); var radius = Math.Max(2f, originalRadius - legThickness); var outerRadius = radius + 4f;
            var centers = new Dictionary<GoPoint, Vector2>();
            foreach (var point in boundary.Points)
            {
                var direction = boundary.DirectionSums[point]; if (direction.LengthSquared() < 0.01f) direction = boundary.FallbackDirections[point]; direction.Normalize();
                centers[point] = BoardPoint(start, cell, point.X, point.Y) + new Vector2(-direction.Y, direction.X) * (originalRadius + 4f) * 2f;
            }
            foreach (var contact in boundary.Contacts) DrawLine(BoardPoint(start, cell, contact.From.X, contact.From.Y), centers[contact.To], legThickness, legColor);
            foreach (var marker in centers)
            {
                var stone = renParse.GetRen(renParse.GetRenNumber(marker.Key.X, marker.Key.Y)).Stone;
                DrawCircle(marker.Value, Math.Max(1f, outerRadius - 1f), legColor); DrawCircle(marker.Value, radius, RenGraphCellColor(stone));
            }
            var color = displayMode == RenParseDisplayMode.BoundaryCount ? legColor : displayMode == RenParseDisplayMode.BoundaryEmptyCount ? RenGraphCellColor(GoStone.Empty) : displayMode == RenParseDisplayMode.BoundaryOpponentCount ? RenGraphCellColor(OpponentOf(ren.Stone)) : accent;
            var outline = displayMode == RenParseDisplayMode.BoundaryCount ? RenGraphCellColor(OpponentOf(ren.Stone)) : (Color?)null;
            DrawRenMetricNumber(ren, boundary.Points.Count, RenMetricUnit.PointCount, color, start, cell, outline);
        }
        if (displayMode == RenParseDisplayMode.Strong) DrawDeferredStrongMetrics(renParse, deferredStrongMetrics, start, cell);
    }
}
