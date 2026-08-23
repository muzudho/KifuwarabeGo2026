namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.GameOasis.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>用途・接続先ごとの ClientIdentityProfile を保存し、旧 Player/Engine 設定を一度だけ移行する。</summary>
public sealed class ClientIdentityCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private ClientIdentityCatalog(string listPath, IReadOnlyList<ClientIdentityProfile> profiles, IReadOnlyList<EntryProfile> players, bool playerProfilesChanged)
    {
        ListPath = listPath;
        Profiles = profiles;
        EntryProfiles = players;
        EntryProfilesChanged = playerProfilesChanged;
    }

    public string ListPath { get; }
    public IReadOnlyList<ClientIdentityProfile> Profiles { get; }
    public IReadOnlyList<EntryProfile> EntryProfiles { get; }
    public bool EntryProfilesChanged { get; }

    public static ClientIdentityCatalog LoadFromDefaultLocation(
        IEnumerable<EntryProfile> players,
        IEnumerable<GtpEngineProfile> engines,
        IEnumerable<CgosConnectionProfile> cgosConnections)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KifuwarabeGo2026", "Targets");
        var listPath = Path.Combine(directory, "target-list.json");
        var loaded = Load(listPath);
        var targetProfiles = loaded.Profiles.Select(profile => profile.Clone()).ToList();
        var playerProfiles = players.Select(profile => profile.Clone()).ToList();
        var enginesById = engines.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var connectionNamesById = cgosConnections.ToDictionary(profile => profile.Id, profile => profile.DisplayName, StringComparer.Ordinal);
        var playerProfilesChanged = false;

        foreach (var player in playerProfiles)
        {
            var originalIds = player.ClientIdentityProfileIds.ToArray();
            player.ClientIdentityProfileIds = player.ClientIdentityProfileIds
                .Where(id => targetProfiles.Any(target => string.Equals(target.Id, id, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            EnsureClientIdentity(player, targetProfiles, enginesById.GetValueOrDefault(player.EngineProfileId));

            playerProfilesChanged |= !originalIds.SequenceEqual(player.ClientIdentityProfileIds, StringComparer.Ordinal);
        }

        var normalizedTargets = targetProfiles.Select(target => ClientIdentityProfilePolicy.Normalize(target, connectionNamesById)).ToList();
        var targetsChanged = !ClientIdentityProfilePolicy.ListsAreEqual(loaded.Profiles, normalizedTargets);
        var result = new ClientIdentityCatalog(listPath, normalizedTargets, playerProfiles, playerProfilesChanged);
        if (targetsChanged)
            result.Save(normalizedTargets);
        return result;
    }

    public static ClientIdentityCatalog Load(string listPath)
    {
        if (!CatalogDocumentStorage.Default.Exists(listPath))
            return new ClientIdentityCatalog(listPath, Array.Empty<ClientIdentityProfile>(), Array.Empty<EntryProfile>(), false);

        var profiles = JsonSerializer.Deserialize<ClientIdentityProfileList>(CatalogDocumentStorage.Default.ReadAllText(listPath), JsonOptions)?.Targets ?? [];
        return new ClientIdentityCatalog(listPath, profiles.Select(profile => ClientIdentityProfilePolicy.Normalize(profile)).ToList(), Array.Empty<EntryProfile>(), false);
    }

    public void Save(IEnumerable<ClientIdentityProfile> profiles)
    {
        var list = profiles.Select(profile => ClientIdentityProfilePolicy.Normalize(profile)).ToList();
        CatalogDocumentStorage.Default.WriteAllText(ListPath, JsonSerializer.Serialize(new ClientIdentityProfileList { Targets = list }, JsonOptions));
    }

    private static void EnsureClientIdentity(EntryProfile player, ICollection<ClientIdentityProfile> targets, GtpEngineProfile? engine)
    {
        if (GetPlayerClientIdentities(player, targets).Any()) return;

        var target = new ClientIdentityProfile
        {
            DisplayName = "Client Identity",
            LoginName = engine?.DefaultCgosLoginName ?? new string(player.Identifier.Where(character => !char.IsWhiteSpace(character)).ToArray()),
            LoginPass = engine?.DefaultCgosPlainTextPassword ?? "",
        };
        targets.Add(target);
        player.ClientIdentityProfileIds.Add(target.Id);
    }

    private static IEnumerable<ClientIdentityProfile> GetPlayerClientIdentities(EntryProfile player, IEnumerable<ClientIdentityProfile> targets) =>
        targets.Where(target => player.ClientIdentityProfileIds.Contains(target.Id, StringComparer.Ordinal));

    private sealed class ClientIdentityProfileList
    {
        public List<ClientIdentityProfile> Targets { get; set; } = new();
    }
}
