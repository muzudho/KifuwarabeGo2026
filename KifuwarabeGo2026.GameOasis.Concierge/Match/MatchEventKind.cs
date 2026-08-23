namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

/// <summary>
/// Identifies a transport-independent state change that observers can consume.
/// </summary>
public enum MatchEventKind
{
    ActionAccepted,
    ClockSynchronized,
    ResultDeclared,
    ResultConfirmed,
    PlayResumed,
    ResultAdjudicated,
}
