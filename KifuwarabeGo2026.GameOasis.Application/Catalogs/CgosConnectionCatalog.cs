namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>Persistent CGOS connection catalog use cases, independent of its physical document.</summary>
public sealed class CgosConnectionCatalog
{
    private readonly ICgosConnectionProfileStore _store;

    private CgosConnectionCatalog(ICgosConnectionProfileStore store, IReadOnlyList<CgosConnectionProfile> profiles)
    {
        _store = store;
        Profiles = profiles;
    }

    public string ListPath => _store.ListPath;
    public IReadOnlyList<CgosConnectionProfile> Profiles { get; }

    public static CgosConnectionCatalog Load(ICgosConnectionProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var source = store.Load().Where(profile => !string.IsNullOrWhiteSpace(profile.Host)).ToList();
        var normalized = source.Select(CgosConnectionProfilePolicy.Normalize).ToList();
        if (!CgosConnectionProfilePolicy.ListsAreEqual(source, normalized)) store.Save(normalized);
        return new(store, normalized);
    }

    public void Save(IEnumerable<CgosConnectionProfile> profiles) =>
        _store.Save(profiles.Select(CgosConnectionProfilePolicy.Normalize).ToList());
}
