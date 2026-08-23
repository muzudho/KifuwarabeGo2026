namespace KifuwarabeGo2026.GameOasis.Gui.Gtp;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Sgf;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Adapts one GUI-owned local engine process to the concierge execution host contract.
/// </summary>
public sealed class GtpInitialPositionExecutionHost : IInitialPositionExecutionHost
{
    private readonly GtpEngineClient _client;

    public GtpInitialPositionExecutionHost(GtpEngineClient client)
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

    public async Task RecoverAsync(
        InitialPositionRecoveryMode recoveryMode,
        CancellationToken cancellationToken = default)
    {
        if (recoveryMode == InitialPositionRecoveryMode.RestartSession)
        {
            await _client.DisposeAsync();
            await _client.StartAsync(cancellationToken);
            return;
        }

        var response = await _client.SendCommandAsync("clear_board", cancellationToken);
        response.ThrowIfError("clear_board");
    }

    public Task<IInitialPositionDocumentLease> MaterializeAsync(
        InitialPositionDocument document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IInitialPositionDocumentLease>(
            GtpInitialPositionSgfFile.Create(document));
    }
}
