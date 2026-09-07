namespace KifuwarabeGo2026.InstallerEngine;

internal sealed class InstallerSettings
{
    public string? InstallationDirectory { get; set; }
    public string? GuiCurrentVersion { get; set; }
    public string? GuiPreviousVersion { get; set; }
    public string? EngineCurrentVersion { get; set; }
    public string? EnginePreviousVersion { get; set; }
    // Keep the JSON key used by older installers and lobby versions.
    public bool? CloseLauncherAfterStartingGui { get; set; }

    public string? Current(InstallerProduct product) => product == InstallerProduct.Gui ? GuiCurrentVersion : EngineCurrentVersion;
    public string? Previous(InstallerProduct product) => product == InstallerProduct.Gui ? GuiPreviousVersion : EnginePreviousVersion;
    public void Promote(InstallerProduct product, string version)
    {
        version = version.TrimStart('v', 'V');
        if (product == InstallerProduct.Gui) { GuiPreviousVersion = GuiCurrentVersion; GuiCurrentVersion = version; }
        else { EnginePreviousVersion = EngineCurrentVersion; EngineCurrentVersion = version; }
    }
}
