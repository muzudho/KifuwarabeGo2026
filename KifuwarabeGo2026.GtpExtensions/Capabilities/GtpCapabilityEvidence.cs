namespace KifuwarabeGo2026.GtpExtensions.Capabilities;

/// <summary>
/// Describes the evidence used to decide command support.
/// </summary>
public enum GtpCapabilityEvidence
{
    KnownCommand,
    ListCommands,
    ConsistentResponses,
    ContradictoryResponses,
    Unavailable,
}
