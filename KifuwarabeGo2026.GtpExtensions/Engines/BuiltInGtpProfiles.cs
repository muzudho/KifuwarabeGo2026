namespace KifuwarabeGo2026.GtpExtensions.Engines;

using KifuwarabeGo2026.GtpExtensions.Capabilities;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Protocol;
using KifuwarabeGo2026.GtpExtensions.Strategies;

/// <summary>
/// Resolves conservative built-in compatibility profiles from an explicit id or probed engine identity.
/// </summary>
public static class BuiltInGtpProfiles
{
    public const string AutoId = "auto";
    public const string KifuwarabeId = "kifuwarabe";
    public const string KataGoId = "katago";
    public const string LeelaZeroId = "leela-zero";
    public const string GnuGoId = "gnu-go";

    public static IGtpEngineCompatibilityProfile Resolve(
        GtpCapabilitySet capabilities,
        string? requestedProfileId = null,
        InitialPositionMethod? preferredMethod = null)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var profile = ResolveBase(capabilities.EngineName, requestedProfileId);
        return preferredMethod is null
            ? profile
            : new PreferredMethodGtpProfile(profile, preferredMethod.Value);
    }

    public static IGtpEngineCompatibilityProfile ResolveBase(string? engineName, string? requestedProfileId = null)
    {
        var requested = requestedProfileId?.Trim();
        if (!string.IsNullOrEmpty(requested) && !requested.Equals(AutoId, StringComparison.OrdinalIgnoreCase))
        {
            return FromId(requested);
        }

        var normalizedName = engineName?.Trim() ?? string.Empty;
        if (Contains(normalizedName, "kifuwarabe") || Contains(normalizedName, "きふわらべ"))
            return KifuwarabeGtpProfile.Instance;
        if (Contains(normalizedName, "katago"))
            return KataGoGtpProfile.Instance;
        if (Contains(normalizedName, "leela zero") || Contains(normalizedName, "leelaz"))
            return LeelaZeroGtpProfile.Instance;
        if (Contains(normalizedName, "gnu go") || Contains(normalizedName, "gnugo"))
            return GnuGoGtpProfile.Instance;
        return GenericGtpProfile.Instance;
    }

    public static IGtpEngineCompatibilityProfile FromId(string? profileId) => profileId?.Trim().ToLowerInvariant() switch
    {
        KifuwarabeId => KifuwarabeGtpProfile.Instance,
        KataGoId => KataGoGtpProfile.Instance,
        LeelaZeroId => LeelaZeroGtpProfile.Instance,
        GnuGoId => GnuGoGtpProfile.Instance,
        _ => GenericGtpProfile.Instance,
    };

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);
}

public sealed class KifuwarabeGtpProfile : BuiltInGtpProfile
{
    public static KifuwarabeGtpProfile Instance { get; } = new();
    private KifuwarabeGtpProfile() : base(BuiltInGtpProfiles.KifuwarabeId, "きふわらべ", StandardStrategies) { }
}

public sealed class KataGoGtpProfile : BuiltInGtpProfile
{
    public static KataGoGtpProfile Instance { get; } = new();
    private KataGoGtpProfile() : base(BuiltInGtpProfiles.KataGoId, "KataGo", StandardStrategies) { }
}

public sealed class LeelaZeroGtpProfile : BuiltInGtpProfile
{
    public static LeelaZeroGtpProfile Instance { get; } = new();
    private LeelaZeroGtpProfile() : base(BuiltInGtpProfiles.LeelaZeroId, "Leela Zero", StandardStrategies) { }
}

public sealed class GnuGoGtpProfile : BuiltInGtpProfile
{
    public static GnuGoGtpProfile Instance { get; } = new();
    private GnuGoGtpProfile() : base(BuiltInGtpProfiles.GnuGoId, "GNU Go", StandardStrategies) { }
}

public abstract class BuiltInGtpProfile : IGtpEngineCompatibilityProfile
{
    protected static IReadOnlyList<IInitialPositionStrategy> StandardStrategies { get; } =
    [
        FixedHandicapStrategy.Instance,
        SetFreeHandicapStrategy.Instance,
        LoadSgfStrategy.Instance,
        SequentialPlayStrategy.Instance,
    ];

    protected BuiltInGtpProfile(string id, string displayName, IReadOnlyList<IInitialPositionStrategy> strategies)
    {
        Id = id;
        DisplayName = displayName;
        Strategies = strategies;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<IInitialPositionStrategy> Strategies { get; }
    public InitialPositionRecoveryMode RecoveryAfterAttempt => InitialPositionRecoveryMode.RestartSession;
    public GtpFilePathArgumentStyle LoadSgfPathStyle => GtpFilePathArgumentStyle.Auto;
    public int? LoadSgfMoveNumber => null;
}

internal sealed class PreferredMethodGtpProfile : IGtpEngineCompatibilityProfile
{
    public PreferredMethodGtpProfile(IGtpEngineCompatibilityProfile source, InitialPositionMethod preferredMethod)
    {
        Id = source.Id;
        DisplayName = source.DisplayName;
        RecoveryAfterAttempt = source.RecoveryAfterAttempt;
        LoadSgfPathStyle = source.LoadSgfPathStyle;
        LoadSgfMoveNumber = source.LoadSgfMoveNumber;
        Strategies = source.Strategies
            .OrderBy(strategy => strategy.Method == preferredMethod ? 0 : 1)
            .ToArray();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public IReadOnlyList<IInitialPositionStrategy> Strategies { get; }
    public InitialPositionRecoveryMode RecoveryAfterAttempt { get; }
    public GtpFilePathArgumentStyle LoadSgfPathStyle { get; }
    public int? LoadSgfMoveNumber { get; }
}
