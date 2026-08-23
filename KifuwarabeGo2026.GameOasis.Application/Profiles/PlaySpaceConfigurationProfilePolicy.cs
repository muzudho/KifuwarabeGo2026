namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

public static class PlaySpaceConfigurationProfilePolicy
{
    public static PlaySpaceConfigurationProfile Normalize(PlaySpaceConfigurationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (string.IsNullOrWhiteSpace(profile.PlaySpaceId))
            throw new ArgumentException("Play-space ID is required.", nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.ConfigurationDocument))
            throw new ArgumentException("Configuration document is required.", nameof(profile));
        return profile with
        {
            Id = string.IsNullOrWhiteSpace(profile.Id) ? Guid.NewGuid().ToString("N") : profile.Id.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "Unnamed configuration" : profile.DisplayName.Trim(),
            PlaySpaceId = profile.PlaySpaceId.Trim(),
        };
    }
}
