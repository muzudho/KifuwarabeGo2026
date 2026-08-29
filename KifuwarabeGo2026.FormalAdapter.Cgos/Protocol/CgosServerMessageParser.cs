namespace KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

using System.Globalization;

public static class CgosServerMessageParser
{
    public static CgosServerMessage Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        line = line.Trim();
        if (line.Length == 0) throw new CgosProtocolException("A CGOS server message cannot be empty.", line);
        if (line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
            return new CgosServerError(line, line[6..].Trim());

        var split = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = split[0].ToLowerInvariant();
        var arguments = split.Length == 2
            ? split[1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
        return command switch
        {
            "protocol" => new CgosProtocolAdvertised(line, split.Length == 2 ? split[1] : "",
                split.Length == 2 && split[1].Contains("genmove_analyze", StringComparison.OrdinalIgnoreCase)),
            "username" => RequireCount(arguments, 0, line, new CgosUsernameRequested(line)),
            "password" => RequireCount(arguments, 0, line, new CgosPasswordRequested(line)),
            "ok" => new CgosLoginAccepted(line),
            "setup" => ParseSetup(line, arguments),
            "play" => ParsePlay(line, arguments),
            "genmove" => ParseGenMove(line, arguments),
            "gameover" => ParseGameOver(line, split.Length == 2 ? split[1] : ""),
            "info" => new CgosInfoMessage(line, split.Length == 2 ? split[1] : ""),
            _ => new CgosUnknownServerMessage(line, command, arguments),
        };
    }

    private static CgosMatchSetup ParseSetup(string line, string[] values)
    {
        if (values.Length < 6 || (values.Length - 6) % 2 != 0)
            throw new CgosProtocolException("CGOS setup requires six fields followed by vertex/time pairs.", line);
        var gameId = Integer(values[0], "game ID", line);
        var boardSize = Integer(values[1], "board size", line);
        if (boardSize <= 0) throw new CgosProtocolException("CGOS board size must be positive.", line);
        if (!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var komi))
            throw new CgosProtocolException("CGOS setup has an invalid komi.", line);
        var mainTime = Long(values[3], "main time", line);
        var history = new List<CgosHistoricalMove>();
        var color = "b";
        for (var index = 6; index < values.Length; index += 2)
        {
            history.Add(new CgosHistoricalMove(color, Vertex(values[index], line), Long(values[index + 1], "move time", line)));
            color = color == "b" ? "w" : "b";
        }
        return new CgosMatchSetup(line, gameId, boardSize, komi, mainTime, StripRank(values[4]), StripRank(values[5]), history);
    }

    private static CgosMovePlayed ParsePlay(string line, string[] values)
    {
        if (values.Length != 3) throw new CgosProtocolException("CGOS play requires color, vertex, and time.", line);
        return new CgosMovePlayed(line, Color(values[0], line), Vertex(values[1], line), Long(values[2], "move time", line));
    }

    private static CgosGenMoveRequested ParseGenMove(string line, string[] values)
    {
        if (values.Length != 2) throw new CgosProtocolException("CGOS genmove requires color and time.", line);
        return new CgosGenMoveRequested(line, Color(values[0], line), Long(values[1], "move time", line));
    }

    private static CgosGameOver ParseGameOver(string line, string result)
    {
        if (string.IsNullOrWhiteSpace(result)) throw new CgosProtocolException("CGOS gameover requires a result.", line);
        return new CgosGameOver(line, result);
    }

    private static string Color(string value, string line)
    {
        value = value.ToLowerInvariant();
        return value is "b" or "w" ? value : throw new CgosProtocolException("CGOS color must be b or w.", line);
    }

    private static string Vertex(string value, string line)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            throw new CgosProtocolException("CGOS vertex must be one token.", line);
        return value;
    }

    private static int Integer(string value, string field, string line) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : throw new CgosProtocolException($"CGOS has an invalid {field}.", line);
    private static long Long(string value, string field, string line) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && result >= 0
            ? result : throw new CgosProtocolException($"CGOS has an invalid {field}.", line);
    private static string StripRank(string player)
    {
        var rankStart = player.LastIndexOf('(');
        return rankStart > 0 && player.EndsWith(')') ? player[..rankStart] : player;
    }
    private static T RequireCount<T>(string[] values, int count, string line, T result)
    {
        if (values.Length != count) throw new CgosProtocolException("CGOS login prompt has unexpected arguments.", line);
        return result;
    }
}
