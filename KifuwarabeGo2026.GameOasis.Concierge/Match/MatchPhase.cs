namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

/// <summary>
/// Describes whether play, result agreement, or the complete match is active.
/// </summary>
public enum MatchPhase
{
    Playing,
    AwaitingResult,
    Completed,
}
