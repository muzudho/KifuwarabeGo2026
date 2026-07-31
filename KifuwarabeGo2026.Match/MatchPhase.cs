namespace KifuwarabeGo2026.Match;

/// <summary>
/// Describes whether play, result agreement, or the complete match is active.
/// </summary>
public enum MatchPhase
{
    Playing,
    AwaitingResult,
    Completed,
}
