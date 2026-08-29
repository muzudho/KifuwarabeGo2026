namespace KifuwarabeGo2026.FormalAdapter.Cgos.GameMasterEngine;

using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

/// <summary>Tracks CGOS administrator login readiness and translates operator input to typed commands.</summary>
public sealed class CgosAdminStateMachine
{
    public bool IsReady { get; private set; }

    public bool Handle(CgosServerMessage message)
    {
        if (message is not CgosLoginAccepted || IsReady) return false;
        IsReady = true;
        return true;
    }

    public bool TryCreateCommand(string input, out CgosClientCommand? command)
    {
        command = null;
        var text = input.Trim();
        if (!IsReady || text.Length == 0) return false;
        if (text.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            command = new CgosQuit();
            return true;
        }
        if (text.Equals("who", StringComparison.OrdinalIgnoreCase))
        {
            command = new CgosWho();
            return true;
        }
        if (text.Equals("match", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("match ", StringComparison.OrdinalIgnoreCase))
        {
            command = new CgosMatch(text.Length > 5 ? text[5..].Trim() : "");
            return true;
        }
        return false;
    }
}
