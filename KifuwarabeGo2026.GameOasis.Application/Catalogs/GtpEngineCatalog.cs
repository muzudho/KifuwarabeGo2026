namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;

/// <summary>Persistent GTP engine catalog use cases, independent of its physical storage.</summary>
public sealed class GtpEngineCatalog
{
    private readonly ICatalogDocumentStore _store;

    private GtpEngineCatalog(ICatalogDocumentStore store, string listPath, IReadOnlyList<GtpEngineProfile> profiles,
        bool requiresSave = false, bool duplicateIdsRepaired = false)
    {
        _store = store;
        ListPath = listPath;
        Profiles = profiles;
        RequiresSave = requiresSave;
        DuplicateIdsRepaired = duplicateIdsRepaired;
    }

    public string ListPath { get; }
    public IReadOnlyList<GtpEngineProfile> Profiles { get; }
    public bool RequiresSave { get; }
    public bool DuplicateIdsRepaired { get; }

    public static GtpEngineCatalog LoadFromDefaultLocation(
        ICatalogDocumentStore store,
        ICatalogPathProvider paths,
        IEnumerable<GtpEngineProfile> releaseDefaultProfiles,
        string releaseDefaultDirectory)
    {
        var listPath = paths.GtpEngineListPath;
        if (!store.Exists(listPath))
        {
            var defaults = releaseDefaultProfiles
                .Select(profile => GtpEngineProfilePolicy.Normalize(profile, releaseDefaultDirectory)).ToList();
            new GtpEngineCatalog(store, listPath, defaults).Save(defaults);
        }

        var catalog = Load(store, listPath);
        if (catalog.RequiresSave)
        {
            var duplicateIdsRepaired = catalog.DuplicateIdsRepaired;
            catalog.Save(catalog.Profiles);
            catalog = Load(store, listPath);
            if (duplicateIdsRepaired)
                catalog = new GtpEngineCatalog(store, catalog.ListPath, catalog.Profiles,
                    catalog.RequiresSave, duplicateIdsRepaired: true);
        }

        var developmentListPath = paths.FindDevelopmentGtpEngineListPath();
        if (developmentListPath is null) return catalog;
        var profiles = catalog.Profiles.ToList();
        var changed = false;
        foreach (var developmentDefault in Load(store, developmentListPath).Profiles.Take(1))
        {
            if (profiles.Any(profile => string.Equals(
                    profile.ExecutablePath, developmentDefault.ExecutablePath, StringComparison.OrdinalIgnoreCase))) continue;
            var imported = developmentDefault.Clone();
            if (profiles.Any(profile => string.Equals(profile.Id, imported.Id, StringComparison.Ordinal)))
                imported.Id = CreateUniqueId(profiles.Select(profile => profile.Id));
            profiles.Insert(0, imported);
            changed = true;
        }
        if (!changed) return catalog;
        catalog.Save(profiles);
        return Load(store, listPath);
    }

    public static GtpEngineCatalog Load(ICatalogDocumentStore store, string listPath)
    {
        if (!store.Exists(listPath)) return new(store, listPath, []);
        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var result = GtpEngineCatalogDocumentCodec.Deserialize(store.ReadAllText(listPath), listDirectory);
        return new(store, listPath, result.Profiles, result.RequiresSave, result.DuplicateIdsRepaired);
    }

    public void Save(IEnumerable<GtpEngineProfile> profiles)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        _store.WriteAllText(ListPath, GtpEngineCatalogDocumentCodec.Serialize(profiles, listDirectory));
    }

    private static string CreateUniqueId(IEnumerable<string> usedIds)
    {
        var used = new HashSet<string>(usedIds, StringComparer.Ordinal);
        string id;
        do id = Guid.NewGuid().ToString("N"); while (!used.Add(id));
        return id;
    }
}
