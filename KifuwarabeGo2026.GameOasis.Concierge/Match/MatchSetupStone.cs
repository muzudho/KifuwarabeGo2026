namespace KifuwarabeGo2026.GameOasis.Concierge.Match;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Places one stone on the board before play begins.
/// </summary>
public readonly record struct MatchSetupStone(GoStone Stone, GoPoint Point);
