namespace KifuwarabeGo2026.Match;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Identifies the platform-independent match assembly until its first domain API is introduced.
/// </summary>
public static class MatchProject
{
    /// <summary>
    /// Gets a Shared contract type used by the match assembly.
    /// </summary>
    public static Type SharedContractType => typeof(GoPoint);
}
