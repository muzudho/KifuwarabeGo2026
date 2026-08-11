namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>用途・接続先ごとの TargetProfile を保存し、旧 Player/Engine 設定を一度だけ移行する。</summary>
public sealed class TargetCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private TargetCatalog(string listPath, IReadOnlyList<TargetProfile> profiles, IReadOnlyList<PlayerProfile> players, bool playerProfilesChanged)
    {
        ListPath = listPath;
        Profiles = profiles;
        PlayerProfiles = players;
        PlayerProfilesChanged = playerProfilesChanged;
    }

    public string ListPath { get; }
    public IReadOnlyList<TargetProfile> Profiles { get; }
    public IReadOnlyList<PlayerProfile> PlayerProfiles { get; }
    public bool PlayerProfilesChanged { get; }

    public static TargetCatalog LoadFromDefaultLocation(
        IEnumerable<PlayerProfile> players,
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
            var originalIds = player.TargetProfileIds.ToArray();
            player.TargetProfileIds = player.TargetProfileIds
                .Where(id => targetProfiles.Any(target => string.Equals(target.Id, id, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            EnsureLocalMatchTarget(player, targetProfiles);
            if (player.Kind == PlayerProfileKind.Computer && enginesById.TryGetValue(player.EngineProfileId, out var engine))
                EnsureCgosTarget(player, targetProfiles, engine, defaultCgosConnectionId);

            playerProfilesChanged |= !originalIds.SequenceEqual(player.TargetProfileIds, StringComparer.Ordinal);
        }

        var normalizedTargets = targetProfiles.Select(Normalize).ToList();
        var targetsChanged = !TargetListsAreEqual(loaded.Profiles, normalizedTargets);
        var result = new TargetCatalog(listPath, normalizedTargets, playerProfiles, playerProfilesChanged);
        if (targetsChanged)
            result.Save(normalizedTargets);
        return result;
    }

    public static TargetCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
            return new TargetCatalog(listPath, Array.Empty<TargetProfile>(), Array.Empty<PlayerProfile>(), false);

        var profiles = JsonSerializer.Deserialize<TargetProfileList>(File.ReadAllText(listPath), JsonOptions)?.Targets ?? [];
        return new TargetCatalog(listPath, profiles.Select(Normalize).ToList(), Array.Empty<PlayerProfile>(), false);
    }

    public void Save(IEnumerable<TargetProfile> profiles)
    {
        var list = profiles.Select(Normalize).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory);
        File.WriteAllText(ListPath, JsonSerializer.Serialize(new TargetProfileList { Targets = list }, JsonOptions));
    }

    private static void EnsureLocalMatchTarget(PlayerProfile player, ICollection<TargetProfile> targets)
    {
        if (GetPlayerTargets(player, targets).Any(target => string.IsNullOrEmpty(target.ConnectionProfileId))) return;

        var target = new TargetProfile
        {
            DisplayName = "LocalMatch",
            LoginName = player.Identifier,
        };
        targets.Add(target);
        player.TargetProfileIds.Add(target.Id);
    }

    private static void EnsureCgosTarget(PlayerProfile player, ICollection<TargetProfile> targets, GtpEngineProfile engine, string connectionProfileId)
    {
        // ConnectionProfileId がない状態で作ると LocalMatch Target と区別できない。
        if (string.IsNullOrWhiteSpace(connectionProfileId)) return;
        if (GetPlayerTargets(player, targets).Any(target => !string.IsNullOrEmpty(target.ConnectionProfileId))) return;

        var target = new TargetProfile
        {
            DisplayName = "CGOS",
            ConnectionProfileId = connectionProfileId,
            LoginName = engine.DefaultCgosLoginName,
            LoginPass = engine.DefaultCgosPlainTextPassword,
        };
        targets.Add(target);
        player.TargetProfileIds.Add(target.Id);
    }

    private static IEnumerable<TargetProfile> GetPlayerTargets(PlayerProfile player, IEnumerable<TargetProfile> targets) =>
        targets.Where(target => player.TargetProfileIds.Contains(target.Id, StringComparer.Ordinal));

    private static TargetProfile Normalize(TargetProfile profile)
    {
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "New Target" : normalized.DisplayName.Trim();
        normalized.ConnectionProfileId ??= "";
        normalized.LoginName ??= "";
        normalized.LoginPass ??= "";
        return normalized;
    }

    private static bool TargetListsAreEqual(IReadOnlyList<TargetProfile> left, IReadOnlyList<TargetProfile> right) =>
        left.Count == right.Count && left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.DisplayName == pair.Second.DisplayName &&
            pair.First.ConnectionProfileId == pair.Second.ConnectionProfileId &&
            pair.First.LoginName == pair.Second.LoginName &&
            pair.First.LoginPass == pair.Second.LoginPass);

    private sealed class TargetProfileList
    {
        public List<TargetProfile> Targets { get; set; } = new();
    }
}
