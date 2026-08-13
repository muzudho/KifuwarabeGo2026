namespace KifuwarabeGo2026.Engine.GoApps.Formal.Play;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;
using System.Linq;

/// <summary>Formal App の共通対局用の着手生成戦略です。</summary>
internal sealed class PlayStrategy : IGenerateMoveStrategy
{
    /// <summary>優先順位に従って合法手から次の 1 手を選びます。</summary>
    public GoPoint? GenerateMove(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        if (legalMoves.Count == 0)
            return null;

        // Priority 1: 相手のアタリを取り切る。
        var atariCaptureCandidates = PlayMovePrioritizer.CollectAtariCaptureCandidates(request.Board, request.Color, legalMoves);
        if (atariCaptureCandidates.Count > 0)
            return MoveSelector.Select(atariCaptureCandidates.Select(candidate => candidate.Move).ToList(), request);

        // Priority 2: 自分のアタリを防ぐ。ただし、着手後に二眼になる候補はここでは選ばない。
        var defenseCandidates = PlayMovePrioritizer.CollectThreatenedRenDefenseCandidates(request.Board, request.Color, legalMoves);
        if (defenseCandidates.Count > 0)
            return MoveSelector.Select(defenseCandidates.Select(candidate => candidate.Move).ToList(), request);

        // Priority 3: 上記以外は既存の選択モードに従って選ぶ。
        return MoveSelector.Select(legalMoves.Select(candidate => candidate.Move).ToList(), request);
    }
}
