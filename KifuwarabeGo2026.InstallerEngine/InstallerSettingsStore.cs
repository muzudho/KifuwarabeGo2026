namespace KifuwarabeGo2026.InstallerEngine;

using System.Text.Json;

internal sealed class InstallerSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly object _gate = new();
    private readonly InstallerPaths paths;
    private readonly IFileSystem fileSystem;

    public InstallerSettingsStore(InstallerPaths paths, IFileSystem? fileSystem = null)
    {
        this.paths = paths;
        this.fileSystem = fileSystem ?? new SystemFileSystem();
    }

    public InstallerSettings Load()
    {
        lock (_gate)
        {
            if (!fileSystem.FileExists(paths.SettingsFile)) return new InstallerSettings();
            try { return JsonSerializer.Deserialize<InstallerSettings>(fileSystem.ReadAllText(paths.SettingsFile), Options) ?? new(); }
            catch (JsonException) { return new(); }
        }
    }

    public void Save(InstallerSettings settings)
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
