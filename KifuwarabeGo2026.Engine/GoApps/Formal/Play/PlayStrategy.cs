namespace KifuwarabeGo2026.Engine.GoApps.Formal.Play;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Formal App の通常対局用着手生成です。</summary>
internal sealed class PlayStrategy : IGenerateMoveStrategy
{
    public GoPoint? GenerateMove(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);
        return legalMoves.Count == 0
            ? null
            : MoveSelector.Select(legalMoves.Select(candidate => candidate.Move).ToList(), request);
    }
}
