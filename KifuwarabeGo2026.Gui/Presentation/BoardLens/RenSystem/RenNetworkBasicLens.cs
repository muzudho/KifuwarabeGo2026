namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    private void DrawRenGraphCells(GoAppSession session, Vector2 start, float cell) =>
        DrawRenGraphCells(session.BoardSize, session.GetStone, start, cell);

    private void DrawRenGraphOverlay(GoAppSession session, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var renParse = session.ParseRens();
        var nodes = CreateRenGraphNodes(renParse, start, cell, applyEyeJudgement);
        FillRect(BoardBounds, new Color(56, 145, 129));
        DrawRenGraphEdges(nodes, renParse.Edges, cell);
        DrawRenGraphNodes(nodes, cell);
    }

    private void DrawRenGraphCells(int boardSize, Func<int, int, GoStone> getStone, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        for (var y = 0; y < boardSize; y++)
        for (var x = 0; x < boardSize; x++)
        {
            var center = BoardPoint(start, cell, x, y);
            var rect = new Rectangle((int)MathF.Round(center.X - halfCell), (int)MathF.Round(center.Y - halfCell), (int)MathF.Ceiling(cell), (int)MathF.Ceiling(cell));
            FillRect(rect, RenGraphCellColor(getStone(x, y)));
        }
    }

    /// <summary>REN NETWORKのノードを生成します。</summary>
    private RenGraphNode[] CreateRenGraphNodes(GoRenParseResult renParse, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var sumX = new float[renParse.Count + 1];
        var sumY = new float[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++) for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            var center = BoardPoint(start, cell, x, y);
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
    private void DrawRenGraphNodes(RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = RenNumberScale(cell);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
                continue;

            DrawCircle(node.Center, radius, RenGraphNodeColor(node.Stone));
            DrawRenNumber(node.Number, node.Center, scale);
            DrawRenGraphEyeMarkers(node, radius, scale);
        }
    }

    /// <summary>REN NETWORK BASIC LENS の接続線を描画します。</summary>
    private void DrawRenGraphEdges(RenGraphNode[] nodes, IReadOnlyList<GoRenGraphEdge> edges, float cell)
    {
        var thickness = MathHelper.Clamp(cell * 0.08f, 4f, 8f);
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
                continue;

            var from = nodes[edge.From];
            var to = nodes[edge.To];
            DrawLine(from.Center, to.Center, thickness, RenNetworkEdgeColor(from.Stone, to.Stone));
        }
    }

    private static Color RenNetworkEdgeColor(GoStone from, GoStone to)
    {
        if (from == GoStone.Empty) return RenGraphCellColor(to);
        if (to == GoStone.Empty) return RenGraphCellColor(from);
        return new Color(66, 119, 145, 205);
    }
}
