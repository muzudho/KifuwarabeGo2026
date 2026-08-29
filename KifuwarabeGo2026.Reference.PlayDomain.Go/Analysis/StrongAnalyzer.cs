namespace KifuwarabeGo2026.Reference.PlayDomain.Go.Analysis;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;
using System.Collections.Generic;

/// <summary>一つの石連に対する Strong Lens の解析結果です。</summary>
public readonly record struct StrongAnalysis(
    int RenNumber,
    GoStone Stone,
    int OwnArea,
    int AdjacentOpponentArea,
    int AdjacentOpponentRenCount)
{
    /// <summary>自連の面積から隣接相手連の面積合計を引いた評価値です。</summary>
    public int Value => OwnArea - AdjacentOpponentArea;

    /// <summary>少なくとも一つの相手連に接触しているかを示します。</summary>
    public bool TouchesOpponent => AdjacentOpponentRenCount > 0;
}

/// <summary>
/// Strong Lens の盤面解析です。
/// GUI の表示とエンジンの評価で同じ「自連面積 − 隣接相手連面積」を使います。
/// </summary>
public static class StrongAnalyzer
{
    public static StrongAnalysis Analyze(GoRenParseResult renParse, int renNumber)
    {
        ArgumentNullException.ThrowIfNull(renParse);

        var ren = renParse.GetRen(renNumber);
        var adjacentOpponentArea = 0;
        var adjacentOpponentRenCount = 0;
        var visited = new HashSet<int>();
        foreach (var neighborRenNumber in ren.NeighborRenNumbers)
        {
            if (!visited.Add(neighborRenNumber))
                continue;

            var neighbor = renParse.GetRen(neighborRenNumber);
            if (neighbor.Stone == GoStone.Empty || neighbor.Stone == ren.Stone)
                continue;

            adjacentOpponentArea += neighbor.Points.Count;
            adjacentOpponentRenCount++;
        }

        return new StrongAnalysis(
            ren.Number,
            ren.Stone,
            ren.Points.Count,
            adjacentOpponentArea,
            adjacentOpponentRenCount);
    }
}
