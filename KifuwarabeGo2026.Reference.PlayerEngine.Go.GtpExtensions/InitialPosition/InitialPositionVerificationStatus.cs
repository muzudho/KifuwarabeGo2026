namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

/// <summary>
/// Describes how confidently an engine response reproduces a requested position.
/// </summary>
public enum InitialPositionVerificationStatus
{
    Verified,
    Unverified,
    PositionMismatch,
    InvalidResponse,
}
