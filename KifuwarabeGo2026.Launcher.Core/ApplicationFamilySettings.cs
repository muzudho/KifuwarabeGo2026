namespace KifuwarabeGo2026.Launcher;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>きふわらべの碁アプリケーション群と全バージョンで共有する利用者設定を読みます。</summary>
public static class ApplicationFamilySettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KifuwarabeGo2026",
        "application-settings.json");

    public static string DefaultScreenshotSaveDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "KifuwarabeGo2026",
        "Screenshots");

    public static string ScreenshotSaveDirectory
    {
        get
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var values = JsonSerializer.Deserialize<SharedValues>(File.ReadAllText(FilePath), JsonOptions);
                    if (!string.IsNullOrWhiteSpace(values?.ScreenshotSaveDirectory))
                        return Path.GetFullPath(values.ScreenshotSaveDirectory);
                }
            }
            catch (Exception)
            {
                // 壊れた設定でスクリーンショット操作やアプリケーション起動を妨げない。
            }

            return DefaultScreenshotSaveDirectory;
        }
    }

    public static bool CloseLauncherAfterStartingGui
    {
        get
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var values = JsonSerializer.Deserialize<SharedValues>(File.ReadAllText(FilePath), JsonOptions);
                    return values?.CloseLauncherAfterStartingGui ?? true;
                }
            }
            catch (Exception) { }
            return true;
        }
    }

    public static void SaveScreenshotDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory.Trim());
        Directory.CreateDirectory(fullPath);
        UpdateSetting(root => root[nameof(ScreenshotSaveDirectory)] = fullPath);
    }

    public static void SaveCloseLauncherAfterStartingGui(bool value) =>
        UpdateSetting(root => root[nameof(CloseLauncherAfterStartingGui)] = value);

    private static void UpdateSetting(Action<JsonObject> update)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        JsonObject root;
        try { root = File.Exists(FilePath) ? JsonNode.Parse(File.ReadAllText(FilePath)) as JsonObject ?? [] : []; }
        catch (JsonException) { root = []; }
        update(root);
        var temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    private sealed class SharedValues
    {
        public string? ScreenshotSaveDirectory { get; init; }
        public bool? CloseLauncherAfterStartingGui { get; init; }
    }
}
