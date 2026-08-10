namespace KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Casual App のポン抜き用着手生成です。</summary>
internal sealed class PonnukiStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        if (legalMoves.Count == 0)
            return null;

        var prioritizedMoves = new List<PonnukiMoveCandidate>(legalMoves.Count);
        foreach (var candidate in legalMoves)
        {
            prioritizedMoves.Add(new PonnukiMoveCandidate(
                candidate.Move,
                PonnukiMovePrioritizer.Evaluate(
                    candidate.BoardAfterMove,
                    candidate.Move,
                    candidate.CapturedStones)));
        }

        return MoveSelector.Select(PonnukiMovePrioritizer.SelectBest(prioritizedMoves), request);
    }
}
