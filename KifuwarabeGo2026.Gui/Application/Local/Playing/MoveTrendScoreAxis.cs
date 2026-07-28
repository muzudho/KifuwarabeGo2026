namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using System;
using System.Collections.Generic;

/// <summary>
/// 着手評価値が収まる、上下対称のスコアY軸範囲を計算します。
/// </summary>
public static class MoveTrendScoreAxis
{
    public const double DefaultMaximum = 20.0;

    public static double CalculateMaximum(IReadOnlyList<MoveTrendPoint> points)
    {
        var maximumAbsoluteScore = 0.0;
        foreach (var point in points)
        {
            if (point.BlackPerspectiveScore is not { } score || !double.IsFinite(score))
            {
                continue;
            }

            maximumAbsoluteScore = Math.Max(maximumAbsoluteScore, Math.Abs(score));
        }

        if (maximumAbsoluteScore <= DefaultMaximum)
        {
            return DefaultMaximum;
        }

        // 最大値が枠線と重ならないよう5%の余白を持たせ、読みやすい10刻みに切り上げる。
        var paddedMaximum = maximumAbsoluteScore * 1.05;
        var roundedMaximum = Math.Ceiling(paddedMaximum / 10.0) * 10.0;
        return double.IsFinite(roundedMaximum)
            ? roundedMaximum
            : maximumAbsoluteScore;
    }
}
