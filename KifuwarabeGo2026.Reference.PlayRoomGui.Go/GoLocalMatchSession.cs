namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>専用Go Play Room Hostが所有する最小Local Match状態です。</summary>
public sealed class GoLocalMatchSession
{
    private readonly GoBoard _board;

    public GoLocalMatchSession(GoPlayRoomLaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Activity != GoPlayRoomActivity.Playing)
            throw new ArgumentException("A Local Match session requires a playing launch plan.", nameof(plan));

        Plan = plan;
        _board = new GoBoard(plan.BoardSize);
        foreach (var setup in plan.SetupStones)
            if (!_board.TrySetSetupStone(setup.Point, setup.Stone))
                throw new ArgumentException($"Invalid setup stone at ({setup.Point.X},{setup.Point.Y}).", nameof(plan));
        CurrentTurn = plan.StartingPlayer;
    }

    public GoPlayRoomLaunchPlan Plan { get; }
    public GoStone CurrentTurn { get; private set; }
    public GoPoint? KoPoint { get; private set; }
    public GoPoint? LastMovePoint { get; private set; }
    public int PlayedMoveCount { get; private set; }
    public int BlackCaptures { get; private set; }
    public int WhiteCaptures { get; private set; }
    public int ConsecutivePasses { get; private set; }
    public bool IsGameOver { get; private set; }
    public string GameOverReason { get; private set; } = "";

    public bool IsComputerTurn => Plan.PlayerConnections.Any(connection =>
        RoleStone(connection.RoleId) == CurrentTurn);

    public bool CanPlay(GoPoint point)
    {
        if (IsGameOver) return false;
        var trial = _board.Clone();
        return trial.TryPlaceStone(point, CurrentTurn, KoPoint, out _, out _);
    }

    public bool TryPlay(GoPoint point)
    {
        if (IsGameOver || !_board.TryPlaceStone(point, CurrentTurn, KoPoint, out var captured, out var nextKoPoint))
            return false;

        if (CurrentTurn == GoStone.Black) BlackCaptures += captured;
        else WhiteCaptures += captured;
        KoPoint = nextKoPoint;
        LastMovePoint = point;
        ConsecutivePasses = 0;
        PlayedMoveCount++;
        CurrentTurn = Opposite(CurrentTurn);
        return true;
    }

    public bool Pass()
    {
        if (IsGameOver) return false;
        KoPoint = null;
        LastMovePoint = null;
        ConsecutivePasses++;
        PlayedMoveCount++;
        CurrentTurn = Opposite(CurrentTurn);
        if (ConsecutivePasses >= 2)
        {
            IsGameOver = true;
            GameOverReason = "Two consecutive passes.";
        }
        return true;
    }

    public bool Resign(GoStone stone)
    {
        if (IsGameOver || stone != CurrentTurn) return false;
        IsGameOver = true;
        GameOverReason = $"{stone} resigned.";
        return true;
    }

    public GoPlayRoomViewState CaptureViewState() => GoPlayRoomViewState.Capture(
        IsGameOver ? GoPlayRoomActivity.GameOver : GoPlayRoomActivity.Playing,
        _board.Size,
        _board.GetStone,
        CurrentTurn,
        PlayedMoveCount,
        BlackCaptures,
        WhiteCaptures,
        KoPoint,
        winner: null,
        GameOverReason,
        PlayedMoveCount,
        PlayedMoveCount,
        LastMovePoint);

    public IReadOnlyList<(GoPoint Point, GoStone Stone)> Stones => _board.EnumerateStones().ToArray();

    public static GoStone? RoleStone(string roleId) => roleId.ToLowerInvariant() switch
    {
        "black" => GoStone.Black,
        "white" => GoStone.White,
        _ => null,
    };

    private static GoStone Opposite(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
}
