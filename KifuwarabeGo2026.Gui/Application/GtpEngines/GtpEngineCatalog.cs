namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.GameOasis.Storage;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// GTPプロトコルに対応した思考エンジンのカタログ
/// </summary>
public sealed class GtpEngineCatalog
{
    private GtpEngineCatalog(string listPath, IReadOnlyList<GtpEngineProfile> profiles, bool requiresSave = false,
        bool duplicateIdsRepaired = false)
    {
        ListPath = listPath;
        Profiles = profiles;
        RequiresSave = requiresSave;
        DuplicateIdsRepaired = duplicateIdsRepaired;
    }

    public string ListPath { get; }

    public IReadOnlyList<GtpEngineProfile> Profiles { get; }

    /// <summary>旧形式の設定を読み込んだため、次回保存時に永続 ID を書き戻す必要があるか。</summary>
    public bool RequiresSave { get; }

    public bool DuplicateIdsRepaired { get; }

    public static GtpEngineCatalog LoadFromDefaultLocation()
    {
        var listPath = CatalogDocumentStorage.Paths.GtpEngineListPath;
        if (!CatalogDocumentStorage.Default.Exists(listPath))
        {
            var defaultDirectory =
                Path.GetDirectoryName(ReleaseDefaultSettings.FilePath) ??
                AppContext.BaseDirectory;
            var defaultProfiles = ReleaseDefaultSettings.Current.EngineSettings.GtpEngines
                .Select(profile => GtpEngineProfilePolicy.Normalize(profile, defaultDirectory))
                .ToList();
            new GtpEngineCatalog(listPath, defaultProfiles).Save(defaultProfiles);
        }

        var catalog = Load(listPath);
        if (catalog.RequiresSave)
        {
            var duplicateIdsRepaired = catalog.DuplicateIdsRepaired;
            catalog.Save(catalog.Profiles);
            catalog = Load(listPath);
            if (duplicateIdsRepaired)
                catalog = new GtpEngineCatalog(catalog.ListPath, catalog.Profiles,
                    catalog.RequiresSave, duplicateIdsRepaired: true);
        }
        var developmentListPath = CatalogDocumentStorage.Paths.FindDevelopmentGtpEngineListPath();
        if (developmentListPath is null) return catalog;

        var profiles = catalog.Profiles.ToList();
        var changed = false;
        foreach (var defaultProfile in Load(developmentListPath).Profiles.Take(1))
        {
            if (profiles.Any(profile =>
                    string.Equals(profile.ExecutablePath, defaultProfile.ExecutablePath, StringComparison.OrdinalIgnoreCase)))
                continue;

            var importedProfile = defaultProfile.Clone();
            if (profiles.Any(profile => string.Equals(profile.Id, importedProfile.Id, StringComparison.Ordinal)))
                importedProfile.Id = CreateUniqueId(profiles);

            profiles.Insert(0, importedProfile);
            changed = true;
        }

        if (!changed) return catalog;
        catalog.Save(profiles);
        return Load(listPath);
    }

    public static GtpEngineCatalog Load(string listPath)
    {
        if (!CatalogDocumentStorage.Default.Exists(listPath))
        {
            return new GtpEngineCatalog(listPath, Array.Empty<GtpEngineProfile>());
        }

        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var result = GtpEngineCatalogDocumentCodec.Deserialize(
            CatalogDocumentStorage.Default.ReadAllText(listPath), listDirectory);
        return new GtpEngineCatalog(listPath, result.Profiles, result.RequiresSave, result.DuplicateIdsRepaired);
    }

    public void Save(IEnumerable<GtpEngineProfile> profiles)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        CatalogDocumentStorage.Default.WriteAllText(
            ListPath, GtpEngineCatalogDocumentCodec.Serialize(profiles, listDirectory));
    }

    private static string CreateUniqueId(IEnumerable<GtpEngineProfile> profiles) =>
        CreateUniqueId(profiles.Select(profile => profile.Id));

    private static string CreateUniqueId(IEnumerable<string> usedIds)
    {
        var usedIdSet = usedIds as ISet<string> ?? new HashSet<string>(usedIds, StringComparer.Ordinal);
        string id;
        do
        {
            id = Guid.NewGuid().ToString("N");
        }
        while (!usedIdSet.Add(id));

        return id;
    }

}
