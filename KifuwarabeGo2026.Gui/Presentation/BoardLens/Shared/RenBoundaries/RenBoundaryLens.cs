namespace KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared.RenBoundaries;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Shared.BoardLens.Strong;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>連の境界交点を利用する Board Lens を静的に描画します。</summary>
public static class RenBoundaryLens
{
    private static readonly RenBoundaryPoints BoundaryPoints = new();

    public static void DrawRenBoundaryLens(BoardLensModel renderer, GoRenParseResult renParse,
        RenParseDisplayMode displayMode, Vector2 start, float cell)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (displayMode == RenParseDisplayMode.Nobi) return;
        var includesEmpty = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.AdjacentEmptyArea;
        var includesOpponent = displayMode is RenParseDisplayMode.BoundaryCount or RenParseDisplayMode.BoundaryOpponentCount or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var showsAdjacentArea = displayMode is RenParseDisplayMode.AdjacentEmptyArea or RenParseDisplayMode.AdjacentOpponentArea or RenParseDisplayMode.Strong;
        var accent = includesEmpty && includesOpponent ? new Color(255, 210, 96) : includesEmpty ? new Color(126, 255, 188) : new Color(255, 144, 126);
        var deferred = new List<(int RenNumber, int Value, Color Color, Color Outline)>();
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone == GoStone.Empty) continue;
            var boundary = BoundaryPoints.Collect(renParse, ren, includesEmpty, includesOpponent);
            var legThickness = MathHelper.Clamp(cell * 0.035f, 2f, 4f); var legColor = renderer.GetRenGraphCellColor(ren.Stone);
            if (showsAdjacentArea)
            {
                DrawAdjacentRelationships(renderer, renParse, boundary.Contacts, boundary.AdjacentRenNumbers, legColor, legThickness, start, cell);
                var value = displayMode == RenParseDisplayMode.Strong ? StrongAnalyzer.Analyze(renParse, ren.Number).Value : SumAdjacentAreas(renParse, boundary.AdjacentRenNumbers);
                if (displayMode == RenParseDisplayMode.Strong) { deferred.Add((ren.Number, value, legColor, renderer.GetRenGraphCellColor(OpponentOf(ren.Stone)))); continue; }
                renderer.DrawRenBoundaryPointMetric(ren, value, displayMode == RenParseDisplayMode.AdjacentEmptyArea ? renderer.GetRenGraphCellColor(GoStone.Empty) : renderer.GetRenGraphCellColor(OpponentOf(ren.Stone)), start, cell, null);
                continue;
            }
            DrawPointMarkers(renderer, renParse, boundary, legColor, legThickness, start, cell);
            var color = displayMode == RenParseDisplayMode.BoundaryCount ? legColor : displayMode == RenParseDisplayMode.BoundaryEmptyCount ? renderer.GetRenGraphCellColor(GoStone.Empty) : displayMode == RenParseDisplayMode.BoundaryOpponentCount ? renderer.GetRenGraphCellColor(OpponentOf(ren.Stone)) : accent;
            var outline = displayMode == RenParseDisplayMode.BoundaryCount ? renderer.GetRenGraphCellColor(OpponentOf(ren.Stone)) : (Color?)null;
            renderer.DrawRenBoundaryPointMetric(ren, boundary.Points.Count, color, start, cell, outline);
        }
        if (displayMode == RenParseDisplayMode.Strong) renderer.DrawDeferredStrongBoundaryMetrics(renParse, deferred, start, cell);
    }

    private static void DrawPointMarkers(BoardLensModel renderer, GoRenParseResult parse, RenBoundaryPointSet boundary, Color legColor, float legThickness, Vector2 start, float cell)
    {
        var originalRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f); var radius = Math.Max(2f, originalRadius - legThickness); var outerRadius = radius + 4f;
        var centers = new Dictionary<GoPoint, Vector2>();
        foreach (var point in boundary.Points)
        {
            var direction = boundary.DirectionSums[point]; if (direction.LengthSquared() < 0.01f) direction = boundary.FallbackDirections[point]; direction.Normalize();
            centers[point] = renderer.GetBoardPoint(start, cell, point.X, point.Y) + new Vector2(-direction.Y, direction.X) * (originalRadius + 4f) * 2f;
        }
        foreach (var contact in boundary.Contacts) renderer.DrawLine(renderer.GetBoardPoint(start, cell, contact.From.X, contact.From.Y), centers[contact.To], legThickness, legColor);
        foreach (var marker in centers)
        {
            var stone = parse.GetRen(parse.GetRenNumber(marker.Key.X, marker.Key.Y)).Stone;
            renderer.DrawCircle(marker.Value, Math.Max(1f, outerRadius - 1f), legColor); renderer.DrawCircle(marker.Value, radius, renderer.GetRenGraphCellColor(stone));
        }
    }

    private static void DrawAdjacentRelationships(BoardLensModel renderer, GoRenParseResult parse, List<(GoPoint From, GoPoint To)> contacts, HashSet<int> adjacentNumbers, Color legColor, float thickness, Vector2 start, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.13f, 5f, 11f); var innerHalf = Math.Max(2, (int)MathF.Round(radius - thickness)); var outerHalf = Math.Max(innerHalf + 2, (int)MathF.Round(radius + 3f - thickness));
        var sorted = new List<int>(adjacentNumbers); sorted.Sort();
        foreach (var number in sorted)
        {
            var contact = FindFirstContact(parse, contacts, number); var target = parse.GetRen(number);
            var source = renderer.GetBoardPoint(start, cell, contact.From.X, contact.From.Y); var boundary = renderer.GetBoardPoint(start, cell, contact.To.X, contact.To.Y);
            var direction = new Vector2(contact.From.X - contact.To.X, contact.From.Y - contact.To.Y); direction.Normalize(); var marker = boundary + new Vector2(-direction.Y, direction.X) * outerHalf * 2f;
            renderer.DrawLine(source, marker, thickness, legColor);
            renderer.FillRectangle(new Rectangle((int)MathF.Round(marker.X) - outerHalf, (int)MathF.Round(marker.Y) - outerHalf, outerHalf * 2, outerHalf * 2), legColor);
            renderer.FillRectangle(new Rectangle((int)MathF.Round(marker.X) - innerHalf, (int)MathF.Round(marker.Y) - innerHalf, innerHalf * 2, innerHalf * 2), renderer.GetRenGraphCellColor(target.Stone));
        }
    }
    private static (GoPoint From, GoPoint To) FindFirstContact(GoRenParseResult parse, List<(GoPoint From, GoPoint To)> contacts, int target)
    {
        (GoPoint From, GoPoint To)? selected = null;
        foreach (var contact in contacts)
        {
            if (parse.GetRenNumber(contact.To.X, contact.To.Y) != target) continue;
            if (selected is null || ComesFirst(contact, selected.Value)) selected = contact;
        }
        return selected ?? throw new InvalidOperationException("Adjacent ren has no boundary contact.");
    }
    private static bool ComesFirst((GoPoint From, GoPoint To) candidate, (GoPoint From, GoPoint To) current) => candidate.To.Y < current.To.Y ||
        (candidate.To.Y == current.To.Y && candidate.To.X < current.To.X) ||
        (candidate.To == current.To && (candidate.From.Y < current.From.Y || (candidate.From.Y == current.From.Y && candidate.From.X < current.From.X)));
    private static int SumAdjacentAreas(GoRenParseResult parse, HashSet<int> numbers) { var area = 0; foreach (var number in numbers) area += parse.GetRen(number).Points.Count; return area; }
    private static GoStone OpponentOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
}
