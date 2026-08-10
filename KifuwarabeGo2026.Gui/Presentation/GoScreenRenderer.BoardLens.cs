namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

/// <summary>
/// 囲碁盤上の Board Lens 表示を描画します。
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
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
            {
                continue;
            }

            var from = nodes[edge.From];
            var to = nodes[edge.To];
            DrawLine(
                from.Center,
                to.Center,
                thickness,
                RenNetworkEdgeColor(from.Stone, to.Stone));
        }
    }


    private static Color RenNetworkEdgeColor(GoStone from, GoStone to)
    {
        if (from == GoStone.Empty)
            return RenGraphCellColor(to);

        if (to == GoStone.Empty)
            return RenGraphCellColor(from);

        return new Color(66, 119, 145, 205);
    }


    /// <summary>
    /// ［連グラフ・ノード］描画
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphNodes(RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = RenNumberScale(cell);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
            {
                continue;
            }

            DrawCircle(node.Center, radius, RenGraphNodeColor(node.Stone));
            DrawRenNumber(node.Number, node.Center, scale);
            DrawRenGraphEyeMarkers(node, radius, scale);
        }
    }


    /// <summary>
    /// ［連グラフ・目マーカー］描画
    /// </summary>
    /// <param name="node"></param>
    /// <param name="radius"></param>
    /// <param name="scale"></param>
    /// <summary>
    /// ［連境界］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <summary>
    /// ［連番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>


    /// <summary>
    /// ［連代表番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>


    /// <summary>
    /// 接触している辺はすべて足として描き、終点と集計値だけを連ごとに重複除去します。
    /// </summary>
    private void DrawRenBoundaryLens(
        GoRenParseResult renParse,
        RenParseDisplayMode displayMode,
        Vector2 start,
        float cell)
    {
        if (displayMode == RenParseDisplayMode.Nobi)
            return;

        var includesEmpty = displayMode is
            RenParseDisplayMode.BoundaryCount or
            RenParseDisplayMode.BoundaryEmptyCount or
            RenParseDisplayMode.AdjacentEmptyArea;
        var includesOpponent = displayMode is
            RenParseDisplayMode.BoundaryCount or
            RenParseDisplayMode.BoundaryOpponentCount or
            RenParseDisplayMode.AdjacentOpponentArea or
            RenParseDisplayMode.Strong;
        var showsAdjacentArea = displayMode is
            RenParseDisplayMode.AdjacentEmptyArea or
            RenParseDisplayMode.AdjacentOpponentArea or
            RenParseDisplayMode.Strong;
        var accent = includesEmpty && includesOpponent
            ? new Color(255, 210, 96)
            : includesEmpty
                ? new Color(126, 255, 188)
                : new Color(255, 144, 126);
        var deferredStrongMetrics = new List<(int RenNumber, int Value, Color Color, Color Outline)>();

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

            var legThickness = MathHelper.Clamp(cell * 0.035f, 2f, 4f);
            var legColor = RenGraphCellColor(ren.Stone);
            if (showsAdjacentArea)
            {
                DrawAdjacentRenRelationships(
                    renParse,
                    contacts,
                    adjacentRenNumbers,
                    legColor,
                    legThickness,
                    start,
                    cell);
                var adjacentArea = SumAdjacentRenAreas(renParse, adjacentRenNumbers);
                var adjacentValue = displayMode == RenParseDisplayMode.Strong ? ren.Points.Count - adjacentArea : adjacentArea;
                if (displayMode == RenParseDisplayMode.Strong)
                {
                    deferredStrongMetrics.Add((ren.Number, adjacentValue, RenGraphCellColor(ren.Stone), RenGraphCellColor(OpponentOf(ren.Stone))));
                    continue;
                }
                DrawRenMetricNumber(
                    ren,
                    adjacentValue,
                    RenMetricUnit.PointCount,
                    displayMode == RenParseDisplayMode.AdjacentEmptyArea
                        ? RenGraphCellColor(GoStone.Empty)
                        : RenGraphCellColor(OpponentOf(ren.Stone)),
                    start,
                    cell,
                    null);
                continue;
            }

            var originalMarkerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
            var originalOuterMarkerRadius = originalMarkerRadius + 4f;
            var markerRadius = Math.Max(2f, originalMarkerRadius - legThickness);
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
                markerCenters[point] = boundaryCenter + (clockwiseDirection * originalOuterMarkerRadius * 2f);
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
                DrawCircle(marker.Value, Math.Max(1f, outerMarkerRadius - 1f), legColor);
                DrawCircle(marker.Value, markerRadius, RenGraphCellColor(targetStone));
            }

            var value = boundaryPoints.Count;
            var valueColor = displayMode == RenParseDisplayMode.BoundaryCount
                ? RenGraphCellColor(ren.Stone)
                : displayMode is RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.AdjacentEmptyArea
                    ? RenGraphCellColor(GoStone.Empty)
                    : displayMode == RenParseDisplayMode.BoundaryOpponentCount
                        ? RenGraphCellColor(OpponentOf(ren.Stone))
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

        if (displayMode == RenParseDisplayMode.Strong)
            DrawDeferredStrongMetrics(renParse, deferredStrongMetrics, start, cell);
    }

    /// <summary>
    /// ［連グラフ・ノード色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>


    /// <summary>
    /// ［連グラフ・セル色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>


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

