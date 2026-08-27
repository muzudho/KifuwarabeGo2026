namespace KifuwarabeGo2026.LauncherEngine;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>ゲームオアシスのＧＵＩ群と全バージョンで共有する利用者設定の場所を表します。</summary>
public static class SharedGuiSettings
{
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KifuwarabeGo2026",
        "application-settings.json");

    public static string DefaultScreenshotSaveDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "KifuwarabeGo2026",
        "Screenshots");
}

internal sealed class SharedGuiSettingsStore(string localApplicationData, string myPictures)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly object _gate = new();

    public string FilePath { get; } = Path.Combine(
        Path.GetFullPath(localApplicationData),
        "KifuwarabeGo2026",
        "application-settings.json");

    public string DefaultScreenshotSaveDirectory { get; } = Path.Combine(
        Path.GetFullPath(myPictures),
        "KifuwarabeGo2026",
        "Screenshots");

    public string ScreenshotSaveDirectory
    {
        get
        {
            try
            {
                var values = ReadValues();
                if (!string.IsNullOrWhiteSpace(values?.ScreenshotSaveDirectory))
                    return Path.GetFullPath(values.ScreenshotSaveDirectory);
            }
            catch (Exception)
            {
                // 壊れた設定でスクリーンショット操作やアプリケーション起動を妨げない。
            }

            return DefaultScreenshotSaveDirectory;
        }
    }

    public bool? ReadLegacyCloseLauncherAfterStartingGui()
    {
        try { return ReadValues()?.CloseLauncherAfterStartingGui; }
        catch (Exception) { return null; }
    }

    public void SaveScreenshotDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory.Trim());
        Directory.CreateDirectory(fullPath);
        UpdateSetting(root => root[nameof(ScreenshotSaveDirectory)] = fullPath);
    }

    private SharedValues? ReadValues()
    {
        lock (_gate)
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<SharedValues>(File.ReadAllText(FilePath), JsonOptions)
                : null;
        }
    }

    private void UpdateSetting(Action<JsonObject> update)
    {
        lock (_gate)
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
    }

    private sealed class SharedValues
    {
        public string? ScreenshotSaveDirectory { get; init; }
        public bool? CloseLauncherAfterStartingGui { get; init; }
    }
}
