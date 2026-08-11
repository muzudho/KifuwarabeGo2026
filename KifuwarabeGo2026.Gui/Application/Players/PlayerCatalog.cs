namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 人間・コンピューターをまとめた PlayerProfile の永続カタログ。
/// エンジン設定の追加時には対応するコンピューター Player を補うが、既存 Player は自動削除しない。
/// </summary>
public sealed class PlayerCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private PlayerCatalog(string listPath, IReadOnlyList<PlayerProfile> profiles)
    {
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }

    public IReadOnlyList<PlayerProfile> Profiles { get; }

    public static PlayerCatalog LoadFromDefaultLocation(IEnumerable<GtpEngineProfile> engines)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KifuwarabeGo2026",
            "Players");
        var listPath = Path.Combine(directory, "player-list.json");
        var catalog = Load(listPath);
        var profiles = catalog.Profiles.Select(profile => profile.Clone()).ToList();

        if (profiles.Count == 0)
        {
            // 旧 UI の黒白の初期人間名を失わない、最初の二つの互換 Player。
            profiles.Add(new PlayerProfile { DisplayName = "Black Player", Identifier = "Black Player" });
            profiles.Add(new PlayerProfile { DisplayName = "White Player", Identifier = "White Player" });
        }

        foreach (var engine in engines)
        {
            if (profiles.Any(profile =>
                    profile.Kind == PlayerProfileKind.Computer &&
                    string.Equals(profile.EngineProfileId, engine.Id, StringComparison.Ordinal)))
            {
                continue;
            }

            // 初期導入版では EngineProfileId が起動ごとに変わってしまった。
            // 自動生成 Player は表示名と Identifier がエンジン表示名そのものなので、
            // その形だけを旧 ID から現行 ID へ移し、同じエンジンの重複を統合する。
            var legacyMatches = profiles
                .Where(profile =>
                    profile.Kind == PlayerProfileKind.Computer &&
                    string.Equals(profile.DisplayName, engine.DisplayName, StringComparison.Ordinal) &&
                    string.Equals(profile.Identifier, engine.DisplayName, StringComparison.Ordinal))
                .ToList();
            if (legacyMatches.Count > 0)
            {
                legacyMatches[0].EngineProfileId = engine.Id;
                foreach (var duplicate in legacyMatches.Skip(1))
                    profiles.Remove(duplicate);
                continue;
            }

            profiles.Add(new PlayerProfile
            {
                DisplayName = engine.DisplayName,
                Identifier = engine.DisplayName,
                Kind = PlayerProfileKind.Computer,
                EngineProfileId = engine.Id,
            });
        }

        var changed = !ProfileListsAreEqual(catalog.Profiles, profiles);
        var result = new PlayerCatalog(listPath, profiles);
        if (changed)
            result.Save(profiles);
        return result;
    }

    public static PlayerCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
            return new PlayerCatalog(listPath, Array.Empty<PlayerProfile>());

        var profiles = JsonSerializer.Deserialize<PlayerProfileList>(File.ReadAllText(listPath), JsonOptions)?.Players ?? [];
        return new PlayerCatalog(listPath, profiles.Select(Normalize).ToList());
    }

    public void Save(IEnumerable<PlayerProfile> profiles)
    {
        var list = profiles.Select(Normalize).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory);
        File.WriteAllText(ListPath, JsonSerializer.Serialize(new PlayerProfileList { Players = list }, JsonOptions));
    }

    private static PlayerProfile Normalize(PlayerProfile profile)
    {
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "New Player" : normalized.DisplayName.Trim();
        normalized.Identifier ??= "";
        normalized.EngineProfileId ??= "";
        if (normalized.Kind == PlayerProfileKind.Human)
            normalized.EngineProfileId = "";
        return normalized;
    }

    private static bool ProfileListsAreEqual(IReadOnlyList<PlayerProfile> left, IReadOnlyList<PlayerProfile> right) =>
        left.Count == right.Count &&
        left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.DisplayName == pair.Second.DisplayName &&
            pair.First.Identifier == pair.Second.Identifier &&
            pair.First.Kind == pair.Second.Kind &&
            pair.First.EngineProfileId == pair.Second.EngineProfileId);

    private sealed class PlayerProfileList
    {
        public List<PlayerProfile> Players { get; set; } = new();
    }
}
