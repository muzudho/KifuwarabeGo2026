namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

/// <summary>
/// Identifies where a user-requested "try another method" continuation resumes.
/// </summary>
public sealed record InitialPositionConciergeCursor(
    int NextStrategyIndex,
    bool RecoveryRequired);
