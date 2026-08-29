namespace KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility;

using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

/// <summary>Converts pre-JSON-Lines Host display logs into current notifications.</summary>
public static class CgosLegacyLogNotificationAdapter
{
    public static bool TryParse(string displayLine, out CgosNotification? notification)
    {
        notification = null;
        if (string.IsNullOrWhiteSpace(displayLine)) return false;
        var serverMarker = displayLine.IndexOf("] > ", StringComparison.Ordinal);
        if (serverMarker >= 0)
        {
            CgosServerMessage message;
            var legacyCommand = displayLine[(serverMarker + 4)..];
            try { message = CgosServerMessageParser.Parse(NormalizeLegacyColor(legacyCommand)); }
            catch (CgosProtocolException) { return false; }
            notification = message switch
            {
                CgosMatchSetup setup => new CgosSetupNotification(
                    "legacy", setup.GameId, setup.BoardSize, setup.Komi, setup.MainTimeMilliseconds,
                    setup.WhitePlayer, setup.BlackPlayer, setup.MoveHistory),
                CgosMovePlayed play => new CgosPlayNotification(
                    "legacy", play.Color, play.Vertex, play.TimeLeftMilliseconds),
                CgosGameOver gameOver => new CgosGameOverNotification("legacy", gameOver.Result),
                _ => null,
            };
            return notification is not null;
        }

        var generatedMarker = displayLine.IndexOf("] # Generated ", StringComparison.Ordinal);
        if (generatedMarker < 0) return false;
        var fields = displayLine[(generatedMarker + 14)..]
            .Split(' ', 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length < 3 || !fields[1].Equals("move:", StringComparison.OrdinalIgnoreCase)) return false;
        notification = new CgosPlayNotification(
            "legacy", fields[0], fields[2], null, fields.Length >= 4 ? fields[3] : null, IsGenerated: true);
        return true;
    }

    private static string NormalizeLegacyColor(string commandLine)
    {
        if (commandLine.StartsWith("play black ", StringComparison.OrdinalIgnoreCase))
            return "play b " + commandLine[11..];
        if (commandLine.StartsWith("play white ", StringComparison.OrdinalIgnoreCase))
            return "play w " + commandLine[11..];
        return commandLine;
    }
}
