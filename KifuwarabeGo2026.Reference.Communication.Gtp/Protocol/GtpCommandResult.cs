namespace KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;

/// <summary>
/// Represents one parsed success or error response from a GTP engine.
/// </summary>
public sealed record GtpCommandResult(bool IsSuccess, string Payload);
