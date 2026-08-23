namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;

/// <summary>
/// Describes how an engine must be cleaned before another setup method is attempted.
/// </summary>
public enum InitialPositionRecoveryMode
{
    ClearBoard,
    RestartSession,
}
