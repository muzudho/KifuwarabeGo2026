namespace KifuwarabeGo2026.Engine.GoApps.Formal.Play;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;
using System.Linq;

/// <summary>Formal App の共通対局用の着手生成戦略です。</summary>
internal sealed class PlayStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request) =>
        GenerateMoveWithDecision(request)?.Move;

    /// <summary>優先順位に従って次の 1 手と、その選択理由を返します。</summary>
    public PlayMoveDecision? GenerateMoveWithDecision(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        if (legalMoves.Count == 0)
            return null;

        // Priority 1: 相手のアタリを取り切る。
        var atariCaptureCandidates = PlayMovePrioritizer.CollectAtariCaptureCandidates(request.Board, request.Color, legalMoves);
        if (atariCaptureCandidates.Count > 0)
            return CreateDecision(atariCaptureCandidates, request, PlayDecisionReason.CaptureAtari);

        // Priority 2: 自分のアタリを防ぐ。ただし、着手後に二眼になる候補はここでは選ばない。
        var defenseCandidates = PlayMovePrioritizer.CollectThreatenedRenDefenseCandidates(request.Board, request.Color, legalMoves);
        if (defenseCandidates.Count > 0)
            return CreateDecision(defenseCandidates, request, PlayDecisionReason.DefendThreatenedRen);

        // Priority 3: 上記以外は既存の選択モードに従って選ぶ。
        return CreateDecision(legalMoves, request, PlayDecisionReason.OtherLegalMove);
    }

    private static PlayMoveDecision CreateDecision(
        System.Collections.Generic.IReadOnlyList<LegalMoveCandidate> candidates,
        GenerateMoveRequest request,
        PlayDecisionReason reason)
    {
        var move = MoveSelector.Select(candidates.Select(candidate => candidate.Move).ToList(), request);
        return new PlayMoveDecision(move, candidates.Count, request.SelectionMode, reason);
    }
}

/// <summary>Formal Play の着手選択理由です。</summary>
internal enum PlayDecisionReason
{
    CaptureAtari,
    DefendThreatenedRen,
    OtherLegalMove,
}

/// <summary>棋譜コメントに残す Formal Play の着手決定情報です。</summary>
internal readonly record struct PlayMoveDecision(
    GoPoint Move,
    int CandidateCount,
    MoveSelectionMode SelectionMode,
    PlayDecisionReason Reason)
{
    public string ToComment()
    {
        var reason = Reason switch
        {
            PlayDecisionReason.CaptureAtari => "相手のアタリを取る",
            PlayDecisionReason.DefendThreatenedRen => "自分のアタリの連を守る（二眼になる候補は除外）",
            _ => "優先手なし。合法手から選ぶ",
        };
        var selection = CandidateCount == 1
            ? "候補は 1 手"
            : SelectionMode == MoveSelectionMode.ChebyshevDistanceFromStar
                ? $"候補 {CandidateCount} 手から星に近い手を選ぶ"
                : $"候補 {CandidateCount} 手からランダムに選ぶ";
        return $"Formal Play エンジン\n判定: {reason}\n選択: {selection}";
    }
}
