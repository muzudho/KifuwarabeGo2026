namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;
using System.Text.Json;

/// <summary>Persistent client identity catalog use cases and legacy entry migration.</summary>
public sealed class ClientIdentityCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
    private readonly ICatalogDocumentStore _store;

    private ClientIdentityCatalog(ICatalogDocumentStore store, string listPath,
        IReadOnlyList<ClientIdentityProfile> profiles, IReadOnlyList<EntryProfile> entries, bool entriesChanged)
    {
        _store = store;
        ListPath = listPath;
        Profiles = profiles;
        EntryProfiles = entries;
        EntryProfilesChanged = entriesChanged;
    }

    public string ListPath { get; }
    public IReadOnlyList<ClientIdentityProfile> Profiles { get; }
    public IReadOnlyList<EntryProfile> EntryProfiles { get; }
    public bool EntryProfilesChanged { get; }

    public static ClientIdentityCatalog LoadFromDefaultLocation(
        ICatalogDocumentStore store, ICatalogPathProvider paths,
        IEnumerable<EntryProfile> entries, IEnumerable<GtpEngineProfile> engines,
        IReadOnlyDictionary<string, string> connectionNamesById)
    {
        var loaded = Load(store, paths.ClientIdentityListPath);
        var targets = loaded.Profiles.Select(profile => profile.Clone()).ToList();
        var entryProfiles = entries.Select(profile => profile.Clone()).ToList();
        var enginesById = engines.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var entriesChanged = false;
        foreach (var entry in entryProfiles)
        {
            var originalIds = entry.ClientIdentityProfileIds.ToArray();
            entry.ClientIdentityProfileIds = entry.ClientIdentityProfileIds
                .Where(id => targets.Any(target => string.Equals(target.Id, id, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal).ToList();
            EnsureClientIdentity(entry, targets, enginesById.GetValueOrDefault(entry.EngineProfileId));
            entriesChanged |= !originalIds.SequenceEqual(entry.ClientIdentityProfileIds, StringComparer.Ordinal);
        }

        var normalized = targets.Select(target => ClientIdentityProfilePolicy.Normalize(target, connectionNamesById)).ToList();
        var result = new ClientIdentityCatalog(store, loaded.ListPath, normalized, entryProfiles, entriesChanged);
        if (!ClientIdentityProfilePolicy.ListsAreEqual(loaded.Profiles, normalized)) result.Save(normalized);
        return result;
    }

    public static ClientIdentityCatalog Load(ICatalogDocumentStore store, string listPath)
    {
        if (!store.Exists(listPath)) return new(store, listPath, [], [], false);
        var profiles = JsonSerializer.Deserialize<ClientIdentityProfileList>(store.ReadAllText(listPath), JsonOptions)?.Targets ?? [];
        return new(store, listPath, profiles.Select(profile => ClientIdentityProfilePolicy.Normalize(profile)).ToList(), [], false);
    }

    public void Save(IEnumerable<ClientIdentityProfile> profiles)
    {
        var list = profiles.Select(profile => ClientIdentityProfilePolicy.Normalize(profile)).ToList();
        _store.WriteAllText(ListPath, JsonSerializer.Serialize(new ClientIdentityProfileList { Targets = list }, JsonOptions));
    }

    private static void EnsureClientIdentity(EntryProfile entry, ICollection<ClientIdentityProfile> targets, GtpEngineProfile? engine)
    {
        if (targets.Any(target => entry.ClientIdentityProfileIds.Contains(target.Id, StringComparer.Ordinal))) return;
        var target = new ClientIdentityProfile
        {
            DisplayName = "Client Identity",
            LoginName = engine?.DefaultCgosLoginName ?? new(entry.Identifier.Where(character => !char.IsWhiteSpace(character)).ToArray()),
            LoginPass = engine?.DefaultCgosPlainTextPassword ?? "",
        };
        targets.Add(target);
        entry.ClientIdentityProfileIds.Add(target.Id);
    }

    private sealed class ClientIdentityProfileList { public List<ClientIdentityProfile> Targets { get; set; } = []; }
}
