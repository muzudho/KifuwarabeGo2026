namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Reports whether an action changed the match and exposes the resulting snapshot.
/// </summary>
public sealed record MatchActionResult(
    bool Succeeded,
    MatchActionKind Action,
    MatchActionFailure Failure,
    GoStone PlayedBy,
    GoPoint? Point,
    int CapturedStones,
    MatchSnapshot Snapshot);
