namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Records one successfully accepted action without storage or presentation metadata.
/// </summary>
public readonly record struct MatchActionRecord(
    long Revision,
    MatchActionKind Action,
    GoStone PlayedBy,
    GoPoint? Point,
    int CapturedStones);
