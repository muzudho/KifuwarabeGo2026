namespace KifuwarabeGo2026.Domain;

using System;

public sealed class GoRenParseResult
{
    private readonly int[,] _renNumbers;

    public GoRenParseResult(int[,] renNumbers, int count)
    {
        _renNumbers = renNumbers;
        Count = count;
        Size = renNumbers.GetLength(0);
    }

    public int Size { get; }

    public int Count { get; }

    public int GetRenNumber(int x, int y)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Point is outside the board.");
        }

        return _renNumbers[x, y];
    }
}
