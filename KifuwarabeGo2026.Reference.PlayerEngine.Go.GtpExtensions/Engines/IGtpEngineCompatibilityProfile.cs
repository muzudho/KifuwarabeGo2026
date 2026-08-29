namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Strategies;

/// <summary>
/// Defines setup-method ordering and safe recovery behavior for one engine family.
/// </summary>
public interface IGtpEngineCompatibilityProfile
{
    string Id { get; }

    string DisplayName { get; }

    GtpProfileEvidence Evidence { get; }

    IReadOnlyList<IInitialPositionStrategy> Strategies { get; }

    InitialPositionRecoveryMode RecoveryAfterAttempt { get; }

    GtpFilePathArgumentStyle LoadSgfPathStyle { get; }

    int? LoadSgfMoveNumber { get; }
}
