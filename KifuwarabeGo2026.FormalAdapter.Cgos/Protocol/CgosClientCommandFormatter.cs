namespace KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

public static class CgosClientCommandFormatter
{
    public static string Format(CgosClientCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var line = command switch
        {
            CgosClientIdentity identity => Token(identity.ClientId, nameof(identity.ClientId)) +
                (identity.SupportsGenMoveAnalyze ? " genmove_analyze" : ""),
            CgosUsername username => Token(username.Value, nameof(username.Value)),
            CgosPassword password => Token(password.Value, nameof(password.Value)),
            CgosMove move => Token(move.Vertex, nameof(move.Vertex)) +
                (move.AnalysisJson is null ? "" : " " + SingleLine(move.AnalysisJson, nameof(move.AnalysisJson))),
            CgosResign => "resign",
            CgosReady => "ready",
            CgosQuit => "quit",
            CgosWho => "who",
            CgosMatch match => string.IsNullOrWhiteSpace(match.Arguments)
                ? "match" : "match " + SingleLine(match.Arguments.Trim(), nameof(match.Arguments)),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown CGOS client command."),
        };
        return SingleLine(line, nameof(command));
    }

    public static string FormatForLog(CgosClientCommand command) => command.IsSensitive ? "(password)" : Format(command);

    private static string Token(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace))
            throw new ArgumentException("A CGOS token cannot be empty or contain whitespace.", parameterName);
        return SingleLine(value, parameterName);
    }
    private static string SingleLine(string value, string parameterName)
    {
        if (value.IndexOfAny(['\r', '\n', '\0']) >= 0)
            throw new ArgumentException("A CGOS command cannot contain a line break or NUL.", parameterName);
        return value;
    }
}
