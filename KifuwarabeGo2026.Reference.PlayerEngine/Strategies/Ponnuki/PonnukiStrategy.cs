namespace KifuwarabeGo2026.Reference.PlayerEngine.Strategies.Ponnuki;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>Casual App のポン抜き用着手戦略です。</summary>
public sealed class PonnukiStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request)
        => GenerateMoveWithDecision(request)?.Move;

    public PonnukiMoveDecision? GenerateMoveWithDecision(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        if (legalMoves.Count == 0)
            return null;

        // Priority. 1: Boundary Empty Count が 1 の相手連へアタリを打ちます。
        var atariCandidates = PonnukiMovePrioritizer.CollectPriorityOneAtariCandidates(
            request.Board,
            request.Color,
            legalMoves);
        if (atariCandidates.Count == 1)
        {
            return new PonnukiMoveDecision(
                atariCandidates[0].Move,
                default,
                1,
                request.SelectionMode,
                PonnukiDecisionReason.PriorityOneAtari,
                null);
        }

        // アタリが複数なら、その候補だけを Priority. 2 以下で絞り込みます。
        var candidatesAfterPriorityOne = atariCandidates.Count > 0
            ? atariCandidates
            : legalMoves;

        // Priority. 2: Boundary Empty Count が拮抗する自連のノビを選びます。
        var priorityTwoCandidates = PonnukiMovePrioritizer.SelectMaximumBoundaryEmpty(
            PonnukiMovePrioritizer.CollectPriorityOneNobiCandidates(
                request.Board,
                request.Color,
                candidatesAfterPriorityOne));
        if (priorityTwoCandidates.Count == 1)
        {
            var selected = priorityTwoCandidates[0];
            return new PonnukiMoveDecision(
                selected.LegalMove.Move,
                default,
                1,
                request.SelectionMode,
                atariCandidates.Count > 0
                    ? PonnukiDecisionReason.PriorityOneAtariThenPriorityTwoNobi
                    : PonnukiDecisionReason.PriorityTwoNobi,
                selected.BoundaryEmptyCountAfterMove);
        }

        // Priority. 2 が同点ならその候補だけ、無ければ直前段階の候補へ
        // Other Priorities を適用します。
        var candidatesForOtherPriorities = priorityTwoCandidates.Count > 0
            ? priorityTwoCandidates.Select(candidate => candidate.LegalMove).ToList()
            : candidatesAfterPriorityOne;
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
        int? priorityTwoBoundaryEmptyCount = priorityTwoCandidates.Count > 0
            ? priorityTwoCandidates[0].BoundaryEmptyCountAfterMove
            : null;
        var reason = atariCandidates.Count > 0
            ? priorityTwoCandidates.Count > 0
                ? PonnukiDecisionReason.PriorityOneAtariThenPriorityTwoNobiThenOtherPriorities
                : PonnukiDecisionReason.PriorityOneAtariThenOtherPriorities
            : priorityTwoCandidates.Count > 0
                ? PonnukiDecisionReason.PriorityTwoNobiThenOtherPriorities
                : PonnukiDecisionReason.OtherPriorities;
        return new PonnukiMoveDecision(
            move,
            selectedPriority.Priority,
            bestMoves.Count,
            request.SelectionMode,
            reason,
            priorityTwoBoundaryEmptyCount);
    }
}

public enum PonnukiDecisionReason
{
    PriorityOneAtari,
    PriorityOneAtariThenPriorityTwoNobi,
    PriorityOneAtariThenPriorityTwoNobiThenOtherPriorities,
    PriorityOneAtariThenOtherPriorities,
    PriorityTwoNobi,
    PriorityTwoNobiThenOtherPriorities,
    OtherPriorities,
}

public readonly record struct PonnukiMoveDecision(
    GoPoint Move,
    PonnukiMovePriority Priority,
    int SamePriorityCandidateCount,
    MoveSelectionMode SelectionMode,
    PonnukiDecisionReason Reason,
    int? PriorityTwoBoundaryEmptyCount)
{
    public string ToComment()
    {
        var reason = Reason switch
        {
            PonnukiDecisionReason.PriorityOneAtari => "Priority.1 のアタリ",
            PonnukiDecisionReason.PriorityOneAtariThenPriorityTwoNobi =>
                $"Priority.1 のアタリで同点 → Priority.2 のノビ（着手後の Boundary Empty Count: {PriorityTwoBoundaryEmptyCount}）",
            PonnukiDecisionReason.PriorityOneAtariThenPriorityTwoNobiThenOtherPriorities =>
                $"Priority.1 のアタリ・Priority.2 のノビで同点（Boundary Empty Count: {PriorityTwoBoundaryEmptyCount}）",
            PonnukiDecisionReason.PriorityOneAtariThenOtherPriorities => "Priority.1 のアタリで同点",
            PonnukiDecisionReason.PriorityTwoNobi =>
                $"Priority.2 のノビ（着手後の Boundary Empty Count: {PriorityTwoBoundaryEmptyCount}）",
            PonnukiDecisionReason.PriorityTwoNobiThenOtherPriorities =>
                $"Priority.2 のノビで同点（Boundary Empty Count: {PriorityTwoBoundaryEmptyCount}）",
            _ => DescribeOtherPriority(Priority),
        };
        var selection = SamePriorityCandidateCount == 1
            ? "最優先候補が1手"
            : SelectionMode == MoveSelectionMode.ChebyshevDistanceFromStar
                ? $"同順位{SamePriorityCandidateCount}手から星域距離を考慮して選択"
                : $"同順位{SamePriorityCandidateCount}手からランダム選択";
        var fallback = Reason is PonnukiDecisionReason.PriorityOneAtariThenPriorityTwoNobiThenOtherPriorities
            or PonnukiDecisionReason.PriorityOneAtariThenOtherPriorities
            or PonnukiDecisionReason.PriorityTwoNobiThenOtherPriorities
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
