namespace KifuwarabeGo2026.Reference.Communication.Gtp;

using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Adapts the GUI-owned engine process client to the transport required by GtpExtensions.
/// </summary>
public sealed class GtpEngineClientCommandSession : IGtpCommandSession
{
    private readonly GtpEngineClient _client;

    public GtpEngineClientCommandSession(GtpEngineClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public async Task<GtpCommandResult> SendAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.SendCommandAsync(command, cancellationToken);
        return new GtpCommandResult(response.IsSuccess, response.Payload);
    }
}
