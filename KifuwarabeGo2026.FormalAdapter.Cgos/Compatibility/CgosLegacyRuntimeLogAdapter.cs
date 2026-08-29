namespace KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility;

public enum CgosLegacyProcessState
{
    Running,
    Starting,
    Connecting,
    Protocol,
    Login,
    AdminReady,
    AdminCommand,
    Setup,
    Play,
    GenMove,
    GtpWait,
    GenMoveDone,
    GameOver,
    Closed,
    Error,
}

public enum CgosLegacyGtpWaitTransition { None, Started, Completed }

/// <summary>Contains all semantic knowledge of pre-JSON-Lines Host status logs.</summary>
public static class CgosLegacyRuntimeLogAdapter
{
    public static CgosLegacyProcessState DeriveProcessState(IReadOnlyList<string> output)
    {
        foreach (var line in output.Reverse())
        {
            if (ContainsAny(line, "CGOS error", "[StandardError]", "[Error]", "Unhandled exception",
                    "Unsupported CGOS command", "Could not connect", "TCP connect timed out", "TCP connect failed"))
                return CgosLegacyProcessState.Error;
            if (line.Contains("CGOS connection closed", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.Closed;
            if (line.Contains("Generated ", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.GenMoveDone;
            if (line.Contains("GTP response wait started:", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.GtpWait;
            if (ContainsAny(line, "> genmove", "< genmove")) return CgosLegacyProcessState.GenMove;
            if (ContainsAny(line, "> play", "< play")) return CgosLegacyProcessState.Play;
            if (ContainsAny(line, "Setup game", "> setup", "< setup")) return CgosLegacyProcessState.Setup;
            if (line.Contains("Game over", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.GameOver;
            if (ContainsAny(line, "> username", "> password", "< (password)", "< username", "< password", "> (password)"))
                return CgosLegacyProcessState.Login;
            if (line.Contains("Admin command input is ready", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.AdminReady;
            if (ContainsAny(line, "Sent admin command", "< who", "< match")) return CgosLegacyProcessState.AdminCommand;
            if (ContainsAny(line, "> protocol", "< protocol")) return CgosLegacyProcessState.Protocol;
            if (line.Contains("Connecting to", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.Connecting;
            if (line.Contains("Started CGOS communication process", StringComparison.OrdinalIgnoreCase)) return CgosLegacyProcessState.Starting;
        }
        return CgosLegacyProcessState.Running;
    }

    public static CgosLegacyGtpWaitTransition GetGtpWaitTransition(string line)
    {
        if (line.Contains("GTP response wait started:", StringComparison.OrdinalIgnoreCase)) return CgosLegacyGtpWaitTransition.Started;
        if (line.Contains("GTP response wait completed", StringComparison.OrdinalIgnoreCase)) return CgosLegacyGtpWaitTransition.Completed;
        return CgosLegacyGtpWaitTransition.None;
    }

    public static bool TryGetWaitingPlayer(string line, out string player)
    {
        player = "";
        var marker = line.IndexOf("] > ", StringComparison.Ordinal);
        if (marker < 0) return false;
        var parts = line[(marker + 4)..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || !parts[1].Equals("waiting", StringComparison.OrdinalIgnoreCase)) return false;
        player = parts[0];
        return player.Length > 0;
    }

    private static bool ContainsAny(string line, params string[] values) =>
        values.Any(value => line.Contains(value, StringComparison.OrdinalIgnoreCase));
}
