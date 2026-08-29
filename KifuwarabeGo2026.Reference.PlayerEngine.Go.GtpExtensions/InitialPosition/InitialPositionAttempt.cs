namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

using System.Collections.ObjectModel;

/// <summary>
/// Provides immutable progress and diagnostic information for one setup method.
/// </summary>
public sealed class InitialPositionAttempt
{
    private readonly ReadOnlyCollection<string> _commands;

    public InitialPositionAttempt(
        InitialPositionMethod method,
        string methodDisplayName,
        InitialPositionAttemptStatus status,
        DateTimeOffset startedAt,
        TimeSpan duration,
        IEnumerable<string>? commands = null,
        string? failedCommand = null,
        string? engineResponse = null,
        string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodDisplayName);
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Attempt duration cannot be negative.");
        }

        Method = method;
        MethodDisplayName = methodDisplayName;
        Status = status;
        StartedAt = startedAt;
        Duration = duration;
        _commands = Array.AsReadOnly(commands?.ToArray() ?? []);
        FailedCommand = failedCommand;
        EngineResponse = engineResponse;
        Detail = detail;
    }

    public InitialPositionMethod Method { get; }

    public string MethodDisplayName { get; }

    public InitialPositionAttemptStatus Status { get; }

    public DateTimeOffset StartedAt { get; }

    public TimeSpan Duration { get; }

    public IReadOnlyList<string> Commands => _commands;

    public string? FailedCommand { get; }

    public string? EngineResponse { get; }

    public string? Detail { get; }
}
