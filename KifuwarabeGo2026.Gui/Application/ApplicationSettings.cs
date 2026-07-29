namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed class ApplicationSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public ApplicationSettings()
    {
        TournamentRules = ReleaseDefaultSettings.Current.TournamentRuleSettings.TournamentRules
            .Select(rule => rule.Clone())
            .ToList();
        CgosConnections = ReleaseDefaultSettings.Current.CgosConnectionSettings.CgosConnections
            .ToList();
    }

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KifuwarabeGo2026",
        "application-settings.json");

    public static ApplicationSettings Current { get; private set; } = Load();

    public string LogRootDirectory { get; set; } = GetDefaultLogRootDirectory();

    public string SgfSaveDirectory { get; set; } = "";

    public bool IsSgfAutoSaveEnabled { get; set; }

    public List<TournamentRules> TournamentRules { get; set; }

    public List<CgosConnectionProfile> CgosConnections { get; set; }

    public static void Save(string logRootDirectory)
    {
        var fullPath = Path.GetFullPath(logRootDirectory.Trim());
        Directory.CreateDirectory(fullPath);
        Current.LogRootDirectory = fullPath;
        WriteCurrent();
    }

    public static void SaveTournamentRules(IEnumerable<TournamentRules> rules)
    {
        Current.TournamentRules = rules.Select(rule => rule.Clone()).ToList();
        WriteCurrent();
    }

    public static void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles)
    {
        Current.CgosConnections = profiles.ToList();
        WriteCurrent();
    }

    public static void SaveSgfDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory.Trim());
        Directory.CreateDirectory(fullPath);
        Current.SgfSaveDirectory = fullPath;
        WriteCurrent();
    }

    public static void SaveSgfAutoSaveEnabled(bool enabled)
    {
        Current.IsSgfAutoSaveEnabled = enabled;
        WriteCurrent();
    }

    private static ApplicationSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(FilePath), JsonOptions);
                if (settings is not null && !string.IsNullOrWhiteSpace(settings.LogRootDirectory))
                {
                    settings.LogRootDirectory = Path.GetFullPath(settings.LogRootDirectory);
                    settings.SgfSaveDirectory = NormalizeOptionalDirectory(settings.SgfSaveDirectory);
                    var releaseDefaults = new ApplicationSettings();
                    settings.TournamentRules ??= releaseDefaults.TournamentRules;
                    settings.CgosConnections ??= releaseDefaults.CgosConnections;
                    return settings;
                }
            }

            var legacyPath = Path.Combine(AppContext.BaseDirectory, "application-settings.json");
            if (File.Exists(legacyPath))
            {
                var legacy = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(legacyPath), JsonOptions);
                if (legacy is not null && !string.IsNullOrWhiteSpace(legacy.LogRootDirectory))
                {
                    legacy.LogRootDirectory = Path.GetFullPath(legacy.LogRootDirectory);
                    legacy.SgfSaveDirectory = NormalizeOptionalDirectory(legacy.SgfSaveDirectory);
                    var releaseDefaults = new ApplicationSettings();
                    legacy.TournamentRules = releaseDefaults.TournamentRules;
                    legacy.CgosConnections = releaseDefaults.CgosConnections;
                    TryWrite(legacy);
                    return legacy;
                }
            }
        }
        catch (Exception)
        {
            // A damaged settings file must not prevent the application from starting.
        }

        var defaults = new ApplicationSettings();
        TryWrite(defaults);
        return defaults;
    }

    private static void WriteCurrent() => Write(Current);

    private static void Write(ApplicationSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? AppContext.BaseDirectory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static void TryWrite(ApplicationSettings settings)
    {
        try
        {
            Write(settings);
        }
        catch (Exception)
        {
            // Read-only or temporarily unavailable user storage must not prevent startup.
        }
    }

    private static string NormalizeOptionalDirectory(string? directory) =>
        string.IsNullOrWhiteSpace(directory) ? "" : Path.GetFullPath(directory);

    private static string GetDefaultLogRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx")))
                return Path.Combine(directory.FullName, "Logs");
            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Logs");
    }
}
