namespace KifuwarabeGo2026.FormalAdapter.Gtp.Protocol;

/// <summary>
/// Provides the minimum command transport needed by GTP extension logic.
/// </summary>
public interface IGtpCommandSession
{
    Task<GtpCommandResult> SendAsync(
        string command,
        CancellationToken cancellationToken = default);
}
