namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>A persistent, game-agnostic named configuration for a play-space.</summary>
public sealed record PlaySpaceConfigurationProfile
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string PlaySpaceId { get; init; } = "";
    public string ConfigurationDocument { get; init; } = "";
}
