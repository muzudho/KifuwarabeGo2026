namespace KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;

using KifuwarabeGo2026.Engine.Shared;
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
        // 自連と隣接相手連の面積がちょうど拮抗した接触点を優先する。
        var contestedContactPriority = strong.TouchesOpponent && strong.Value == 0 ? 1 : 0;
        return new PonnukiMovePriority(capturedStones, contestedContactPriority);
    }

    public static List<PonnukiNobiCandidate> CollectPriorityOneNobiCandidates(
        GoBoard boardBeforeMove,
        GoStone color,
        IReadOnlyList<LegalMoveCandidate> legalMoves)
    {
        var renParse = boardBeforeMove.ParseRens();
        var nobiPoints = new HashSet<GoPoint>();
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone != color)
                continue;

            var ownBoundaryEmptyPoints = CollectBoundaryEmptyPoints(renParse, ren);
            var opponentBoundaryEmptyTotal = 0;
            foreach (var neighborRenNumber in ren.NeighborRenNumbers)
            {
                var neighbor = renParse.GetRen(neighborRenNumber);
                if (neighbor.Stone == GoStone.Empty || neighbor.Stone == color)
                    continue;

                opponentBoundaryEmptyTotal += CollectBoundaryEmptyPoints(renParse, neighbor).Count;
            }

            if (ownBoundaryEmptyPoints.Count == opponentBoundaryEmptyTotal)
                nobiPoints.UnionWith(ownBoundaryEmptyPoints);
        }

        var result = new List<PonnukiNobiCandidate>();
        foreach (var legalMove in legalMoves)
        {
            if (!nobiPoints.Contains(legalMove.Move))
                continue;

            var afterParse = legalMove.BoardAfterMove.ParseRens();
            var placedRen = afterParse.GetRen(afterParse.GetRenNumber(legalMove.Move.X, legalMove.Move.Y));
            result.Add(new PonnukiNobiCandidate(
                legalMove,
                CollectBoundaryEmptyPoints(afterParse, placedRen).Count));
        }

        return result;
    }

    public static List<PonnukiNobiCandidate> SelectMaximumBoundaryEmpty(
        IReadOnlyList<PonnukiNobiCandidate> candidates)
    {
        if (candidates.Count == 0)
            return [];

        var maximum = candidates.Max(candidate => candidate.BoundaryEmptyCountAfterMove);
        return candidates
            .Where(candidate => candidate.BoundaryEmptyCountAfterMove == maximum)
            .ToList();
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

    private static HashSet<GoPoint> CollectBoundaryEmptyPoints(GoRenParseResult renParse, GoRen ren)
    {
        var points = new HashSet<GoPoint>();
        foreach (var point in ren.Points)
        {
            AddIfEmpty(point.X - 1, point.Y);
            AddIfEmpty(point.X + 1, point.Y);
            AddIfEmpty(point.X, point.Y - 1);
            AddIfEmpty(point.X, point.Y + 1);
        }

        return points;

        void AddIfEmpty(int x, int y)
        {
            if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size)
                return;
            if (renParse.GetRen(renParse.GetRenNumber(x, y)).Stone == GoStone.Empty)
                points.Add(new GoPoint(x, y));
        }
    }
}

internal readonly record struct PonnukiMoveCandidate(GoPoint Move, PonnukiMovePriority Priority);

internal readonly record struct PonnukiNobiCandidate(
    LegalMoveCandidate LegalMove,
    int BoundaryEmptyCountAfterMove);

/// <summary>
/// 優先順位を表す構造体です。捕獲石数と拮抗接触優先度の2つの要素で比較されます。
/// </summary>
/// <param name="CapturedStones"></param>
/// <param name="ContestedContactPriority"></param>
internal readonly record struct PonnukiMovePriority(int CapturedStones, int ContestedContactPriority)
    : IComparable<PonnukiMovePriority>
{
    public int CompareTo(PonnukiMovePriority other)
    {
        // （１）取った石の数の差
        var captureComparison = CapturedStones.CompareTo(other.CapturedStones);
        return captureComparison != 0
            ? captureComparison
            // （２）拮抗接触優先度の差
            : ContestedContactPriority.CompareTo(other.ContestedContactPriority);
    }
}
