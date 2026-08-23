namespace KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;

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
