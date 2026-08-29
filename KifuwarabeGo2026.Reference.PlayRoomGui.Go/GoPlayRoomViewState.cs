namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>囲碁Play Room GUIが表示する活動状態です。</summary>
public enum GoPlayRoomActivity
{
    Resting,
    Playing,
    GameOver,
    BoardEditing,
    VariationEditing,
    Reviewing,
}

/// <summary>
/// 囲碁Play Roomの描画に必要な、特定GUIフレームワークに依存しない読取専用状態です。
/// </summary>
public sealed class GoPlayRoomViewState
{
    private readonly GoStone[] _intersections;

    private GoPlayRoomViewState(
        GoPlayRoomActivity activity,
        int boardSize,
        GoStone[] intersections,
        GoStone currentTurn,
        int playedMoveCount,
        int blackCaptures,
        int whiteCaptures,
        GoPoint? koPoint,
        GoStone? winner,
        string gameOverReason,
        int timelineIndex,
        int timelineMaximum)
    {
        Activity = activity;
        BoardSize = boardSize;
        _intersections = intersections;
        CurrentTurn = currentTurn;
        PlayedMoveCount = playedMoveCount;
        BlackCaptures = blackCaptures;
        WhiteCaptures = whiteCaptures;
        KoPoint = koPoint;
        Winner = winner;
        GameOverReason = gameOverReason;
        TimelineIndex = timelineIndex;
        TimelineMaximum = timelineMaximum;
    }

    public GoPlayRoomActivity Activity { get; }
    public int BoardSize { get; }
    public GoStone CurrentTurn { get; }
    public int PlayedMoveCount { get; }
    public int BlackCaptures { get; }
    public int WhiteCaptures { get; }
    public GoPoint? KoPoint { get; }
    public GoStone? Winner { get; }
    public string GameOverReason { get; }
    public int TimelineIndex { get; }
    public int TimelineMaximum { get; }

    public GoStone GetStone(int x, int y)
    {
        if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
            throw new ArgumentOutOfRangeException($"({x}, {y}) is outside a {BoardSize}x{BoardSize} board.");

        return _intersections[(y * BoardSize) + x];
    }

    public static GoPlayRoomViewState Capture(
        GoPlayRoomActivity activity,
        int boardSize,
        Func<int, int, GoStone> getStone,
        GoStone currentTurn,
        int playedMoveCount,
        int blackCaptures,
        int whiteCaptures,
        GoPoint? koPoint,
        GoStone? winner,
        string? gameOverReason,
        int timelineIndex,
        int timelineMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boardSize);
        ArgumentNullException.ThrowIfNull(getStone);

        var intersections = new GoStone[checked(boardSize * boardSize)];
        for (var y = 0; y < boardSize; y++)
        for (var x = 0; x < boardSize; x++)
            intersections[(y * boardSize) + x] = getStone(x, y);

        return new GoPlayRoomViewState(
            activity,
            boardSize,
            intersections,
            currentTurn,
            playedMoveCount,
            blackCaptures,
            whiteCaptures,
            koPoint,
            winner,
            gameOverReason ?? "",
            Math.Max(0, timelineIndex),
            Math.Max(0, timelineMaximum));
    }
}
