namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

/// <summary>
/// Describes the outcome of considering or executing one initial-position method.
/// </summary>
public enum InitialPositionAttemptStatus
{
    NotApplicable,
    Unsupported,
    CommandRejected,
    TransportFailure,
    PositionMismatch,
    InvalidResponse,
    UnverifiedSuccess,
    VerifiedSuccess,
}
