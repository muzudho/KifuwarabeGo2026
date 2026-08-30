namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

/// <summary>
/// Describes why a match stopped accepting moves.
/// </summary>
public enum MatchEndReason
{
    None,
    ConsecutivePasses,
    MoveLimit,
    Resignation,
    SuperKoViolation,
    Adjudication,
}
