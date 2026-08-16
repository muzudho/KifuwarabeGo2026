namespace KifuwarabeGo2026.Launcher;

internal sealed class LauncherPaths
{
    public LauncherPaths(string? localApplicationData = null)
    {
        var local = localApplicationData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Root = Path.GetFullPath(Path.Combine(local, "KifuwarabeGo2026"));
    }

    public string Root { get; }
    public string SettingsFile => Path.Combine(Root, "launcher-settings.json");
    public string Downloads => Path.Combine(Root, "Downloads");
    public string LogFile => Path.Combine(Root, "Logs", "launcher.log");
    public string ProductRoot(LauncherProduct product) => Path.Combine(Root, "Packages", product == LauncherProduct.Gui ? "Gui" : "Engine");
    public string VersionDirectory(LauncherProduct product, string version) => Path.Combine(ProductRoot(product), "v" + version.Trim().TrimStart('v', 'V'));
}
