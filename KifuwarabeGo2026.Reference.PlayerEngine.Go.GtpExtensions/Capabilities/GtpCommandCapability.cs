namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Capabilities;

/// <summary>
/// Stores one command's support state and the evidence behind it.
/// </summary>
public sealed record GtpCommandCapability(
    string Command,
    GtpCommandSupport Support,
    GtpCapabilityEvidence Evidence,
    string? Detail = null);
