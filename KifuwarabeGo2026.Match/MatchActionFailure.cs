namespace KifuwarabeGo2026.Match;

/// <summary>
/// Describes why a match action was rejected.
/// </summary>
public enum MatchActionFailure
{
    None,
    MatchCompleted,
    PointOutsideBoard,
    PointOccupied,
    Ko,
    IllegalMove,
}
