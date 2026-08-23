namespace KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;

/// <summary>
/// 着手に付属する対局者視点の評価値を、チャート表示用の黒視点へ変換します。
/// </summary>
public static class MoveTrendSeriesBuilder
{
    public static IReadOnlyList<MoveTrendPoint> Build(IReadOnlyList<GoGameMove> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);

        var points = new List<MoveTrendPoint>(moves.Count);
        for (var index = 0; index < moves.Count; index++)
        {
            var move = moves[index];
            var analysis = move.Analysis;
            var perspective = move.Stone == GoStone.Black ? 1.0 : -1.0;
            double? score = analysis?.Score is { } rawScore && double.IsFinite(rawScore)
                ? rawScore * perspective
                : null;
            double? winAdvantage = analysis?.Winrate is { } rawWinrate &&
                                    double.IsFinite(rawWinrate) &&
                                    rawWinrate is >= 0.0 and <= 1.0
                ? (rawWinrate * 2.0 - 1.0) * perspective
                : null;

            points.Add(new MoveTrendPoint(index + 1, move.Stone, score, winAdvantage));
        }

        return points;
    }
}
