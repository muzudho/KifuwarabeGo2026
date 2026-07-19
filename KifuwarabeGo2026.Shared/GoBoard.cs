namespace KifuwarabeGo2026.Shared.Domain;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GoBoard
{
    private readonly GoStone[,] _stones;
    private readonly ulong[,,] _zobristTable;

    public GoBoard(int size)
    {
        if (size is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be 9, 13, or 19.");
        }

        Size = size;
        _stones = new GoStone[size, size];
        _zobristTable = CreateZobristTable(size);
    }

    private GoBoard(GoBoard source)
    {
        Size = source.Size;
        _stones = new GoStone[Size, Size];
        _zobristTable = source._zobristTable;
        CurrentHash = source.CurrentHash;

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                _stones[x, y] = source._stones[x, y];
            }
        }
    }

    public int Size { get; }

    public ulong CurrentHash { get; private set; }

    public GoBoard Clone() => new(this);

    public GoStone GetStone(int x, int y)
    {
        if (!IsOnBoard(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Point is outside the board.");
        }

        return _stones[x, y];
    }

    public int CountStones(GoStone stone)
    {
        if (stone == GoStone.Empty)
        {
            return 0;
        }

        var count = 0;
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                if (_stones[x, y] == stone)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public bool TrySetSetupStone(int x, int y, GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White) || !IsOnBoard(x, y) || _stones[x, y] != GoStone.Empty)
        {
            return false;
        }

        SetStone(x, y, stone);
        return true;
    }

    public bool TrySetEditedStone(int x, int y, GoStone stone)
    {
        if (stone is not (GoStone.Empty or GoStone.Black or GoStone.White) || !IsOnBoard(x, y))
        {
            return false;
        }

        SetStone(x, y, stone);
        return true;
    }

    public GoRenParseResult ParseRens()
    {
        var renNumbers = new int[Size, Size];
        var renNumber = 0;

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                if (renNumbers[x, y] != 0)
                {
                    continue;
                }

                renNumber++;
                MarkRen(x, y, renNumber, renNumbers);
            }
        }

        return BuildRenParseResult(renNumbers, renNumber);
    }

    public bool IsEyeFor(int x, int y, GoStone stone) => IsEyeFor(ParseRens(), x, y, stone);

    public bool IsEyeFor(GoRenParseResult renParse, int x, int y, GoStone stone)
    {
        ArgumentNullException.ThrowIfNull(renParse);
        if (stone == GoStone.Empty || renParse.Size != Size || !IsOnBoard(x, y) || _stones[x, y] != GoStone.Empty)
        {
            return false;
        }

        var ren = renParse.GetRen(renParse.GetRenNumber(x, y));
        if (!ren.IsEye || ren.EyeOwnerRenNumber is not { } ownerRenNumber)
        {
            return false;
        }

        return renParse.GetRen(ownerRenNumber).Stone == stone;
    }

    public bool TryPlaceStone(int x, int y, GoStone stone, GoPoint? forbiddenKoPoint, out int capturedStones, out GoPoint? koPoint)
    {
        capturedStones = 0;
        koPoint = null;
        if (stone == GoStone.Empty || !IsOnBoard(x, y) || _stones[x, y] != GoStone.Empty)
        {
            return false;
        }

        if (forbiddenKoPoint is { } ko && ko.X == x && ko.Y == y)
        {
            return false;
        }

        SetStone(x, y, stone);
        var opponent = OppositeOf(stone);
        var removedStones = new List<(int X, int Y, GoStone Stone)>();
        foreach (var neighbor in EnumerateNeighbors(x, y))
        {
            if (_stones[neighbor.X, neighbor.Y] != opponent)
            {
                continue;
            }

            var ren = CollectRen(neighbor.X, neighbor.Y);
            if (!HasLiberty(ren))
            {
                capturedStones += RemoveRen(ren, removedStones);
            }
        }

        var placedRen = CollectRen(x, y);
        if (!HasLiberty(placedRen))
        {
            SetStone(x, y, GoStone.Empty);
            RestoreStones(removedStones);
            capturedStones = 0;
            return false;
        }

        if (capturedStones == 1 && placedRen.Count == 1 && CountLiberties(placedRen) == 1)
        {
            var capturedPoint = removedStones[0];
            koPoint = new GoPoint(capturedPoint.X, capturedPoint.Y);
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

    private void MarkRen(int x, int y, int renNumber, int[,] renNumbers)
    {
        var color = _stones[x, y];
        var queue = new Queue<(int X, int Y)>();
        renNumbers[x, y] = renNumber;
        queue.Enqueue((x, y));

        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (renNumbers[neighbor.X, neighbor.Y] != 0 || _stones[neighbor.X, neighbor.Y] != color)
                {
                    continue;
                }

                renNumbers[neighbor.X, neighbor.Y] = renNumber;
                queue.Enqueue(neighbor);
            }
        }
    }

    private GoRenParseResult BuildRenParseResult(int[,] renNumbers, int renCount)
    {
        var stones = new GoStone[renCount + 1];
        var points = new List<GoPoint>[renCount + 1];
        var neighbors = new HashSet<int>[renCount + 1];
        var eyeNumbers = new List<int>[renCount + 1];
        var eyeOwnerRenNumbers = new int?[renCount + 1];
        for (var renNumber = 1; renNumber <= renCount; renNumber++)
        {
            points[renNumber] = new List<GoPoint>();
            neighbors[renNumber] = new HashSet<int>();
            eyeNumbers[renNumber] = new List<int>();
        }

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var currentRenNumber = renNumbers[x, y];
                stones[currentRenNumber] = _stones[x, y];
                points[currentRenNumber].Add(new GoPoint(x, y));
            }
        }

        var edges = CreateRenGraphEdges(renNumbers, neighbors);
        ApplyEyeJudgement(renCount, stones, points, neighbors, eyeNumbers, eyeOwnerRenNumbers);

        var rens = new List<GoRen>(renCount);
        for (var currentRenNumber = 1; currentRenNumber <= renCount; currentRenNumber++)
        {
            rens.Add(new GoRen(
                currentRenNumber,
                stones[currentRenNumber],
                points[currentRenNumber],
                neighbors[currentRenNumber].OrderBy(number => number).ToArray(),
                eyeNumbers[currentRenNumber],
                eyeOwnerRenNumbers[currentRenNumber]));
        }

        return new GoRenParseResult(renNumbers, rens, edges);
    }

    private List<GoRenGraphEdge> CreateRenGraphEdges(int[,] renNumbers, HashSet<int>[] neighbors)
    {
        var edges = new List<GoRenGraphEdge>();
        var seen = new HashSet<GoRenGraphEdge>();

        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var renNumber = renNumbers[x, y];
                if (x + 1 < Size)
                {
                    AddRenGraphEdge(renNumber, renNumbers[x + 1, y], neighbors, seen, edges);
                }

                if (y + 1 < Size)
                {
                    AddRenGraphEdge(renNumber, renNumbers[x, y + 1], neighbors, seen, edges);
                }
            }
        }

        return edges;
    }

    private static void AddRenGraphEdge(
        int from,
        int to,
        HashSet<int>[] neighbors,
        HashSet<GoRenGraphEdge> seen,
        List<GoRenGraphEdge> edges)
    {
        if (from == to)
        {
            return;
        }

        neighbors[from].Add(to);
        neighbors[to].Add(from);
        var edge = from < to ? new GoRenGraphEdge(from, to) : new GoRenGraphEdge(to, from);
        if (seen.Add(edge))
        {
            edges.Add(edge);
        }
    }

    private static void ApplyEyeJudgement(
        int renCount,
        GoStone[] stones,
        List<GoPoint>[] points,
        HashSet<int>[] neighbors,
        List<int>[] eyeNumbers,
        int?[] eyeOwnerRenNumbers)
    {
        for (var renNumber = 1; renNumber <= renCount; renNumber++)
        {
            if (stones[renNumber] != GoStone.Empty || points[renNumber].Count != 1 || neighbors[renNumber].Count != 1)
            {
                continue;
            }

            var ownerRenNumber = neighbors[renNumber].Single();
            if (stones[ownerRenNumber] == GoStone.Empty)
            {
                continue;
            }

            eyeOwnerRenNumbers[renNumber] = ownerRenNumber;
            eyeNumbers[ownerRenNumber].Add(renNumber);
        }
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

    private int CountLiberties(List<(int X, int Y)> ren)
    {
        var liberties = new HashSet<(int X, int Y)>();
        foreach (var point in ren)
        {
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (_stones[neighbor.X, neighbor.Y] == GoStone.Empty)
                {
                    liberties.Add(neighbor);
                }
            }
        }

        return liberties.Count;
    }

    private int RemoveRen(List<(int X, int Y)> ren, List<(int X, int Y, GoStone Stone)> removedStones)
    {
        foreach (var point in ren)
        {
            removedStones.Add((point.X, point.Y, _stones[point.X, point.Y]));
            SetStone(point.X, point.Y, GoStone.Empty);
        }

        return ren.Count;
    }

    private void RestoreStones(List<(int X, int Y, GoStone Stone)> removedStones)
    {
        foreach (var stone in removedStones)
        {
            SetStone(stone.X, stone.Y, stone.Stone);
        }
    }

    private void SetStone(int x, int y, GoStone stone)
    {
        var oldStone = _stones[x, y];
        if (oldStone == stone)
        {
            return;
        }

        if (oldStone != GoStone.Empty)
        {
            CurrentHash ^= _zobristTable[x, y, StoneHashIndex(oldStone)];
        }

        _stones[x, y] = stone;

        if (stone != GoStone.Empty)
        {
            CurrentHash ^= _zobristTable[x, y, StoneHashIndex(stone)];
        }
    }

    private static int StoneHashIndex(GoStone stone) => stone == GoStone.Black ? 0 : 1;

    private static ulong[,,] CreateZobristTable(int size)
    {
        var table = new ulong[size, size, 2];
        var state = 0x9e3779b97f4a7c15UL ^ (ulong)size;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                for (var stone = 0; stone < 2; stone++)
                {
                    table[x, y, stone] = NextRandomUlong(ref state);
                }
            }
        }

        return table;
    }

    private static ulong NextRandomUlong(ref ulong state)
    {
        state += 0x9e3779b97f4a7c15UL;
        var value = state;
        value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
        value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
        return value ^ (value >> 31);
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
