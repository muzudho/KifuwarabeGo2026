namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ApplicationSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KifuwarabeGo2026",
        "application-settings.json");

    public static ApplicationSettings Current { get; private set; } = Load();

    public string LogRootDirectory { get; set; } = GetDefaultLogRootDirectory();

    public List<TournamentRules> TournamentRules { get; set; } = CreateDefaultTournamentRules();

    public List<CgosConnectionProfile> CgosConnections { get; set; } = CreateDefaultCgosConnections();

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
                    settings.TournamentRules ??= CreateDefaultTournamentRules();
                    settings.CgosConnections ??= CreateDefaultCgosConnections();
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
                    legacy.TournamentRules = CreateDefaultTournamentRules();
                    legacy.CgosConnections = CreateDefaultCgosConnections();
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

    private static List<TournamentRules> CreateDefaultTournamentRules() =>
    [
        new TournamentRules
        {
            Id = "cgf-open-2026-9ro",
            DisplayName = "CGF Open 2026 9-ro",
            Rule = GoRuleKind.Chinese,
            BoardSize = 9,
            Komi = 7.0m,
            MainTimeMinutes = 10,
            MainTimeSeconds = 0,
            MoveLimit = 400,
        },
        new TournamentRules
        {
            Id = "cgf-open-2026-19ro",
            DisplayName = "CGF Open 2026 19-ro",
            Rule = GoRuleKind.Japanese,
            BoardSize = 19,
            Komi = 6.5m,
            MainTimeMinutes = 30,
            MainTimeSeconds = 0,
            MoveLimit = 400,
        },
    ];

    private static List<CgosConnectionProfile> CreateDefaultCgosConnections() =>
    [
        new("CGF Open 2026", "uec-go.com", 6809, "1day, 2day", "CGOS server")
        {
            Event = "CGF Open 2026",
        },
    ];

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
