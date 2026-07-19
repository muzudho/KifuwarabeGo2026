namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
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
        if (displayMode == RenParseDisplayMode.Graph)
        {
            var renParse = parseRens();
            DrawRenGraphCells(boardSize, getStone, start, cell);
            DrawRenBoundaries(renParse, start, cell);
            DrawRenRepresentativeNumbers(renParse, start, cell);
            return;
        }

        if (displayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            var renParse = parseRens();
            var nodes = CreateRenGraphNodes(renParse, start, cell, displayMode == RenParseDisplayMode.Eye);
            FillRect(BoardBounds, new Color(56, 145, 129));
            DrawRenGraphEdges(nodes, renParse.Edges, cell);
            DrawRenGraphNodes(nodes, cell);
            return;
        }

        drawPlacedStones();
        if (displayMode != RenParseDisplayMode.Overlay) return;

        var overlayRenParse = parseRens();
        DrawRenBoundaries(overlayRenParse, start, cell);
        DrawRenNumbers(overlayRenParse, start, cell);
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
        if (session.RenParseDisplayMode != RenParseDisplayMode.Overlay)
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

