namespace KifuwarabeGo2026.Match;

using KifuwarabeGo2026.Shared.Domain;

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
    GoStone? Winner);
