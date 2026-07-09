namespace KifuwarabeGo2026.Domain;

using System;
using System.Collections.Generic;

public sealed class GoBoard
{
    private readonly GoStone[,] _stones;

    public GoBoard(int size)
    {
        if (size is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be 9, 13, or 19.");
        }

        Size = size;
        _stones = new GoStone[size, size];
    }

    public int Size { get; }

    public GoStone GetStone(int x, int y)
    {
        if (!IsOnBoard(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Point is outside the board.");
        }

        return _stones[x, y];
    }

    public bool TryPlaceStone(int x, int y, GoStone stone, out int capturedStones)
    {
        capturedStones = 0;
        if (stone == GoStone.Empty || !IsOnBoard(x, y) || _stones[x, y] != GoStone.Empty)
        {
            return false;
        }

        _stones[x, y] = stone;
        var opponent = OppositeOf(stone);
        foreach (var neighbor in EnumerateNeighbors(x, y))
        {
            if (_stones[neighbor.X, neighbor.Y] != opponent)
            {
                continue;
            }

            var ren = CollectRen(neighbor.X, neighbor.Y);
            if (!HasLiberty(ren))
            {
                capturedStones += RemoveRen(ren);
            }
        }

        var placedRen = CollectRen(x, y);
        if (!HasLiberty(placedRen))
        {
            _stones[x, y] = GoStone.Empty;
            capturedStones = 0;
            return false;
        }

        return true;
    }

    private bool IsOnBoard(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    private static GoStone OppositeOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private List<(int X, int Y)> CollectRen(int x, int y)
    {
        var color = _stones[x, y];
        var ren = new List<(int X, int Y)>();
        if (color == GoStone.Empty)
        {
            return ren;
        }

        var visited = new bool[Size, Size];
        var queue = new Queue<(int X, int Y)>();
        visited[x, y] = true;
        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            ren.Add(point);

            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (visited[neighbor.X, neighbor.Y] || _stones[neighbor.X, neighbor.Y] != color)
                {
                    continue;
                }

                visited[neighbor.X, neighbor.Y] = true;
                queue.Enqueue(neighbor);
            }
        }

        return ren;
    }

    private bool HasLiberty(List<(int X, int Y)> ren)
    {
        foreach (var point in ren)
        {
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (_stones[neighbor.X, neighbor.Y] == GoStone.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int RemoveRen(List<(int X, int Y)> ren)
    {
        foreach (var point in ren)
        {
            _stones[point.X, point.Y] = GoStone.Empty;
        }

        return ren.Count;
    }

    private IEnumerable<(int X, int Y)> EnumerateNeighbors(int x, int y)
    {
        if (x > 0)
        {
            yield return (x - 1, y);
        }

        if (x < Size - 1)
        {
            yield return (x + 1, y);
        }

        if (y > 0)
        {
            yield return (x, y - 1);
        }

        if (y < Size - 1)
        {
            yield return (x, y + 1);
        }
    }
}
