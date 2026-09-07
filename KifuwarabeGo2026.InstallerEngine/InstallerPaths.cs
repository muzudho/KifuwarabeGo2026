namespace KifuwarabeGo2026.InstallerEngine;

internal sealed class InstallerPaths
{
    public InstallerPaths(string localApplicationData, string? installationDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        Root = Path.GetFullPath(Path.Combine(localApplicationData, "KifuwarabeGo2026"));
        InstallationRoot = string.IsNullOrWhiteSpace(installationDirectory)
            ? Root
            : Path.GetFullPath(installationDirectory);
    }

    public string Root { get; }
    public string InstallationRoot { get; }
    // Persisted names are shared with existing Launcher installations; do not rename without migration.
    public string SettingsFile => Path.Combine(Root, "launcher-settings.json");
    public string Downloads => Path.Combine(Root, "Downloads");
    public string LogFile => Path.Combine(Root, "Logs", "launcher.log");
    public string ProductRoot(InstallerProduct product) => Path.Combine(InstallationRoot, "Packages", product == InstallerProduct.Gui ? "Gui" : "Engine");
    public string VersionDirectory(InstallerProduct product, string version) => Path.Combine(ProductRoot(product), "v" + version.Trim().TrimStart('v', 'V'));
}
