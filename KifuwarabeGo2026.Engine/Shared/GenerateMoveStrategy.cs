namespace KifuwarabeGo2026.Engine.Shared;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>アプリごとの着手生成戦略です。</summary>
internal interface IGenerateMoveStrategy
{
    GoPoint? GenerateMove(GenerateMoveRequest request);
}

internal sealed record GenerateMoveRequest(
    GoBoard Board,
    GoStone Color,
    GoPoint? KoPoint,
    bool AvoidEyes,
    MoveSelectionMode SelectionMode,
    Random Random);

internal readonly record struct LegalMoveCandidate(GoPoint Move, GoBoard BoardAfterMove, int CapturedStones);

internal enum MoveSelectionMode
{
    Normal,
    ChebyshevDistanceFromStar,
}

internal static class LegalMoveCandidates
{
    public static List<LegalMoveCandidate> Collect(GenerateMoveRequest request)
    {
        var renParse = request.Board.ParseRens();
        var candidates = new List<LegalMoveCandidate>();
        for (var y = 0; y < request.Board.Size; y++)
        {
            for (var x = 0; x < request.Board.Size; x++)
            {
                var trial = request.Board.Clone();
                if (!trial.TryPlaceStone(x, y, request.Color, request.KoPoint, out var capturedStones, out _) ||
                    (request.AvoidEyes && request.Board.IsEyeFor(renParse, x, y, request.Color)))
                    continue;

                candidates.Add(new LegalMoveCandidate(new GoPoint(x, y), trial, capturedStones));
            }
        }

        return candidates;
    }
}

internal static class MoveSelector
{
    public static GoPoint Select(IReadOnlyList<GoPoint> moves, GenerateMoveRequest request) =>
        request.SelectionMode == MoveSelectionMode.Normal
            ? moves[request.Random.Next(moves.Count)]
            : StarRegionRandomMoveSelector.Select(moves, request.Board.Size, request.Random);
}
