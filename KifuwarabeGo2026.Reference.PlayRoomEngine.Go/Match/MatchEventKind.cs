namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

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
