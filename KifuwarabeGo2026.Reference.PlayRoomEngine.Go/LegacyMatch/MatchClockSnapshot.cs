namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

/// <summary>
/// Exposes the latest authoritative clock synchronization to observers.
/// </summary>
public readonly record struct MatchClockSnapshot(
    long Sequence,
    DateTimeOffset SynchronizedAt,
    TimeSpan BlackRemaining,
    TimeSpan WhiteRemaining,
    DateTimeOffset? ActiveTurnDeadline);
