namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Persistent entry catalog use cases, independent of physical storage and GUI.</summary>
public sealed class EntryCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private readonly ICatalogDocumentStore _store;

    private EntryCatalog(ICatalogDocumentStore store, string listPath, IReadOnlyList<EntryProfile> profiles)
    {
        _store = store;
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }
    public IReadOnlyList<EntryProfile> Profiles { get; }

    public static EntryCatalog LoadFromDefaultLocation(
        ICatalogDocumentStore store, ICatalogPathProvider paths, IEnumerable<GtpEngineProfile> engines)
    {
        var catalog = Load(store, paths.EntryListPath);
        var profiles = catalog.Profiles.Select(profile => profile.Clone()).ToList();
        if (profiles.Count == 0)
        {
            profiles.Add(new EntryProfile { DisplayName = "Black Player", Identifier = "Black Player" });
            profiles.Add(new EntryProfile { DisplayName = "White Player", Identifier = "White Player" });
        }

        foreach (var engine in engines)
        {
            if (profiles.Any(profile => profile.Kind == EntryProfileKind.Computer &&
                string.Equals(profile.EngineProfileId, engine.Id, StringComparison.Ordinal))) continue;
            var legacyMatches = profiles.Where(profile => profile.Kind == EntryProfileKind.Computer &&
                string.Equals(profile.DisplayName, engine.DisplayName, StringComparison.Ordinal) &&
                string.Equals(profile.Identifier, engine.DisplayName, StringComparison.Ordinal)).ToList();
            if (legacyMatches.Count > 0)
            {
                legacyMatches[0].EngineProfileId = engine.Id;
                foreach (var duplicate in legacyMatches.Skip(1)) profiles.Remove(duplicate);
                continue;
            }
            profiles.Add(new EntryProfile
            {
                DisplayName = engine.DisplayName,
                Identifier = engine.DisplayName,
                Kind = EntryProfileKind.Computer,
                EngineProfileId = engine.Id,
            });
        }

        var result = new EntryCatalog(store, catalog.ListPath, profiles);
        if (!EntryProfilePolicy.ListsAreEqual(catalog.Profiles, profiles)) result.Save(profiles);
        return result;
    }

    public static EntryCatalog Load(ICatalogDocumentStore store, string listPath)
    {
        if (!store.Exists(listPath)) return new(store, listPath, []);
        var profiles = JsonSerializer.Deserialize<EntryProfileList>(store.ReadAllText(listPath), JsonOptions)?.Players ?? [];
        return new(store, listPath, profiles.Select(EntryProfilePolicy.Normalize).ToList());
    }

    public void Save(IEnumerable<EntryProfile> profiles)
    {
        var list = profiles.Select(EntryProfilePolicy.Normalize).ToList();
        _store.WriteAllText(ListPath, JsonSerializer.Serialize(new EntryProfileList { Players = list }, JsonOptions));
    }

    private sealed class EntryProfileList { public List<EntryProfile> Players { get; set; } = []; }
}
