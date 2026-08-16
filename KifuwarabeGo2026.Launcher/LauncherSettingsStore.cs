namespace KifuwarabeGo2026.Launcher;

using System.Text.Json;

internal sealed class LauncherSettingsStore(LauncherPaths paths)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly object _gate = new();

    public LauncherSettings Load()
    {
        lock (_gate)
        {
            if (!File.Exists(paths.SettingsFile)) return new LauncherSettings();
            try { return JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(paths.SettingsFile), Options) ?? new(); }
            catch (JsonException) { return new(); }
        }
    }

    public void Save(LauncherSettings settings)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(paths.Root);
            var temporary = paths.SettingsFile + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
            if (File.Exists(paths.SettingsFile)) File.Replace(temporary, paths.SettingsFile, null);
            else File.Move(temporary, paths.SettingsFile);
        }
    }
}
