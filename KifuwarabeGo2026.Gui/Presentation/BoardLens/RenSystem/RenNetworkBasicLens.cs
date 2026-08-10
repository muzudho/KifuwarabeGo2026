namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
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
}
