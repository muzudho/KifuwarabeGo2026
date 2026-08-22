namespace KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

internal enum PonnukiStone
{
    Empty,
    Black,
    White,
}

internal readonly record struct PonnukiPoint(int X, int Y);

/// <summary>参照実装内だけで使用する、最小の石取り盤です。</summary>
internal sealed class PonnukiBoard
{
    private readonly PonnukiStone[,] _stones;

    public PonnukiBoard(int size)
    {
        Size = size;
        _stones = new PonnukiStone[size, size];
    }

    private PonnukiBoard(PonnukiBoard source)
    {
        Size = source.Size;
        _stones = (PonnukiStone[,])source._stones.Clone();
    }

    public int Size { get; }

    public PonnukiBoard Clone() => new(this);

    public PonnukiStone GetStone(PonnukiPoint point) => _stones[point.X, point.Y];

    public bool TrySetSetupStone(PonnukiPoint point, PonnukiStone stone)
    {
        if (!IsOnBoard(point) || stone == PonnukiStone.Empty || GetStone(point) != PonnukiStone.Empty)
            return false;

        _stones[point.X, point.Y] = stone;
        return true;
    }

    public bool TryPlaceStone(
        PonnukiPoint point,
        PonnukiStone stone,
        PonnukiPoint? forbiddenKoPoint,
        out int capturedStones,
        out PonnukiPoint? nextKoPoint)
    {
        capturedStones = 0;
        nextKoPoint = null;
        if (!IsOnBoard(point) || stone == PonnukiStone.Empty || GetStone(point) != PonnukiStone.Empty ||
            forbiddenKoPoint == point)
            return false;

        _stones[point.X, point.Y] = stone;
        var removed = new List<(PonnukiPoint Point, PonnukiStone Stone)>();
        foreach (var neighbor in EnumerateNeighbors(point))
        {
            if (GetStone(neighbor) != Opposite(stone))
                continue;

            var chain = CollectChain(neighbor);
            if (HasLiberty(chain))
                continue;

            foreach (var captured in chain)
            {
                removed.Add((captured, GetStone(captured)));
                _stones[captured.X, captured.Y] = PonnukiStone.Empty;
            }
            capturedStones += chain.Count;
        }

        var placedChain = CollectChain(point);
        if (!HasLiberty(placedChain))
        {
            _stones[point.X, point.Y] = PonnukiStone.Empty;
            foreach (var captured in removed)
                _stones[captured.Point.X, captured.Point.Y] = captured.Stone;
            capturedStones = 0;
            return false;
        }

        if (capturedStones == 1 && placedChain.Count == 1 && CountLiberties(placedChain) == 1)
            nextKoPoint = removed[0].Point;

        return true;
    }

    public IEnumerable<(PonnukiPoint Point, PonnukiStone Stone)> EnumerateStones()
    {
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var stone = _stones[x, y];
            if (stone != PonnukiStone.Empty)
                yield return (new PonnukiPoint(x, y), stone);
        }
    }

    private bool IsOnBoard(PonnukiPoint point) =>
        point.X >= 0 && point.X < Size && point.Y >= 0 && point.Y < Size;

    private List<PonnukiPoint> CollectChain(PonnukiPoint start)
    {
        var color = GetStone(start);
        var result = new List<PonnukiPoint>();
        var visited = new HashSet<PonnukiPoint> { start };
        var queue = new Queue<PonnukiPoint>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            result.Add(point);
            foreach (var neighbor in EnumerateNeighbors(point))
            {
                if (GetStone(neighbor) == color && visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
        return result;
    }

    private bool HasLiberty(IReadOnlyList<PonnukiPoint> chain) => CountLiberties(chain) > 0;

    private int CountLiberties(IReadOnlyList<PonnukiPoint> chain)
    {
        var liberties = new HashSet<PonnukiPoint>();
        foreach (var point in chain)
        foreach (var neighbor in EnumerateNeighbors(point))
        {
            if (GetStone(neighbor) == PonnukiStone.Empty)
                liberties.Add(neighbor);
        }
        return liberties.Count;
    }

    private IEnumerable<PonnukiPoint> EnumerateNeighbors(PonnukiPoint point)
    {
        if (point.X > 0) yield return new PonnukiPoint(point.X - 1, point.Y);
        if (point.X + 1 < Size) yield return new PonnukiPoint(point.X + 1, point.Y);
        if (point.Y > 0) yield return new PonnukiPoint(point.X, point.Y - 1);
        if (point.Y + 1 < Size) yield return new PonnukiPoint(point.X, point.Y + 1);
    }

    private static PonnukiStone Opposite(PonnukiStone stone) =>
        stone == PonnukiStone.Black ? PonnukiStone.White : PonnukiStone.Black;
}
