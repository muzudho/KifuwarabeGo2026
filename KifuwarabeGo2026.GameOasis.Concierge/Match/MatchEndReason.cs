namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

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
