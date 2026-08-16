namespace KifuwarabeGo2026.Shared;

using System.Text.Json;

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

    private sealed class SharedValues
    {
        public string? ScreenshotSaveDirectory { get; init; }
    }
}
