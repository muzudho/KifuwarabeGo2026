namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Strategies;

/// <summary>
/// Uses only probed capabilities and the safest generic recovery policy.
/// </summary>
public sealed class GenericGtpProfile : IGtpEngineCompatibilityProfile
{
    public static GenericGtpProfile Instance { get; } = new();

    private GenericGtpProfile()
    {
    }

    public string Id => "generic-gtp";

    public string DisplayName => "汎用GTP";

    public GtpProfileEvidence Evidence => GtpProfileEvidence.ConservativeFallback;

    public IReadOnlyList<IInitialPositionStrategy> Strategies { get; } =
    [
        FixedHandicapStrategy.Instance,
        SetFreeHandicapStrategy.Instance,
        LoadSgfStrategy.Instance,
        SequentialPlayStrategy.Instance,
    ];

    public InitialPositionRecoveryMode RecoveryAfterAttempt => InitialPositionRecoveryMode.RestartSession;

    public GtpFilePathArgumentStyle LoadSgfPathStyle => GtpFilePathArgumentStyle.Auto;

    public int? LoadSgfMoveNumber => null;
}
