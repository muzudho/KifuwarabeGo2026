namespace KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;

/// <summary>
/// Reports the effect of a result declaration, resume request, or adjudication.
/// </summary>
public sealed record MatchResultUpdate(
    bool Accepted,
    bool Changed,
    bool Completed,
    MatchSnapshot Snapshot);
