namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
    };

    private GtpEngineCatalog(string listPath, IReadOnlyList<GtpEngineProfile> profiles)
    {
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }

    public IReadOnlyList<GtpEngineProfile> Profiles { get; }

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

        return Load(listPath);
    }

    public static GtpEngineCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
        {
            return new GtpEngineCatalog(listPath, Array.Empty<GtpEngineProfile>());
        }

        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var profiles = JsonSerializer.Deserialize<GtpEngineProfileList>(File.ReadAllText(listPath), JsonOptions)?.GtpEngines ?? new();
        var normalizedProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ExecutablePath))
            .Select(profile => Normalize(profile, listDirectory))
            .ToList();

        return new GtpEngineCatalog(listPath, normalizedProfiles);
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
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? "Unnamed GTP Engine"
            : normalized.DisplayName.Trim();
        normalized.DefaultCgosLoginName = normalized.DefaultCgosLoginName?.Trim() ?? "";
        normalized.DefaultCgosPlainTextPassword ??= "";
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
