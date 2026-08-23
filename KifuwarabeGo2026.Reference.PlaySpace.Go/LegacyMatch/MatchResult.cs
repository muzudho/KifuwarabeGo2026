namespace KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;

/// <summary>
/// Represents a structured result without presentation-specific result text.
/// </summary>
public sealed record MatchResult
{
    public MatchResult(MatchOutcome outcome, decimal? margin = null)
    {
        if (margin is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin), margin, "A winning margin must be positive.");
        }

        if ((outcome is MatchOutcome.Draw or MatchOutcome.NoResult) && margin is not null)
        {
            throw new ArgumentException("A draw or no-result outcome cannot have a winning margin.", nameof(margin));
        }

        Outcome = outcome;
        Margin = margin;
    }

    public MatchOutcome Outcome { get; }

    public decimal? Margin { get; }
}
