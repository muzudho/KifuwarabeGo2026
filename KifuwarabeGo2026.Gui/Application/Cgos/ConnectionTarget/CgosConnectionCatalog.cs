namespace KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CgosConnectionCatalog
{
    private CgosConnectionCatalog(IReadOnlyList<CgosConnectionProfile> profiles)
    {
        Profiles = profiles;
    }

    public string ListPath => ApplicationSettings.FilePath;

    public IReadOnlyList<CgosConnectionProfile> Profiles { get; }

    public static CgosConnectionCatalog LoadFromDefaultLocation()
    {
        var profiles = ApplicationSettings.Current.CgosConnections
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Host))
            .Select(Normalize)
            .ToList();
        return new CgosConnectionCatalog(profiles);
    }

    public void Save(IEnumerable<CgosConnectionProfile> profiles) =>
        ApplicationSettings.SaveCgosConnections(profiles.Select(Normalize));

    private static CgosConnectionProfile Normalize(CgosConnectionProfile profile)
    {
        var displayName = string.IsNullOrWhiteSpace(profile.DisplayName)
            ? "Unnamed CGOS Connection"
            : profile.DisplayName.Trim();
        var host = string.IsNullOrWhiteSpace(profile.Host)
            ? "uec-go.com"
            : profile.Host.Trim();
        var round = string.IsNullOrWhiteSpace(profile.Round)
            ? "-"
            : profile.Round.Trim();
        return profile with
        {
            DisplayName = displayName,
            Host = host,
            Port = Math.Clamp(profile.Port, 1, 65535),
            Event = profile.Event?.Trim() ?? "",
            Round = round,
            Note = profile.Note?.Trim() ?? "",
        };
    }
}
