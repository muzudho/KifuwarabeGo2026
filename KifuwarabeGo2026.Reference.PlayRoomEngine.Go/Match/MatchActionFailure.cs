namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

/// <summary>
/// Describes why a match action was rejected.
/// </summary>
public enum MatchActionFailure
{
    None,
    AwaitingResult,
    MatchCompleted,
    PointOutsideBoard,
    PointOccupied,
    Ko,
    IllegalMove,
}
