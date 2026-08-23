namespace KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;

/// <summary>
/// Provides the minimum command transport needed by GTP extension logic.
/// </summary>
public interface IGtpCommandSession
{
    Task<GtpCommandResult> SendAsync(
        string command,
        CancellationToken cancellationToken = default);
}
