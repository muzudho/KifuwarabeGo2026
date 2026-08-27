namespace KifuwarabeGo2026.Launcher;

internal sealed class LauncherSettings
{
    public string? InstallationDirectory { get; set; }
    public string? GuiCurrentVersion { get; set; }
    public string? GuiPreviousVersion { get; set; }
    public string? EngineCurrentVersion { get; set; }
    public string? EnginePreviousVersion { get; set; }
    public bool? CloseLauncherAfterStartingGui { get; set; }

    public string? Current(LauncherProduct product) => product == LauncherProduct.Gui ? GuiCurrentVersion : EngineCurrentVersion;
    public string? Previous(LauncherProduct product) => product == LauncherProduct.Gui ? GuiPreviousVersion : EnginePreviousVersion;
    public void Promote(LauncherProduct product, string version)
    {
        version = version.TrimStart('v', 'V');
        if (product == LauncherProduct.Gui) { GuiPreviousVersion = GuiCurrentVersion; GuiCurrentVersion = version; }
        else { EnginePreviousVersion = EngineCurrentVersion; EngineCurrentVersion = version; }
    }
}
