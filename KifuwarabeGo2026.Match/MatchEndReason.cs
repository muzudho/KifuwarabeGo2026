namespace KifuwarabeGo2026.Match;

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
}
