namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Places one stone on the board before play begins.
/// </summary>
public readonly record struct MatchSetupStone(GoStone Stone, GoPoint Point);
