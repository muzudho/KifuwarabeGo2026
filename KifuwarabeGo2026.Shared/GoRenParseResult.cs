namespace KifuwarabeGo2026.Shared.Domain;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// ［連解析の結果］
/// </summary>
public sealed class GoRenParseResult
{
    private readonly int[,] _renNumbers;
    private readonly GoRen[] _rens;

    public GoRenParseResult(int[,] renNumbers, IReadOnlyList<GoRen> rens, IReadOnlyList<GoRenGraphEdge> edges)
    {
        _renNumbers = renNumbers;
        _rens = new GoRen[rens.Count + 1];
        foreach (var ren in rens)
        {
            if (ren.Number <= 0 || ren.Number >= _rens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(rens), "Ren number is outside the result range.");
            }

            _rens[ren.Number] = ren;
        }

        Count = rens.Count;
        Size = renNumbers.GetLength(0);
        Edges = new ReadOnlyCollection<GoRenGraphEdge>(edges.ToArray());
    }

    public int Size { get; }

    public int Count { get; }

    public IReadOnlyList<GoRenGraphEdge> Edges { get; }

    public int GetRenNumber(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Point is outside the board.");
        }

        return _renNumbers[x, y];
    }

    public GoRen GetRen(int renNumber)
    {
        if (renNumber <= 0 || renNumber > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(renNumber), "Ren number is outside the result range.");
        }

        return _rens[renNumber];
    }
}

public sealed class GoRen
{
    public GoRen(
        int number,
        GoStone stone,
        IReadOnlyList<GoPoint> points,
        IReadOnlyList<int> neighborRenNumbers,
        IReadOnlyList<int> eyeRenNumbers,
        int? eyeOwnerRenNumber)
    {
        Number = number;
        Stone = stone;
        Points = new ReadOnlyCollection<GoPoint>(points.ToArray());
        NeighborRenNumbers = new ReadOnlyCollection<int>(neighborRenNumbers.ToArray());
        EyeRenNumbers = new ReadOnlyCollection<int>(eyeRenNumbers.ToArray());
        EyeOwnerRenNumber = eyeOwnerRenNumber;
    }

    public int Number { get; }

    public GoStone Stone { get; }

    public IReadOnlyList<GoPoint> Points { get; }

    public IReadOnlyList<int> NeighborRenNumbers { get; }

    public IReadOnlyList<int> EyeRenNumbers { get; }

    public int? EyeOwnerRenNumber { get; }

    public bool IsEye => Stone == GoStone.Empty && EyeOwnerRenNumber is not null;
}

public readonly record struct GoRenGraphEdge(int From, int To);
