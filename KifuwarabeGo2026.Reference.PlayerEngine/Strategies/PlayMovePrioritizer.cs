namespace KifuwarabeGo2026.Reference.PlayerEngine.Strategies;

using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;
using System.Linq;

/// <summary>Formal Play 用の着手優先順位を収集します。</summary>
public static class PlayMovePrioritizer
{
    /// <summary>相手の連がアタリなら、その唯一の呼吸点へ打つ候補を返します。</summary>
    public static List<LegalMoveCandidate> CollectAtariCaptureCandidates(
        GoBoard boardBeforeMove,
        GoStone color,
        IReadOnlyList<LegalMoveCandidate> legalMoves)
    {
        var renParse = boardBeforeMove.ParseRens();
        var capturePoints = new HashSet<GoPoint>();
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone == GoStone.Empty || ren.Stone == color)
                continue;

            var liberties = CollectLiberties(renParse, ren);
            if (liberties.Count == 1)
                capturePoints.UnionWith(liberties);
        }

        return legalMoves.Where(candidate => capturePoints.Contains(candidate.Move)).ToList();
    }

    /// <summary>
    /// 自分の連がアタリのとき、その唯一の呼吸点へ打つ防御候補を返します。
    /// 着手後にその連の呼吸点が 2 以上になる候補だけを対象にします。
    /// </summary>
    public static List<LegalMoveCandidate> CollectThreatenedRenDefenseCandidates(
        GoBoard boardBeforeMove,
        GoStone color,
        IReadOnlyList<LegalMoveCandidate> legalMoves)
    {
        var renParse = boardBeforeMove.ParseRens();
        var defensePoints = new HashSet<GoPoint>();
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone != color)
                continue;

            var liberties = CollectLiberties(renParse, ren);
            if (liberties.Count == 1)
                defensePoints.UnionWith(liberties);
        }

        return legalMoves
            .Where(candidate => defensePoints.Contains(candidate.Move))
            .Where(candidate => CountPlacedRenLiberties(candidate) >= 2)
            .ToList();
    }

    private static int CountPlacedRenLiberties(LegalMoveCandidate candidate)
    {
        var afterParse = candidate.BoardAfterMove.ParseRens();
        var placedRen = afterParse.GetRen(afterParse.GetRenNumber(candidate.Move.X, candidate.Move.Y));
        return CollectLiberties(afterParse, placedRen).Count;
    }

    private static HashSet<GoPoint> CollectLiberties(GoRenParseResult renParse, GoRen ren)
    {
        var result = new HashSet<GoPoint>();
        foreach (var point in ren.Points)
        {
            AddIfEmpty(point.X - 1, point.Y);
            AddIfEmpty(point.X + 1, point.Y);
            AddIfEmpty(point.X, point.Y - 1);
            AddIfEmpty(point.X, point.Y + 1);
        }
        return result;

        void AddIfEmpty(int x, int y)
        {
            if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size)
                return;
            if (renParse.GetRen(renParse.GetRenNumber(x, y)).Stone == GoStone.Empty)
                result.Add(new GoPoint(x, y));
        }
    }
}
