namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    /// <summary>STRONG LENS の評価値を、足とマーカーの前景に描画します。</summary>
    private void DrawDeferredStrongMetrics(
        GoRenParseResult renParse,
        List<(int RenNumber, int Value, Color Color, Color Outline)> metrics,
        Vector2 start,
        float cell)
    {
        foreach (var metric in metrics)
            DrawRenMetricNumber(renParse.GetRen(metric.RenNumber), metric.Value, RenMetricUnit.PointCount, metric.Color, start, cell, metric.Outline);
    }
}
