namespace KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Casual App のポン抜き用着手戦略です。</summary>
internal sealed class PonnukiStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request)
        => GenerateMoveWithDecision(request)?.Move;

    public PonnukiMoveDecision? GenerateMoveWithDecision(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        if (legalMoves.Count == 0)
            return null;

        var priorityOneCandidates = PonnukiMovePrioritizer.SelectMaximumBoundaryEmpty(
            PonnukiMovePrioritizer.CollectPriorityOneNobiCandidates(request.Board, request.Color, legalMoves));
        if (priorityOneCandidates.Count == 1)
        {
            var selected = priorityOneCandidates[0];
            return new PonnukiMoveDecision(
                selected.LegalMove.Move,
                default,
                1,
                request.SelectionMode,
                PonnukiDecisionReason.PriorityOneNobi,
                selected.BoundaryEmptyCountAfterMove);
        }

        // Priority. 1 の最大候補が複数なら、その候補だけを既存優先順位で絞ります。
        // 候補が無いときだけ、全合法手へ既存優先順位を適用します。
        var candidatesForOtherPriorities = priorityOneCandidates.Count > 0
            ? priorityOneCandidates.Select(candidate => candidate.LegalMove).ToList()
            : legalMoves;
        var prioritizedMoves = new List<PonnukiMoveCandidate>(candidatesForOtherPriorities.Count);
        foreach (var candidate in candidatesForOtherPriorities)
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
        var selectedPriority = prioritizedMoves.Find(candidate => candidate.Move == move);
        int? priorityOneBoundaryEmptyCount = priorityOneCandidates.Count > 0
            ? priorityOneCandidates[0].BoundaryEmptyCountAfterMove
            : null;
        return new PonnukiMoveDecision(
            move,
            selectedPriority.Priority,
            bestMoves.Count,
            request.SelectionMode,
            priorityOneCandidates.Count > 0
                ? PonnukiDecisionReason.PriorityOneNobiThenOtherPriorities
                : PonnukiDecisionReason.OtherPriorities,
            priorityOneBoundaryEmptyCount);
    }
}

internal enum PonnukiDecisionReason
{
    PriorityOneNobi,
    PriorityOneNobiThenOtherPriorities,
    OtherPriorities,
}

internal readonly record struct PonnukiMoveDecision(
    GoPoint Move,
    PonnukiMovePriority Priority,
    int SamePriorityCandidateCount,
    MoveSelectionMode SelectionMode,
    PonnukiDecisionReason Reason,
    int? PriorityOneBoundaryEmptyCount)
{
    public string ToComment()
    {
        var reason = Reason switch
        {
            PonnukiDecisionReason.PriorityOneNobi =>
                $"Priority.1 のノビ（着手後の Boundary Empty Count: {PriorityOneBoundaryEmptyCount}）",
            PonnukiDecisionReason.PriorityOneNobiThenOtherPriorities =>
                $"Priority.1 のノビで同点（Boundary Empty Count: {PriorityOneBoundaryEmptyCount}）",
            _ => DescribeOtherPriority(Priority),
        };
        var selection = SamePriorityCandidateCount == 1
            ? "最優先候補が1手"
            : SelectionMode == MoveSelectionMode.ChebyshevDistanceFromStar
                ? $"同順位{SamePriorityCandidateCount}手から星域距離を考慮して選択"
                : $"同順位{SamePriorityCandidateCount}手からランダム選択";
        var fallback = Reason == PonnukiDecisionReason.PriorityOneNobiThenOtherPriorities
            ? $"\n同点のため Other Priorities: {DescribeOtherPriority(Priority)}"
            : "";
        return $"ポン抜きエンジン\n理由: {reason}{fallback}\n選択: {selection}";

        static string DescribeOtherPriority(PonnukiMovePriority priority) => priority.CapturedStones > 0
            ? $"{priority.CapturedStones}子を取りにいく"
            : priority.ContestedContactPriority > 0
                ? "相手に接する拮抗連を優先"
                : "優先条件なし";
    }
}
