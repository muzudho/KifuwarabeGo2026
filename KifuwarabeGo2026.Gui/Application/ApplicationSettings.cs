namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.IO;
using System.Text.Json;

public sealed class ApplicationSettings
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "application-settings.json");

    public static ApplicationSettings Current { get; private set; } = Load();

    public string LogRootDirectory { get; set; } = GetDefaultLogRootDirectory();

    public static void Save(string logRootDirectory)
    {
        var fullPath = Path.GetFullPath(logRootDirectory.Trim());
        Directory.CreateDirectory(fullPath);
        Current = new ApplicationSettings { LogRootDirectory = fullPath };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static ApplicationSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null && !string.IsNullOrWhiteSpace(settings.LogRootDirectory))
                {
                    settings.LogRootDirectory = Path.GetFullPath(settings.LogRootDirectory);
                    return settings;
                }
            }
        }
        catch (Exception)
        {
            // A damaged settings file must not prevent the application from starting.
        }

        return new ApplicationSettings();
    }

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
