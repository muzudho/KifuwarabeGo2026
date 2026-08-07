namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

/// <summary>
/// 囲碁盤上の連解析表示を描画します。
/// </summary>
public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// 盤面の供給元に依存せず、指定された連解析表示を描画します。
    /// </summary>
    private void DrawBoardRenAnalysis(
        RenParseDisplayMode displayMode,
        int boardSize,
        Func<int, int, GoStone> getStone,
        Func<GoRenParseResult> parseRens,
        Action drawPlacedStones,
        Vector2 start,
        float cell)
    {
        if (displayMode == RenParseDisplayMode.Off)
        {
            drawPlacedStones();
            return;
        }

        var renParse = parseRens();

        if (displayMode == RenParseDisplayMode.Overlay)
        {
            drawPlacedStones();
            DrawRenBoundaries(renParse, start, cell);
            DrawRenNumbers(renParse, start, cell);
            return;
        }

        if (displayMode == RenParseDisplayMode.Graph)
        {
            DrawRenGraphCells(boardSize, getStone, start, cell);
            DrawRenBoundaries(renParse, start, cell);
            DrawRenRepresentativeNumbers(renParse, start, cell);
            return;
        }

        if (displayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            var nodes = CreateRenGraphNodes(renParse, start, cell, displayMode == RenParseDisplayMode.Eye);
            FillRect(BoardBounds, new Color(56, 145, 129));
            DrawRenGraphEdges(nodes, renParse.Edges, cell);
            DrawRenGraphNodes(nodes, cell);
            return;
        }

        DrawRenGraphCells(boardSize, getStone, start, cell);
        DrawRenBoundaries(renParse, start, cell);

        if (displayMode == RenParseDisplayMode.RenArea)
        {
            DrawRenAreaNumbers(renParse, start, cell);
            return;
        }

        DrawRenBoundaryLens(renParse, displayMode, start, cell);
    }

    /// <summary>
    /// ［連］のグラフノード作成
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="applyEyeJudgement"></param>
    /// <returns></returns>
    private RenGraphNode[] CreateRenGraphNodes(GoRenParseResult renParse, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var sumX = new float[renParse.Count + 1];
        var sumY = new float[renParse.Count + 1];

        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                sumX[renNumber] += center.X;
                sumY[renNumber] += center.Y;
            }
        }

        var nodes = new RenGraphNode[renParse.Count + 1];
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            nodes[renNumber] = new RenGraphNode(
                renNumber,
                ren.Stone,
                new Vector2(sumX[renNumber] / ren.Points.Count, sumY[renNumber] / ren.Points.Count),
                !applyEyeJudgement || !ren.IsEye,
                applyEyeJudgement ? new List<int>(ren.EyeRenNumbers) : []);
        }

        return nodes;
    }


    /// <summary>
    /// ［連パース・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenParseOverlay(GoAppSession session, Vector2 start, float cell)
    {
        if (session.RenParseDisplayMode != RenParseDisplayMode.RenArea)
        {
            return;
        }

        var renParse = session.ParseRens();
        DrawRenBoundaries(renParse, start, cell);
        DrawRenNumbers(renParse, start, cell);
    }


    /// <summary>
    /// ［連グラフ・ステップ１・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphStep1Overlay(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        DrawRenGraphCells(session, start, cell);
        DrawRenBoundaries(renParse, start, cell);
        DrawRenRepresentativeNumbers(renParse, start, cell);
    }


    /// <summary>
    /// ［連グラフ・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="applyEyeJudgement"></param>

    private void DrawRenGraphOverlay(GoAppSession session, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var renParse = session.ParseRens();
        var nodes = CreateRenGraphNodes(renParse, start, cell, applyEyeJudgement);

        FillRect(BoardBounds, new Color(56, 145, 129));
        DrawRenGraphEdges(nodes, renParse.Edges, cell);
        DrawRenGraphNodes(nodes, cell);
    }


    /// <summary>
    /// ［連グラフ・セル］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphCells(GoAppSession session, Vector2 start, float cell)
    {
        DrawRenGraphCells(session.BoardSize, session.GetStone, start, cell);
    }

    /// <summary>
    /// 盤面の供給元に依存せず［連グラフ・セル］を描画します。
    /// </summary>
    private void DrawRenGraphCells(int boardSize, Func<int, int, GoStone> getStone, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        for (var y = 0; y < boardSize; y++)
        {
            for (var x = 0; x < boardSize; x++)
            {
                var center = BoardPoint(start, cell, x, y);
                var rect = new Rectangle(
                    (int)MathF.Round(center.X - halfCell),
                    (int)MathF.Round(center.Y - halfCell),
                    (int)MathF.Ceiling(cell),
                    (int)MathF.Ceiling(cell));
                FillRect(rect, RenGraphCellColor(getStone(x, y)));
            }
        }
    }


    /// <summary>
    /// ［連グラフ・エッジ］描画
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="edges"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphEdges(RenGraphNode[] nodes, IReadOnlyList<GoRenGraphEdge> edges, float cell)
    {
        var thickness = MathHelper.Clamp(cell * 0.08f, 4f, 8f);
        var color = new Color(70, 70, 220, 230);
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
            {
                continue;
            }

            DrawLine(nodes[edge.From].Center, nodes[edge.To].Center, thickness, color);
        }
    }


    /// <summary>
    /// ［連グラフ・ノード］描画
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphNodes(RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = MathHelper.Clamp(cell / 72f, 0.34f, 0.84f);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
            {
                continue;
            }

            DrawCircle(node.Center, radius, RenGraphNodeColor(node.Stone));
            DrawCenteredText(node.Number.ToString(), node.Center, new Color(0, 177, 238), scale);
            DrawRenGraphEyeMarkers(node, radius, scale);
        }
    }


    /// <summary>
    /// ［連グラフ・目マーカー］描画
    /// </summary>
    /// <param name="node"></param>
    /// <param name="radius"></param>
    /// <param name="scale"></param>
    private void DrawRenGraphEyeMarkers(RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0)
        {
            return;
        }

        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;

        for (var i = 0; i < node.EyeNumbers.Count; i++)
        {
            var markerBounds = new Rectangle(
                (int)MathF.Round(startX + (i * spacing) - (markerSize * 0.5f)),
                (int)MathF.Round(startY - (markerSize * 0.5f)),
                (int)MathF.Round(markerSize),
                (int)MathF.Round(markerSize));
            FillRect(markerBounds, new Color(255, 238, 0, 245));
            DrawRect(markerBounds, 2, new Color(255, 250, 220));
            DrawCenteredText(node.EyeNumbers[i].ToString(), new Vector2(markerBounds.Center.X, markerBounds.Center.Y), new Color(56, 94, 120), markerScale);
        }
    }


    /// <summary>
    /// ［連境界］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var size = renParse.Size;
        var halfCell = cell * 0.5f;
        var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f));
        var color = new Color(255, 238, 0, 238);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                var left = center.X - halfCell;
                var top = center.Y - halfCell;
                var right = center.X + halfCell;
                var bottom = center.Y + halfCell;

                if (x == 0 || renParse.GetRenNumber(x - 1, y) != renNumber)
                {
                    FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
                }

                if (y == 0 || renParse.GetRenNumber(x, y - 1) != renNumber)
                {
                    FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
                }

                if (x == size - 1)
                {
                    FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
                }

                if (y == size - 1)
                {
                    FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
                }
            }
        }
    }


    /// <summary>
    /// ［連番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var label = renParse.GetRenNumber(x, y).ToString();
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(label, center, new Color(0, 177, 238), scale);
            }
        }
    }


    /// <summary>
    /// ［連代表番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                if (drawn[renNumber])
                {
                    continue;
                }

                drawn[renNumber] = true;
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(renNumber.ToString(), center, new Color(0, 177, 238), scale);
            }
        }
    }


    private void DrawRenAreaNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            DrawRenMetricNumber(ren, ren.Points.Count, RenMetricUnit.PointCount, new Color(126, 255, 188), start, cell);
        }
    }


    /// <summary>
    /// 接触している辺はすべて足として描き、終点と集計値だけを連ごとに重複除去します。
    /// </summary>
    private void DrawRenBoundaryLens(
        GoRenParseResult renParse,
        RenParseDisplayMode displayMode,
        Vector2 start,
        float cell)
    {
        var includesEmpty = displayMode is
            RenParseDisplayMode.BoundaryCount or
            RenParseDisplayMode.BoundaryEmptyCount or
            RenParseDisplayMode.AdjacentEmptyArea;
        var includesOpponent = displayMode is
            RenParseDisplayMode.BoundaryCount or
            RenParseDisplayMode.BoundaryOpponentCount or
            RenParseDisplayMode.AdjacentOpponentArea;
        var showsAdjacentArea = displayMode is
            RenParseDisplayMode.AdjacentEmptyArea or
            RenParseDisplayMode.AdjacentOpponentArea;
        var accent = includesEmpty && includesOpponent
            ? new Color(255, 210, 96)
            : includesEmpty
                ? new Color(126, 255, 188)
                : new Color(255, 144, 126);

        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone == GoStone.Empty)
                continue;

            var contacts = new List<(GoPoint From, GoPoint To)>();
            var boundaryPoints = new HashSet<GoPoint>();
            var boundaryDirectionSums = new Dictionary<GoPoint, Vector2>();
            var boundaryFallbackDirections = new Dictionary<GoPoint, Vector2>();
            var adjacentRenNumbers = new HashSet<int>();
            foreach (var point in ren.Points)
            {
                AddContact(point, point.X - 1, point.Y);
                AddContact(point, point.X + 1, point.Y);
                AddContact(point, point.X, point.Y - 1);
                AddContact(point, point.X, point.Y + 1);
            }

            if (showsAdjacentArea)
            {
                foreach (var adjacentRenNumber in adjacentRenNumbers)
                    DrawAdjacentRenHighlight(renParse.GetRen(adjacentRenNumber), accent, start, cell);
            }

            var legThickness = MathHelper.Clamp(cell * 0.035f, 2f, 4f);
            var legColor = RenGraphCellColor(ren.Stone);
            var markerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
            var outerMarkerRadius = markerRadius + 4f;
            var markerCenters = new Dictionary<GoPoint, Vector2>();
            foreach (var point in boundaryPoints)
            {
                var boundaryCenter = BoardPoint(start, cell, point.X, point.Y);
                var sourceDirection = boundaryDirectionSums[point];
                if (sourceDirection.LengthSquared() < 0.01f)
                    sourceDirection = boundaryFallbackDirections[point];
                sourceDirection.Normalize();

                // 隣点を中心に、足が来た方向を時計回りへ90度回した側へ逃がします。
                var clockwiseDirection = new Vector2(-sourceDirection.Y, sourceDirection.X);
                markerCenters[point] = boundaryCenter + (clockwiseDirection * outerMarkerRadius * 2f);
            }

            // 隣点で折り曲げず、各始点から退避済みマーカーへ直接つなぎます。
            foreach (var contact in contacts)
            {
                var from = BoardPoint(start, cell, contact.From.X, contact.From.Y);
                var to = markerCenters[contact.To];
                DrawLine(
                    from,
                    to,
                    legThickness,
                    legColor);
            }

            foreach (var marker in markerCenters)
            {
                var targetStone = renParse.GetRen(renParse.GetRenNumber(marker.Key.X, marker.Key.Y)).Stone;
                DrawCircle(marker.Value, outerMarkerRadius, legColor);
                DrawCircle(marker.Value, markerRadius, RenGraphCellColor(targetStone));
            }

            var value = showsAdjacentArea
                ? SumAdjacentRenAreas(renParse, adjacentRenNumbers)
                : boundaryPoints.Count;
            var valueColor = displayMode == RenParseDisplayMode.BoundaryCount
                ? RenGraphCellColor(ren.Stone)
                : displayMode == RenParseDisplayMode.BoundaryEmptyCount
                    ? RenGraphCellColor(GoStone.Empty)
                : accent;
            var valueOutlineColor = displayMode == RenParseDisplayMode.BoundaryCount
                ? RenGraphCellColor(OpponentOf(ren.Stone))
                : (Color?)null;
            DrawRenMetricNumber(
                ren,
                value,
                RenMetricUnit.PointCount,
                valueColor,
                start,
                cell,
                valueOutlineColor);

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
                var sourceDirection = new Vector2(from.X - x, from.Y - y);
                if (boundaryDirectionSums.TryGetValue(target, out var directionSum))
                    boundaryDirectionSums[target] = directionSum + sourceDirection;
                else
                    boundaryDirectionSums[target] = sourceDirection;
                boundaryFallbackDirections.TryAdd(target, sourceDirection);
                adjacentRenNumbers.Add(targetRenNumber);
            }
        }
    }


    private void DrawAdjacentRenHighlight(GoRen ren, Color accent, Vector2 start, float cell)
    {
        var inset = Math.Max(4, (int)MathF.Round(cell * 0.16f));
        var size = Math.Max(6, (int)MathF.Round(cell) - (inset * 2));
        foreach (var point in ren.Points)
        {
            var center = BoardPoint(start, cell, point.X, point.Y);
            var bounds = new Rectangle(
                (int)MathF.Round(center.X - (cell * 0.5f)) + inset,
                (int)MathF.Round(center.Y - (cell * 0.5f)) + inset,
                size,
                size);
            FillRect(bounds, new Color((int)accent.R, accent.G, accent.B, 72));
            DrawRect(bounds, 2, new Color((int)accent.R, accent.G, accent.B, 210));
        }
    }


    private static int SumAdjacentRenAreas(GoRenParseResult renParse, HashSet<int> adjacentRenNumbers)
    {
        var area = 0;
        foreach (var renNumber in adjacentRenNumbers)
            area += renParse.GetRen(renNumber).Points.Count;
        return area;
    }


    private void DrawRenMetricNumber(
        GoRen ren,
        int value,
        RenMetricUnit unit,
        Color valueColor,
        Vector2 start,
        float cell,
        Color? valueOutlineColor = null)
    {
        var representative = ren.Points[0];
        var center = BoardPoint(start, cell, representative.X, representative.Y);
        var indexScale = MathHelper.Clamp(cell / 120f, 0.18f, 0.46f);
        var valueScale = MathHelper.Clamp(cell / 68f, 0.34f, 0.80f);
        DrawCenteredOutlinedText(
            $"#{ren.Number}",
            center - new Vector2(0f, cell * 0.20f),
            new Color(0, 177, 238),
            new Color(0, 92, 132, 245),
            indexScale);
        var valueCenter = center + new Vector2(0f, cell * 0.10f);
        if (valueOutlineColor is { } outlineColor)
            DrawCenteredOutlinedText(value.ToString(), valueCenter, valueColor, outlineColor, valueScale);
        else
            DrawCenteredText(value.ToString(), valueCenter, valueColor, valueScale);
        DrawRenMetricUnit(
            center + new Vector2(0f, cell * 0.37f),
            unit,
            valueColor,
            cell,
            valueOutlineColor);
    }


    private void DrawCenteredOutlinedText(
        string text,
        Vector2 center,
        Color color,
        Color outlineColor,
        float scale)
    {
        var size = _font.MeasureString(text) * scale;
        var position = new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f);
        var outline = MathHelper.Clamp(scale * 7f, 1.5f, 3f);
        const int outlineSamples = 16;
        for (var i = 0; i < outlineSamples; i++)
        {
            var angle = MathHelper.TwoPi * i / outlineSamples;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * outline;
            _spriteBatch.DrawString(_font, text, position + offset, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }


    private void DrawRenMetricUnit(
        Vector2 center,
        RenMetricUnit unit,
        Color color,
        float cell,
        Color? outlineColor = null)
    {
        var radius = MathHelper.Clamp(cell * 0.075f, 3f, 6f);
        var thickness = Math.Max(2, (int)MathF.Round(radius * 0.42f));
        var backing = new Color(16, 26, 32, 220);
        if (unit == RenMetricUnit.PointCount)
        {
            DrawCircle(center, radius + thickness, outlineColor ?? color);
            DrawCircle(center, radius, outlineColor is null ? backing : color);
            if (outlineColor is not null)
                DrawCircle(center, Math.Max(1f, radius - thickness), backing);
            return;
        }

        var extent = (int)MathF.Round(radius + thickness);
        var bounds = new Rectangle(
            (int)MathF.Round(center.X) - extent,
            (int)MathF.Round(center.Y) - extent,
            extent * 2,
            extent * 2);
        FillRect(bounds, backing);
        DrawRect(bounds, thickness, color);
    }


    private static GoStone OpponentOf(GoStone stone) => stone == GoStone.Black
        ? GoStone.White
        : GoStone.Black;


    private enum RenMetricUnit
    {
        PointCount,
        RenCount,
    }


    /// <summary>
    /// ［連グラフ・ノード色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>
    private static Color RenGraphNodeColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };


    /// <summary>
    /// ［連グラフ・セル色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>
    private static Color RenGraphCellColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };


    /// <summary>
    /// ［連グラフノード］
    /// </summary>
    private sealed class RenGraphNode
    {
        public RenGraphNode(int number, GoStone stone, Vector2 center, bool isVisible, List<int> eyeNumbers)
        {
            Number = number;
            Stone = stone;
            Center = center;
            IsVisible = isVisible;
            EyeNumbers = eyeNumbers;
        }

        public int Number { get; }

        public GoStone Stone { get; }

        public Vector2 Center { get; }

        public bool IsVisible { get; set; }

        public List<int> EyeNumbers { get; }
    }
}

