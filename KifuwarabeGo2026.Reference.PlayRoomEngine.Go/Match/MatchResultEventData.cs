namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Carries a player declaration or an authoritative adjudicated result.
/// </summary>
public readonly record struct MatchResultEventData(GoStone? DeclaredBy, MatchResult Result);
