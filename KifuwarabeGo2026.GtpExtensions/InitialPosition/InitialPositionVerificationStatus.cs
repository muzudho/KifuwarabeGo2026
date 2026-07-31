namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

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
