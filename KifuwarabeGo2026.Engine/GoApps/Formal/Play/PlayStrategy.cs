namespace KifuwarabeGo2026.Engine.GoApps.Formal.Play;

using KifuwarabeGo2026.Engine.Shared;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Formal App の通常対局用着手生成です。</summary>
internal sealed class PlayStrategy : IGenerateMoveStrategy
{
    /// <summary>
    /// 着手生成
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public GoPoint? GenerateMove(GenerateMoveRequest request)
    {
        var legalMoves = LegalMoveCandidates.Collect(request);

        // 優先度１：
        //
        //      TODO: アタリ（相手の石を取れる着手）があれば、それを打つ。

        // 優先度２：
        //
        //      TODO: 自分の連が取られそうな被着手空点があり、
        //      そこに着手するとその連の空点が２つ以上になる場合は、それを打つ。

        // 優先度（残り）：
        return legalMoves.Count == 0
            ? null
            : MoveSelector.Select(legalMoves.Select(candidate => candidate.Move).ToList(), request);
    }
}
