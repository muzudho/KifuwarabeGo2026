namespace KifuwarabeGo2026.Launcher;

using System.Text.Json;

internal sealed class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly object _gate = new();
    private readonly LauncherPaths paths;
    private readonly IFileSystem fileSystem;

    public LauncherSettingsStore(LauncherPaths paths, IFileSystem? fileSystem = null)
    {
        this.paths = paths;
        this.fileSystem = fileSystem ?? new SystemFileSystem();
    }

    public LauncherSettings Load()
    {
        lock (_gate)
        {
            if (!fileSystem.FileExists(paths.SettingsFile)) return new LauncherSettings();
            try { return JsonSerializer.Deserialize<LauncherSettings>(fileSystem.ReadAllText(paths.SettingsFile), Options) ?? new(); }
            catch (JsonException) { return new(); }
        }
    }

    public void Save(LauncherSettings settings)
    {
        lock (_gate)
        {
            fileSystem.CreateDirectory(paths.Root);
            var temporary = paths.SettingsFile + ".tmp";
            fileSystem.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
            if (fileSystem.FileExists(paths.SettingsFile)) fileSystem.ReplaceFile(temporary, paths.SettingsFile);
            else fileSystem.MoveFile(temporary, paths.SettingsFile);
        }
    }
}
