namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
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
        var defaultCgosConnectionId = cgosConnections.FirstOrDefault()?.Id ?? "";
        var playerProfilesChanged = false;

        foreach (var player in playerProfiles)
        {
            var originalIds = player.ClientIdentityProfileIds.ToArray();
            player.ClientIdentityProfileIds = player.ClientIdentityProfileIds
                .Where(id => targetProfiles.Any(target => string.Equals(target.Id, id, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            EnsureLocalMatchClientIdentity(player, targetProfiles);
            if (player.Kind == EntryProfileKind.Computer && enginesById.TryGetValue(player.EngineProfileId, out var engine))
                EnsureCgosClientIdentity(player, targetProfiles, engine, defaultCgosConnectionId);

            playerProfilesChanged |= !originalIds.SequenceEqual(player.ClientIdentityProfileIds, StringComparer.Ordinal);
        }

        var normalizedTargets = targetProfiles.Select(Normalize).ToList();
        var targetsChanged = !ClientIdentityListsAreEqual(loaded.Profiles, normalizedTargets);
        var result = new ClientIdentityCatalog(listPath, normalizedTargets, playerProfiles, playerProfilesChanged);
        if (targetsChanged)
            result.Save(normalizedTargets);
        return result;
    }

    public static ClientIdentityCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
            return new ClientIdentityCatalog(listPath, Array.Empty<ClientIdentityProfile>(), Array.Empty<EntryProfile>(), false);

        var profiles = JsonSerializer.Deserialize<ClientIdentityProfileList>(File.ReadAllText(listPath), JsonOptions)?.Targets ?? [];
        return new ClientIdentityCatalog(listPath, profiles.Select(Normalize).ToList(), Array.Empty<EntryProfile>(), false);
    }

    public void Save(IEnumerable<ClientIdentityProfile> profiles)
    {
        var list = profiles.Select(Normalize).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory);
        File.WriteAllText(ListPath, JsonSerializer.Serialize(new ClientIdentityProfileList { Targets = list }, JsonOptions));
    }

    private static void EnsureLocalMatchClientIdentity(EntryProfile player, ICollection<ClientIdentityProfile> targets)
    {
        if (GetPlayerClientIdentities(player, targets).Any(target => string.IsNullOrEmpty(target.ConnectionProfileId))) return;

        var target = new ClientIdentityProfile
        {
            DisplayName = "LocalMatch",
            LoginName = player.Identifier,
        };
        targets.Add(target);
        player.ClientIdentityProfileIds.Add(target.Id);
    }

    private static void EnsureCgosClientIdentity(EntryProfile player, ICollection<ClientIdentityProfile> targets, GtpEngineProfile engine, string connectionProfileId)
    {
        // ConnectionProfileId がない状態で作ると LocalMatch Target と区別できない。
        if (string.IsNullOrWhiteSpace(connectionProfileId)) return;
        if (GetPlayerClientIdentities(player, targets).Any(target => !string.IsNullOrEmpty(target.ConnectionProfileId))) return;

        var target = new ClientIdentityProfile
        {
            DisplayName = "OnlineMatch (CGOS)",
            ConnectionProfileId = connectionProfileId,
            LoginName = engine.DefaultCgosLoginName,
            LoginPass = engine.DefaultCgosPlainTextPassword,
        };
        targets.Add(target);
        player.ClientIdentityProfileIds.Add(target.Id);
    }

    private static IEnumerable<ClientIdentityProfile> GetPlayerClientIdentities(EntryProfile player, IEnumerable<ClientIdentityProfile> targets) =>
        targets.Where(target => player.ClientIdentityProfileIds.Contains(target.Id, StringComparer.Ordinal));

    private static ClientIdentityProfile Normalize(ClientIdentityProfile profile)
    {
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "New Client Identity" : normalized.DisplayName.Trim();
        // 旧 UI では用途分類とプロトコル名を同一視して "CGOS" と表示していた。
        // Target の分類は OnlineMatch、詳細な実装は CGOS として表示する。
        if (string.Equals(normalized.DisplayName, "CGOS", StringComparison.Ordinal))
            normalized.DisplayName = "OnlineMatch (CGOS)";
        normalized.ConnectionProfileId ??= "";
        normalized.LoginName ??= "";
        normalized.LoginPass ??= "";
        return normalized;
    }

    private static bool ClientIdentityListsAreEqual(IReadOnlyList<ClientIdentityProfile> left, IReadOnlyList<ClientIdentityProfile> right) =>
        left.Count == right.Count && left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.DisplayName == pair.Second.DisplayName &&
            pair.First.ConnectionProfileId == pair.Second.ConnectionProfileId &&
            pair.First.LoginName == pair.Second.LoginName &&
            pair.First.LoginPass == pair.Second.LoginPass);

    private sealed class ClientIdentityProfileList
    {
        public List<ClientIdentityProfile> Targets { get; set; } = new();
    }
}
