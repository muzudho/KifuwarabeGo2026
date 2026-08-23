namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Carries a player declaration or an authoritative adjudicated result.
/// </summary>
public readonly record struct MatchResultEventData(GoStone? DeclaredBy, MatchResult Result);
