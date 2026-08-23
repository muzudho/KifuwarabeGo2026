namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using System.Collections.Generic;

/// <summary>Adapts the existing combined GUI settings document to the Application catalog boundary.</summary>
public static class CgosConnectionCatalogComposition
{
    public static CgosConnectionCatalog LoadFromDefaultLocation() =>
        CgosConnectionCatalog.Load(ApplicationSettingsCgosConnectionStore.Instance);

    private sealed class ApplicationSettingsCgosConnectionStore : ICgosConnectionProfileStore
    {
        public static ApplicationSettingsCgosConnectionStore Instance { get; } = new();
        public string ListPath => ApplicationSettings.FilePath;
        public IReadOnlyList<CgosConnectionProfile> Load() => ApplicationSettings.Current.CgosConnections;
        public void Save(IEnumerable<CgosConnectionProfile> profiles) => ApplicationSettings.SaveCgosConnections(profiles);
    }
}
