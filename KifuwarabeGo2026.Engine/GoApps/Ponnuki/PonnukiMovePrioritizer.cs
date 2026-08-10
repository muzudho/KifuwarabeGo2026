namespace KifuwarabeGo2026.Engine.GoApps.Ponnuki;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Board Lens と同じ連解析を使い、ポン抜きプレイヤーの候補手を優先度付けします。
/// </summary>
internal static class PonnukiMovePrioritizer
{
    public static PonnukiMovePriority Evaluate(
        GoBoard boardAfterMove,
        GoPoint move,
        GoStone color,
        int capturedStones)
    {
        var renParse = boardAfterMove.ParseRens();
        var placedRen = renParse.GetRen(renParse.GetRenNumber(move.X, move.Y));
        var adjacentOpponentArea = 0;
        var touchesOpponent = false;
        foreach (var adjacentRenNumber in placedRen.NeighborRenNumbers)
        {
            var adjacentRen = renParse.GetRen(adjacentRenNumber);
            if (adjacentRen.Stone == color || adjacentRen.Stone == GoStone.Empty)
                continue;

            touchesOpponent = true;
            adjacentOpponentArea += adjacentRen.Points.Count;
        }

        var contactPriority = !touchesOpponent
            ? 0
            : placedRen.Points.Count > adjacentOpponentArea ? 1 : -1;
        return new PonnukiMovePriority(capturedStones, contactPriority);
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

internal readonly record struct PonnukiMovePriority(int CapturedStones, int ContactPriority)
    : IComparable<PonnukiMovePriority>
{
    public int CompareTo(PonnukiMovePriority other)
    {
        var captureComparison = CapturedStones.CompareTo(other.CapturedStones);
        return captureComparison != 0
            ? captureComparison
            : ContactPriority.CompareTo(other.ContactPriority);
    }
}
