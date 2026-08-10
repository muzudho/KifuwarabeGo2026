namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// Boundary 系 Liberty Lens の共通走査です。対象となる隣点を集め、足・印・評価値を描画します。
    /// </summary>
    private void DrawRenBoundaryLens(GoRenParseResult renParse, RenParseDisplayMode displayMode, Vector2 start, float cell)
    {
        if (displayMode == RenParseDisplayMode.Nobi)
            return;

        var includesEmpty = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.AdjacentEmptyArea;
        var includesOpponent = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryOpponentCount or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var showsAdjacentArea = displayMode is RenParseDisplayMode.AdjacentEmptyArea or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var accent = includesEmpty && includesOpponent ? new Color(255, 210, 96) : includesEmpty ? new Color(126, 255, 188) : new Color(255, 144, 126);
        var deferredStrongMetrics = new List<(int RenNumber, int Value, Color Color, Color Outline)>();

        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone == GoStone.Empty)
                continue;

            var contacts = new List<(GoPoint From, GoPoint To)>();
            var boundaryPoints = new HashSet<GoPoint>();
            var directionSums = new Dictionary<GoPoint, Vector2>();
            var fallbackDirections = new Dictionary<GoPoint, Vector2>();
            var adjacentRenNumbers = new HashSet<int>();
            foreach (var point in ren.Points)
            {
                AddContact(point, point.X - 1, point.Y);
                AddContact(point, point.X + 1, point.Y);
                AddContact(point, point.X, point.Y - 1);
                AddContact(point, point.X, point.Y + 1);
            }

            var legThickness = MathHelper.Clamp(cell * 0.035f, 2f, 4f);
            var legColor = RenGraphCellColor(ren.Stone);
            if (showsAdjacentArea)
            {
                DrawAdjacentRenRelationships(renParse, contacts, adjacentRenNumbers, legColor, legThickness, start, cell);
                var adjacentArea = SumAdjacentRenAreas(renParse, adjacentRenNumbers);
                var value = displayMode == RenParseDisplayMode.Strong ? GetStrongValue(ren, adjacentArea) : adjacentArea;
                if (displayMode == RenParseDisplayMode.Strong)
                {
                    deferredStrongMetrics.Add((ren.Number, value, legColor, RenGraphCellColor(OpponentOf(ren.Stone))));
                    continue;
                }
                DrawRenMetricNumber(ren, value, RenMetricUnit.PointCount, displayMode == RenParseDisplayMode.AdjacentEmptyArea ? RenGraphCellColor(GoStone.Empty) : RenGraphCellColor(OpponentOf(ren.Stone)), start, cell);
                continue;
            }

            var originalMarkerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
            var markerRadius = Math.Max(2f, originalMarkerRadius - legThickness);
            var outerMarkerRadius = markerRadius + 4f;
            var markerCenters = new Dictionary<GoPoint, Vector2>();
            foreach (var point in boundaryPoints)
            {
                var direction = directionSums[point];
                if (direction.LengthSquared() < 0.01f)
                    direction = fallbackDirections[point];
                direction.Normalize();
                var center = BoardPoint(start, cell, point.X, point.Y);
                var clockwise = new Vector2(-direction.Y, direction.X);
                markerCenters[point] = center + (clockwise * (originalMarkerRadius + 4f) * 2f);
            }
            foreach (var contact in contacts)
                DrawLine(BoardPoint(start, cell, contact.From.X, contact.From.Y), markerCenters[contact.To], legThickness, legColor);
            foreach (var marker in markerCenters)
            {
                var targetStone = renParse.GetRen(renParse.GetRenNumber(marker.Key.X, marker.Key.Y)).Stone;
                DrawCircle(marker.Value, Math.Max(1f, outerMarkerRadius - 1f), legColor);
                DrawCircle(marker.Value, markerRadius, RenGraphCellColor(targetStone));
            }

            var valueColor = displayMode == RenParseDisplayMode.BoundaryCount ? legColor
                : displayMode is RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.AdjacentEmptyArea ? RenGraphCellColor(GoStone.Empty)
                : displayMode == RenParseDisplayMode.BoundaryOpponentCount ? RenGraphCellColor(OpponentOf(ren.Stone)) : accent;
            var outline = displayMode == RenParseDisplayMode.BoundaryCount ? RenGraphCellColor(OpponentOf(ren.Stone)) : (Color?)null;
            DrawRenMetricNumber(ren, boundaryPoints.Count, RenMetricUnit.PointCount, valueColor, start, cell, outline);

            void AddContact(GoPoint from, int x, int y)
            {
                if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size)
                    return;
                var targetRenNumber = renParse.GetRenNumber(x, y);
                if (targetRenNumber == ren.Number)
                    return;
                var targetRen = renParse.GetRen(targetRenNumber);
                var isEmpty = targetRen.Stone == GoStone.Empty;
                var isOpponent = targetRen.Stone != GoStone.Empty && targetRen.Stone != ren.Stone;
                if ((!isEmpty || !includesEmpty) && (!isOpponent || !includesOpponent))
                    return;
                var target = new GoPoint(x, y);
                contacts.Add((from, target));
                boundaryPoints.Add(target);
                var direction = new Vector2(from.X - x, from.Y - y);
                directionSums[target] = directionSums.TryGetValue(target, out var sum) ? sum + direction : direction;
                fallbackDirections.TryAdd(target, direction);
                adjacentRenNumbers.Add(targetRenNumber);
            }
        }

        if (displayMode == RenParseDisplayMode.Strong)
            DrawDeferredStrongMetrics(renParse, deferredStrongMetrics, start, cell);
    }
}
