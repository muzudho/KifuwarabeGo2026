namespace KifuwarabeGo2026.Gui.Gtp;

using KifuwarabeGo2026.Gui.Domain;
using System;

public static class GtpCoordinate
{
    public static string FormatVertex(GoPoint point, int boardSize)
    {
        if (point.X < 0 || point.X >= boardSize || point.Y < 0 || point.Y >= boardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Point is outside the board.");
        }

        var column = (char)('A' + point.X);
        if (column >= 'I')
        {
            column++;
        }

        return $"{column}{boardSize - point.Y}";
    }

    public static bool TryParseVertex(string text, int boardSize, out GoPoint point)
    {
        point = default;
        if (text.Length < 2 || IsPass(text))
        {
            return false;
        }

        var column = char.ToUpperInvariant(text[0]);
        if (column >= 'I')
        {
            column--;
        }

        var x = column - 'A';
        if (!int.TryParse(text[1..], out var row))
        {
            return false;
        }

        var y = boardSize - row;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            return false;
        }

        point = new GoPoint(x, y);
        return true;
    }

    public static bool IsPass(string text) => text.Equals("pass", StringComparison.OrdinalIgnoreCase);
}
