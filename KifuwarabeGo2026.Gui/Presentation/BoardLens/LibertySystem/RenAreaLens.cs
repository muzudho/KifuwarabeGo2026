namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    /// <summary>REN AREA LENS の連ごとの面積表示です。</summary>
    private void DrawRenAreaNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            DrawRenMetricNumber(
                ren,
                ren.Points.Count,
                RenMetricUnit.PointCount,
                RenGraphCellColor(ren.Stone),
                start,
                cell,
                RenGraphCellColor(OpponentOf(ren.Stone)));
        }
    }
}
