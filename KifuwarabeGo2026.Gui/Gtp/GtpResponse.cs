namespace KifuwarabeGo2026.Gui.Gtp;

using System;

public sealed record GtpResponse(bool IsSuccess, string Payload)
{
    public void ThrowIfError(string command)
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException($"GTP command failed: {command}: {Payload}");
        }
    }
}
