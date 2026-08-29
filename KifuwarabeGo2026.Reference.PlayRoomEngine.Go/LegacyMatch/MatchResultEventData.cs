namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Carries a player declaration or an authoritative adjudicated result.
/// </summary>
public readonly record struct MatchResultEventData(GoStone? DeclaredBy, MatchResult Result);
