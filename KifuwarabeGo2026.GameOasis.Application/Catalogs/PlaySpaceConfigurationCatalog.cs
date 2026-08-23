namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>Game-agnostic use cases for persistent named play-space configurations.</summary>
public sealed class PlaySpaceConfigurationCatalog
{
    private readonly IPlaySpaceConfigurationProfileStore _store;

    private PlaySpaceConfigurationCatalog(
        IPlaySpaceConfigurationProfileStore store, IReadOnlyList<PlaySpaceConfigurationProfile> profiles)
    {
        _store = store;
        Profiles = profiles;
    }

    public string ListPath => _store.ListPath;
    public IReadOnlyList<PlaySpaceConfigurationProfile> Profiles { get; }

    public static PlaySpaceConfigurationCatalog Load(IPlaySpaceConfigurationProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var source = store.Load();
        var profiles = source.Select(PlaySpaceConfigurationProfilePolicy.Normalize).ToList();
        if (!source.SequenceEqual(profiles)) store.Save(profiles);
        return new(store, profiles);
    }

    public PlaySpaceConfigurationProfile Save(PlaySpaceConfigurationProfile profile)
    {
        var normalized = PlaySpaceConfigurationProfilePolicy.Normalize(profile);
        var profiles = LoadCurrent();
        var index = profiles.FindIndex(candidate => candidate.Id.Equals(normalized.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) profiles[index] = normalized; else profiles.Add(normalized);
        _store.Save(profiles);
        return normalized;
    }

    public void Delete(string id) =>
        _store.Save(LoadCurrent().Where(profile => !profile.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList());

    public void SaveOrder(IEnumerable<PlaySpaceConfigurationProfile> profiles) =>
        _store.Save(profiles.Select(PlaySpaceConfigurationProfilePolicy.Normalize).ToList());

    public PlaySpaceConfigurationProfile CreateNew(PlaySpaceConfigurationProfile source, string displayName) =>
        Save(source with { Id = Guid.NewGuid().ToString("N"), DisplayName = displayName });

    public PlaySpaceConfigurationProfile Duplicate(PlaySpaceConfigurationProfile source) =>
        Save(source with { Id = Guid.NewGuid().ToString("N"), DisplayName = $"{source.DisplayName} Copy" });

    private List<PlaySpaceConfigurationProfile> LoadCurrent() =>
        _store.Load().Select(PlaySpaceConfigurationProfilePolicy.Normalize).ToList();
}
