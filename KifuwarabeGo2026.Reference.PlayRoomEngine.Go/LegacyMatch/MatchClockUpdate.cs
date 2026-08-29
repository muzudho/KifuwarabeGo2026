namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

/// <summary>
/// Carries a complete clock state decided by a trusted server-side clock service.
/// </summary>
public readonly record struct MatchClockUpdate(
    long Sequence,
    DateTimeOffset SynchronizedAt,
    TimeSpan BlackRemaining,
    TimeSpan WhiteRemaining,
    DateTimeOffset? ActiveTurnDeadline);
