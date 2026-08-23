namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Sgf;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using System.Globalization;
using System.Text;

/// <summary>
/// Builds a minimal SGF root node for an initial position without performing file I/O.
/// </summary>
public static class InitialPositionSgfBuilder
{
    public static InitialPositionDocument Build(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new StringBuilder("(;GM[1]FF[4]CA[UTF-8]");
        AppendProperty(builder, "SZ", request.BoardSize.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "KM", request.Komi.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "PL", request.StartingTurn == GoStone.Black ? "B" : "W");
        AppendSetupStones(builder, request, GoStone.Black, "AB");
        AppendSetupStones(builder, request, GoStone.White, "AW");
        builder.Append(')').Append('\n');
        return new InitialPositionDocument("initial-position.sgf", builder.ToString());
    }

    private static void AppendSetupStones(
        StringBuilder builder,
        InitialPositionRequest request,
        GoStone stone,
        string propertyName)
    {
        var matchingStones = request.SetupStones.Where(setupStone => setupStone.Stone == stone).ToArray();
        if (matchingStones.Length == 0)
        {
            return;
        }

        builder.Append(propertyName);
        foreach (var setupStone in matchingStones)
        {
            builder.Append('[')
                .Append(FormatPoint(setupStone.Point, request.BoardSize))
                .Append(']');
        }
    }

    private static string FormatPoint(GoPoint point, int boardSize)
    {
        if (point.X < 0 || point.X >= boardSize || point.Y < 0 || point.Y >= boardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Point is outside the SGF board.");
        }

        return $"{(char)('a' + point.X)}{(char)('a' + point.Y)}";
    }

    private static void AppendProperty(StringBuilder builder, string name, string value)
    {
        builder.Append(name).Append('[').Append(EscapeValue(value)).Append(']');
    }

    private static string EscapeValue(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);
}
