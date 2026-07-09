namespace KifuwarabeGo2026.Gtp;

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
