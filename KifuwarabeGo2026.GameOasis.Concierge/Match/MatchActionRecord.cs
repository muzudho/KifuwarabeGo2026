namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Records one successfully accepted action without storage or presentation metadata.
/// </summary>
public readonly record struct MatchActionRecord(
    long Revision,
    MatchActionKind Action,
    GoStone PlayedBy,
    GoPoint? Point,
    int CapturedStones);
