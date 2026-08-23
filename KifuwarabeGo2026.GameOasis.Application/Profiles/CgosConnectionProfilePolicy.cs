namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>Normalization rules for persistent CGOS connection destinations.</summary>
public static class CgosConnectionProfilePolicy
{
    public static CgosConnectionProfile Normalize(CgosConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var displayName = string.IsNullOrWhiteSpace(profile.DisplayName)
            ? "Unnamed CGOS Connection" : profile.DisplayName.Trim();
        var host = string.IsNullOrWhiteSpace(profile.Host) ? "uec-go.com" : profile.Host.Trim();
        var port = Math.Clamp(profile.Port, 1, 65535);
        var round = string.IsNullOrWhiteSpace(profile.Round) ? "-" : profile.Round.Trim();
        return profile with
        {
            Id = string.IsNullOrWhiteSpace(profile.Id) ? Guid.NewGuid().ToString("N") : profile.Id.Trim(),
            Kind = ConnectionProfileKind.Cgos,
            EndpointKey = $"cgos://{host.TrimEnd('.').ToLowerInvariant()}:{port}",
            DisplayName = displayName,
            Host = host,
            Port = port,
            Event = profile.Event?.Trim() ?? "",
            Round = round,
            Note = profile.Note?.Trim() ?? "",
        };
    }

    public static bool ListsAreEqual(
        IReadOnlyList<CgosConnectionProfile> left, IReadOnlyList<CgosConnectionProfile> right) =>
        left.Count == right.Count && left.Zip(right).All(pair => pair.First == pair.Second);
}
