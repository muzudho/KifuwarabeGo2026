namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Summarizes the facts used to choose compatible setup strategies.
/// </summary>
public sealed record InitialPositionClassification(
    InitialPositionKind Kind,
    int BlackStoneCount,
    int WhiteStoneCount,
    GoStone StartingTurn,
    int? FixedHandicapStoneCount = null);
