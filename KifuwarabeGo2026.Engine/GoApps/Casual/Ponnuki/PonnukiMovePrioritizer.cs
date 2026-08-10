namespace KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;

using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Shared.BoardLens.Strong;

/// <summary>
/// Board Lens と同じ連解析を使い、ポン抜きプレイヤーの候補手を優先度付けします。
/// </summary>
internal static class PonnukiMovePrioritizer
{
    public static PonnukiMovePriority Evaluate(
        GoBoard boardAfterMove,
        GoPoint move,
        int capturedStones)
    {
        var renParse = boardAfterMove.ParseRens();
        var placedRen = renParse.GetRen(renParse.GetRenNumber(move.X, move.Y));
        var strong = StrongAnalyzer.Analyze(renParse, placedRen.Number);
        // 自連と隣接相手連の面積がちょうど拮抗した接触点を、逃げノビとして優先する。
        var evacuationNobiPriority = strong.TouchesOpponent && strong.Value == 0 ? 1 : 0;
        return new PonnukiMovePriority(capturedStones, evacuationNobiPriority);
    }

    public static List<GoPoint> SelectBest(IReadOnlyList<PonnukiMoveCandidate> candidates)
    {
        if (candidates.Count == 0)
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));

        var bestPriority = candidates[0].Priority;
        for (var i = 1; i < candidates.Count; i++)
        {
            if (candidates[i].Priority.CompareTo(bestPriority) > 0)
                bestPriority = candidates[i].Priority;
        }

        var bestMoves = new List<GoPoint>();
        foreach (var candidate in candidates)
        {
            if (candidate.Priority == bestPriority)
                bestMoves.Add(candidate.Move);
        }

        return bestMoves;
    }
}

internal readonly record struct PonnukiMoveCandidate(GoPoint Move, PonnukiMovePriority Priority);

/// <summary>
/// 優先順位を表す構造体です。捕獲石数と逃げノビ優先度の2つの要素で比較されます。
/// </summary>
/// <param name="CapturedStones"></param>
/// <param name="EvacuationNobiPriority"></param>
internal readonly record struct PonnukiMovePriority(int CapturedStones, int EvacuationNobiPriority)
    : IComparable<PonnukiMovePriority>
{
    public int CompareTo(PonnukiMovePriority other)
    {
        // （１）取った石の数の差
        var captureComparison = CapturedStones.CompareTo(other.CapturedStones);
        return captureComparison != 0
            ? captureComparison
            // （２）逃げノビ優先度の差
            : EvacuationNobiPriority.CompareTo(other.EvacuationNobiPriority);
    }
}
