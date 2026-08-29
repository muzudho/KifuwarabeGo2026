namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Sgf;

/// <summary>
/// Supplies host-owned command transport, session recovery, and temporary-document materialization.
/// </summary>
public interface IInitialPositionExecutionHost : IGtpCommandSession
{
    Task RecoverAsync(
        InitialPositionRecoveryMode recoveryMode,
        CancellationToken cancellationToken = default);

    Task<IInitialPositionDocumentLease> MaterializeAsync(
        InitialPositionDocument document,
        CancellationToken cancellationToken = default);
}
