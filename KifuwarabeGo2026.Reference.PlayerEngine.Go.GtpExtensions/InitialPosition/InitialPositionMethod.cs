namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

/// <summary>
/// Identifies a method that can be attempted to reproduce an initial position.
/// </summary>
public enum InitialPositionMethod
{
    FixedHandicap,
    SetFreeHandicap,
    LoadSgf,
    KifuwarabeAtomicSetup,
    SequentialPlay,
}
