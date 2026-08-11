namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// GTPプロトコルに対応した思考エンジンのカタログ
/// </summary>
public sealed class GtpEngineCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private GtpEngineCatalog(string listPath, IReadOnlyList<GtpEngineProfile> profiles, bool requiresSave = false)
    {
        ListPath = listPath;
        Profiles = profiles;
        RequiresSave = requiresSave;
    }

    public string ListPath { get; }

    public IReadOnlyList<GtpEngineProfile> Profiles { get; }

    /// <summary>旧形式の設定を読み込んだため、次回保存時に永続 ID を書き戻す必要があるか。</summary>
    public bool RequiresSave { get; }

    public static GtpEngineCatalog LoadFromDefaultLocation()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localApplicationData, "KifuwarabeGo2026", "GtpEngines");
        var listPath = Path.Combine(directory, "gtp-engine-list.json");
        if (!File.Exists(listPath))
        {
            Directory.CreateDirectory(directory);
            var defaultDirectory =
                Path.GetDirectoryName(ReleaseDefaultSettings.FilePath) ??
                AppContext.BaseDirectory;
            var defaultProfiles = ReleaseDefaultSettings.Current.EngineSettings.GtpEngines
                .Select(profile => Normalize(profile, defaultDirectory))
                .ToList();
            new GtpEngineCatalog(listPath, defaultProfiles).Save(defaultProfiles);
        }

        var catalog = Load(listPath);
        if (catalog.RequiresSave)
        {
            catalog.Save(catalog.Profiles);
            catalog = Load(listPath);
        }
        var developmentListPath = FindDevelopmentListPath();
        if (developmentListPath is null) return catalog;

        var profiles = catalog.Profiles.ToList();
        var changed = false;
        foreach (var defaultProfile in Load(developmentListPath).Profiles.Take(1))
        {
            if (profiles.Any(profile =>
                    string.Equals(profile.ExecutablePath, defaultProfile.ExecutablePath, StringComparison.OrdinalIgnoreCase)))
                continue;

            profiles.Insert(0, defaultProfile);
            changed = true;
        }

        if (!changed) return catalog;
        catalog.Save(profiles);
        return Load(listPath);
    }

    private static string? FindDevelopmentListPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx")))
            {
                var path = Path.Combine(
                    directory.FullName,
                    "KifuwarabeGo2026.Gui",
                    "Content",
                    "GtpEngines",
                    "gtp-engine-list.json");
                return File.Exists(path) ? path : null;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static GtpEngineCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
        {
            return new GtpEngineCatalog(listPath, Array.Empty<GtpEngineProfile>());
        }

        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var profiles = JsonSerializer.Deserialize<GtpEngineProfileList>(File.ReadAllText(listPath), JsonOptions)?.GtpEngines ?? new();
        var requiresSave = profiles.Any(profile => string.IsNullOrWhiteSpace(profile.Id));
        var normalizedProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ExecutablePath))
            .Select(profile => Normalize(profile, listDirectory))
            .ToList();

        return new GtpEngineCatalog(listPath, normalizedProfiles, requiresSave);
    }

    public void Save(IEnumerable<GtpEngineProfile> profiles)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(listDirectory);
        var list = new GtpEngineProfileList
        {
            GtpEngines = profiles
                .Select(profile => ToListEntry(Normalize(profile, listDirectory), listDirectory))
                .ToList(),
        };

        File.WriteAllText(ListPath, JsonSerializer.Serialize(list, JsonOptions));
    }

    private static GtpEngineProfile Normalize(GtpEngineProfile profile, string baseDirectory)
    {
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id)
            ? Guid.NewGuid().ToString("N")
            : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? "Unnamed GTP Engine"
            : normalized.DisplayName.Trim();
        normalized.DefaultCgosLoginName = normalized.DefaultCgosLoginName?.Trim() ?? "";
        normalized.DefaultCgosPlainTextPassword ??= "";
        normalized.InitialPositionProfileId = string.IsNullOrWhiteSpace(normalized.InitialPositionProfileId)
            ? "auto"
            : normalized.InitialPositionProfileId.Trim();
        normalized.InitialPositionDetectedEngineName ??= "";
        normalized.InitialPositionDetectedEngineVersion ??= "";
        normalized.InitialPositionDetectedProfileId ??= "";
        normalized.ExecutablePath = ResolvePath(normalized.ExecutablePath, baseDirectory);
        normalized.WorkingDirectoryModel = normalized.WorkingDirectoryModel.IsEmpty
            ? WorkingDirectoryModel.FromString(Path.GetDirectoryName(normalized.ExecutablePath) ?? baseDirectory)
            : WorkingDirectoryModel.FromString(ResolvePath(normalized.WorkingDirectoryModel.Value, baseDirectory));
        normalized.GuiOptions ??= [];
        foreach (var option in GtpEngineGuiOptions.Specs)
            normalized.GuiOptions.TryAdd(option.Id, option.DefaultValue);
        return normalized;
    }

    private static string ResolvePath(string path, string baseDirectory)
    {
        if (Path.IsPathFullyQualified(path) || !HasDirectoryPart(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static bool HasDirectoryPart(string path) =>
        path.Contains(Path.DirectorySeparatorChar) ||
        path.Contains(Path.AltDirectorySeparatorChar);

    private static GtpEngineProfile ToListEntry(GtpEngineProfile profile, string listDirectory)
    {
        var entry = profile.Clone();
        entry.ExecutablePath = ToStoredPath(entry.ExecutablePath, listDirectory);
        entry.WorkingDirectoryModel = WorkingDirectoryModel.FromString(ToStoredPath(entry.WorkingDirectoryModel.Value, listDirectory));
        return entry;
    }

    private static string ToStoredPath(string path, string listDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || !HasDirectoryPart(path))
        {
            return path;
        }

        try
        {
            return Path.GetRelativePath(listDirectory, Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private sealed class GtpEngineProfileList
    {
        public List<GtpEngineProfile> GtpEngines { get; set; } = new();
    }
}
