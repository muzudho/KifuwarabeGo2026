namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

using System.Collections.ObjectModel;

/// <summary>
/// Contains one concierge run, including skipped methods and any continuation cursor.
/// </summary>
public sealed class InitialPositionConciergeResult
{
    private readonly ReadOnlyCollection<InitialPositionAttempt> _attempts;
    private readonly ReadOnlyCollection<string> _diagnostics;

    public InitialPositionConciergeResult(
        IEnumerable<InitialPositionAttempt> attempts,
        InitialPositionConciergeCursor? continuation,
        IEnumerable<string>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(attempts);
        _attempts = Array.AsReadOnly(attempts.ToArray());
        _diagnostics = Array.AsReadOnly(diagnostics?.ToArray() ?? []);
        Continuation = continuation;
    }

    public IReadOnlyList<InitialPositionAttempt> Attempts => _attempts;

    public InitialPositionConciergeCursor? Continuation { get; }

    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public bool CanTryAnotherMethod => Continuation is not null;

    public InitialPositionAttempt? LastAttempt => _attempts.LastOrDefault();

    public bool IsVerified => LastAttempt?.Status == InitialPositionAttemptStatus.VerifiedSuccess;

    public bool IsUnverifiedSuccess => LastAttempt?.Status == InitialPositionAttemptStatus.UnverifiedSuccess;
}
