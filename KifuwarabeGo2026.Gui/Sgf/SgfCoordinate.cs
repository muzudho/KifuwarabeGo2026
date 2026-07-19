namespace KifuwarabeGo2026.Sgf;

using KifuwarabeGo2026.Domain;
using System;

public static class SgfCoordinate
{
    public static string FormatPoint(GoPoint? point, int boardSize)
    {
        if (point is null)
        {
            return "";
        }

        ValidateBoardSize(boardSize);
        var value = point.Value;
        if (value.X < 0 || value.X >= boardSize || value.Y < 0 || value.Y >= boardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Point is outside the board.");
        }

        return $"{(char)('a' + value.X)}{(char)('a' + value.Y)}";
    }

    public static bool TryParsePoint(string text, int boardSize, out GoPoint? point)
    {
        ValidateBoardSize(boardSize);
        point = null;
        if (text.Length == 0)
        {
            return true;
        }

        if (text.Length != 2)
        {
            return false;
        }

        var x = text[0] - 'a';
        var y = text[1] - 'a';
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            return false;
        }

        point = new GoPoint(x, y);
        return true;
    }

    private static void ValidateBoardSize(int boardSize)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }
    }
}
