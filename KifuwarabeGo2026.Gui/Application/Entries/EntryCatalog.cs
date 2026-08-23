namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GameOasis.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 人間・コンピューターをまとめた EntryProfile の永続カタログ。
/// エンジン設定の追加時には対応するコンピューター Player を補うが、既存 Player は自動削除しない。
/// </summary>
public sealed class EntryCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private EntryCatalog(string listPath, IReadOnlyList<EntryProfile> profiles)
    {
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }

    public IReadOnlyList<EntryProfile> Profiles { get; }

    public static EntryCatalog LoadFromDefaultLocation(IEnumerable<GtpEngineProfile> engines)
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
            profiles.Add(new EntryProfile { DisplayName = "Black Player", Identifier = "Black Player" });
            profiles.Add(new EntryProfile { DisplayName = "White Player", Identifier = "White Player" });
        }

        foreach (var engine in engines)
        {
            if (profiles.Any(profile =>
                    profile.Kind == EntryProfileKind.Computer &&
                    string.Equals(profile.EngineProfileId, engine.Id, StringComparison.Ordinal)))
            {
                continue;
            }

            // 初期導入版では EngineProfileId が起動ごとに変わってしまった。
            // 自動生成 Player は表示名と Identifier がエンジン表示名そのものなので、
            // その形だけを旧 ID から現行 ID へ移し、同じエンジンの重複を統合する。
            var legacyMatches = profiles
                .Where(profile =>
                    profile.Kind == EntryProfileKind.Computer &&
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

            profiles.Add(new EntryProfile
            {
                DisplayName = engine.DisplayName,
                Identifier = engine.DisplayName,
                Kind = EntryProfileKind.Computer,
                EngineProfileId = engine.Id,
            });
        }

        var changed = !EntryProfilePolicy.ListsAreEqual(catalog.Profiles, profiles);
        var result = new EntryCatalog(listPath, profiles);
        if (changed)
            result.Save(profiles);
        return result;
    }

    public static EntryCatalog Load(string listPath)
    {
        if (!CatalogDocumentStorage.Default.Exists(listPath))
            return new EntryCatalog(listPath, Array.Empty<EntryProfile>());

        var profiles = JsonSerializer.Deserialize<EntryProfileList>(CatalogDocumentStorage.Default.ReadAllText(listPath), JsonOptions)?.Players ?? [];
        return new EntryCatalog(listPath, profiles.Select(EntryProfilePolicy.Normalize).ToList());
    }

    public void Save(IEnumerable<EntryProfile> profiles)
    {
        var list = profiles.Select(EntryProfilePolicy.Normalize).ToList();
        CatalogDocumentStorage.Default.WriteAllText(ListPath, JsonSerializer.Serialize(new EntryProfileList { Players = list }, JsonOptions));
    }

    private sealed class EntryProfileList
    {
        public List<EntryProfile> Players { get; set; } = new();
    }
}
