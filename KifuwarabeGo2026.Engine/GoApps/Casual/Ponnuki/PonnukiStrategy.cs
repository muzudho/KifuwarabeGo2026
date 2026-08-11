namespace KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Casual App のポン抜き用着手生成です。</summary>
internal sealed class PonnukiStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request)
        => GenerateMoveWithDecision(request)?.Move;

    public PonnukiMoveDecision? GenerateMoveWithDecision(GenerateMoveRequest request)
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

        var bestMoves = PonnukiMovePrioritizer.SelectBest(prioritizedMoves);
        var move = MoveSelector.Select(bestMoves, request);
        var selected = prioritizedMoves.Find(candidate => candidate.Move == move);
        return new PonnukiMoveDecision(move, selected.Priority, bestMoves.Count, request.SelectionMode);
    }
}

internal readonly record struct PonnukiMoveDecision(
    GoPoint Move,
    PonnukiMovePriority Priority,
    int SamePriorityCandidateCount,
    MoveSelectionMode SelectionMode)
{
    public string ToComment()
    {
        var priority = Priority.CapturedStones > 0
            ? $"{Priority.CapturedStones}子を取りにいく"
            : Priority.EvacuationNobiPriority > 0
                ? "逃げるためのノビ"
                : "優先条件なし";
        var selection = SamePriorityCandidateCount == 1
            ? "最優先候補が1手"
            : SelectionMode == MoveSelectionMode.ChebyshevDistanceFromStar
                ? $"同順位{SamePriorityCandidateCount}手から星周辺を意識して抽選"
                : $"同順位{SamePriorityCandidateCount}手からランダム抽選";
        return $"ポン抜きエンジン\n理由: {priority}\n選択: {selection}";
    }
}
