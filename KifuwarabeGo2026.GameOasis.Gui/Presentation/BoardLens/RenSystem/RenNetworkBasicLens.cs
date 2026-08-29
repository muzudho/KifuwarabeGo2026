namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.RenSystem;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed class RenNetworkBasicLens
{
    public static RenNetworkBasicLens Default { get; } = new();

    private RenNetworkBasicLens()
    {
    }

    public void DrawOverlay(BoardLensModel model, GoRenParseResult renParse, Rectangle boardBounds,
        Vector2 start, float cell, bool applyEyeJudgement)
    {
        var nodes = CreateRenGraphNodes(model, renParse, start, cell, applyEyeJudgement);
        model.FillRectangle(boardBounds, new Color(56, 145, 129));
        DrawRenGraphEdges(model, nodes, renParse.Edges, cell);
        DrawRenGraphNodes(model, nodes, cell);
    }

    public void DrawCells(BoardLensModel model, int boardSize, Func<int, int, GoStone> getStone, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        for (var y = 0; y < boardSize; y++)
        for (var x = 0; x < boardSize; x++)
        {
            var center = model.GetBoardPoint(start, cell, x, y);
            var rect = new Rectangle((int)MathF.Round(center.X - halfCell), (int)MathF.Round(center.Y - halfCell), (int)MathF.Ceiling(cell), (int)MathF.Ceiling(cell));
            model.FillRectangle(rect, model.GetRenGraphCellColor(getStone(x, y)));
        }
    }

    /// <summary>REN NETWORKのノードを生成します。</summary>
    private static RenGraphNode[] CreateRenGraphNodes(BoardLensModel model, GoRenParseResult renParse, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var sumX = new float[renParse.Count + 1];
        var sumY = new float[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++) for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            var center = model.GetBoardPoint(start, cell, x, y);
            sumX[number] += center.X; sumY[number] += center.Y;
        }
        var nodes = new RenGraphNode[renParse.Count + 1];
        for (var number = 1; number <= renParse.Count; number++)
        {
            var ren = renParse.GetRen(number);
            nodes[number] = new RenGraphNode(number, ren.Stone, new Vector2(sumX[number] / ren.Points.Count, sumY[number] / ren.Points.Count), !applyEyeJudgement || !ren.IsEye, applyEyeJudgement ? new List<int>(ren.EyeRenNumbers) : []);
        }
        return nodes;
    }

    /// <summary>REN NETWORK BASIC LENS のノードを描画します。</summary>
    private static void DrawRenGraphNodes(BoardLensModel model, RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = MathHelper.Clamp(cell / 120f, 0.18f, 0.46f);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
                continue;

            model.DrawCircle(node.Center, radius, model.GetRenGraphCellColor(node.Stone));
            model.DrawRenNumber(node.Number, node.Center, scale);
            DrawEyeMarkers(model, node, radius, scale);
        }
    }

    /// <summary>REN NETWORK BASIC LENS の接続線を描画します。</summary>
    private static void DrawRenGraphEdges(BoardLensModel model, RenGraphNode[] nodes, IReadOnlyList<GoRenGraphEdge> edges, float cell)
    {
        var thickness = MathHelper.Clamp(cell * 0.08f, 4f, 8f);
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
                continue;

            var from = nodes[edge.From];
            var to = nodes[edge.To];
            model.DrawLine(from.Center, to.Center, thickness, RenNetworkEdgeColor(model, from.Stone, to.Stone));
        }
    }

    private static Color RenNetworkEdgeColor(BoardLensModel model, GoStone from, GoStone to)
    {
        if (from == GoStone.Empty) return model.GetRenGraphCellColor(to);
        if (to == GoStone.Empty) return model.GetRenGraphCellColor(from);
        return new Color(66, 119, 145, 205);
    }

    private static void DrawEyeMarkers(BoardLensModel model, RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0) return;
        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;
        for (var index = 0; index < node.EyeNumbers.Count; index++)
        {
            var bounds = new Rectangle((int)MathF.Round(startX + index * spacing - markerSize * 0.5f),
                (int)MathF.Round(startY - markerSize * 0.5f), (int)MathF.Round(markerSize), (int)MathF.Round(markerSize));
            model.FillRectangle(bounds, new Color(255, 238, 0, 245));
            model.DrawRectangle(bounds, 2, new Color(255, 250, 220));
            model.DrawRenNumber(node.EyeNumbers[index], new Vector2(bounds.Center.X, bounds.Center.Y), markerScale);
        }
    }

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
