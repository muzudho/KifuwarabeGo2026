namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Describes one revision of observable Match state without UI or network data.
/// </summary>
public readonly record struct MatchEventRecord(
    long Revision,
    MatchEventKind Kind,
    MatchActionRecord? Action,
    MatchClockSnapshot? Clock,
    MatchPhase Phase,
    MatchEndReason EndReason,
    GoStone? Winner,
    MatchResultEventData? ResultData);
