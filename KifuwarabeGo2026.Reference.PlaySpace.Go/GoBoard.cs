namespace KifuwarabeGo2026.Reference.PlaySpace.Go;

internal enum GoStone { Empty, Black, White }

internal readonly record struct GoPoint(int X, int Y);

/// <summary>通常囲碁参照実装内だけで使用する盤面です。</summary>
internal sealed class GoBoard
{
    private readonly GoStone[,] _stones;

    public GoBoard(int size)
    {
        Size = size;
        _stones = new GoStone[size, size];
    }

    private GoBoard(GoBoard source)
    {
        Size = source.Size;
        _stones = (GoStone[,])source._stones.Clone();
    }

    public int Size { get; }

    public GoBoard Clone() => new(this);

    public GoStone GetStone(GoPoint point) => _stones[point.X, point.Y];

    public bool TrySetSetupStone(GoPoint point, GoStone stone)
    {
        if (!IsOnBoard(point) || stone == GoStone.Empty || GetStone(point) != GoStone.Empty)
            return false;
        _stones[point.X, point.Y] = stone;
        return true;
    }

    public bool TryPlaceStone(
        GoPoint point,
        GoStone stone,
        GoPoint? forbiddenKoPoint,
        out int capturedStones,
        out GoPoint? nextKoPoint)
    {
        capturedStones = 0;
        nextKoPoint = null;
        if (!IsOnBoard(point) || stone == GoStone.Empty || GetStone(point) != GoStone.Empty || forbiddenKoPoint == point)
            return false;

        _stones[point.X, point.Y] = stone;
        var removed = new List<(GoPoint Point, GoStone Stone)>();
        foreach (var neighbor in EnumerateNeighbors(point))
        {
            if (GetStone(neighbor) != Opposite(stone)) continue;
            var chain = CollectRegion(neighbor);
            if (HasLiberty(chain)) continue;
            foreach (var captured in chain)
            {
                removed.Add((captured, GetStone(captured)));
                _stones[captured.X, captured.Y] = GoStone.Empty;
            }
            capturedStones += chain.Count;
        }

        var placedChain = CollectRegion(point);
        if (!HasLiberty(placedChain))
        {
            _stones[point.X, point.Y] = GoStone.Empty;
            foreach (var captured in removed)
                _stones[captured.Point.X, captured.Point.Y] = captured.Stone;
            capturedStones = 0;
            return false;
        }

        if (capturedStones == 1 && placedChain.Count == 1 && CountLiberties(placedChain) == 1)
            nextKoPoint = removed[0].Point;
        return true;
    }

    public (int BlackArea, int WhiteArea) ScoreArea()
    {
        var black = 0;
        var white = 0;
        var visited = new HashSet<GoPoint>();
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var point = new GoPoint(x, y);
            var stone = GetStone(point);
            if (stone == GoStone.Black) { black++; continue; }
            if (stone == GoStone.White) { white++; continue; }
            if (!visited.Add(point)) continue;

            var region = new List<GoPoint>();
            var borders = new HashSet<GoStone>();
            var queue = new Queue<GoPoint>();
            queue.Enqueue(point);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                region.Add(current);
                foreach (var neighbor in EnumerateNeighbors(current))
                {
                    var neighborStone = GetStone(neighbor);
                    if (neighborStone == GoStone.Empty)
                    {
                        if (visited.Add(neighbor)) queue.Enqueue(neighbor);
                    }
                    else borders.Add(neighborStone);
                }
            }
            if (borders.SetEquals([GoStone.Black])) black += region.Count;
            else if (borders.SetEquals([GoStone.White])) white += region.Count;
        }
        return (black, white);
    }

    public string PositionKey()
    {
        var chars = new char[Size * Size];
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
            chars[(y * Size) + x] = _stones[x, y] switch { GoStone.Black => 'B', GoStone.White => 'W', _ => '.' };
        return new string(chars);
    }

    public IEnumerable<(GoPoint Point, GoStone Stone)> EnumerateStones()
    {
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var stone = _stones[x, y];
            if (stone != GoStone.Empty) yield return (new GoPoint(x, y), stone);
        }
    }

    private bool IsOnBoard(GoPoint point) =>
        point.X >= 0 && point.X < Size && point.Y >= 0 && point.Y < Size;

    private List<GoPoint> CollectRegion(GoPoint start)
    {
        var color = GetStone(start);
        var result = new List<GoPoint>();
        var visited = new HashSet<GoPoint> { start };
        var queue = new Queue<GoPoint>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            result.Add(point);
            foreach (var neighbor in EnumerateNeighbors(point))
                if (GetStone(neighbor) == color && visited.Add(neighbor)) queue.Enqueue(neighbor);
        }
        return result;
    }

    private bool HasLiberty(IReadOnlyList<GoPoint> chain) => CountLiberties(chain) > 0;

    private int CountLiberties(IReadOnlyList<GoPoint> chain)
    {
        var liberties = new HashSet<GoPoint>();
        foreach (var point in chain)
        foreach (var neighbor in EnumerateNeighbors(point))
            if (GetStone(neighbor) == GoStone.Empty) liberties.Add(neighbor);
        return liberties.Count;
    }

    private IEnumerable<GoPoint> EnumerateNeighbors(GoPoint point)
    {
        if (point.X > 0) yield return new(point.X - 1, point.Y);
        if (point.X + 1 < Size) yield return new(point.X + 1, point.Y);
        if (point.Y > 0) yield return new(point.X, point.Y - 1);
        if (point.Y + 1 < Size) yield return new(point.X, point.Y + 1);
    }

    private static GoStone Opposite(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
}
