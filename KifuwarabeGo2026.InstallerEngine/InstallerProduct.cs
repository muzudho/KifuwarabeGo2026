namespace KifuwarabeGo2026.InstallerEngine;

public enum InstallerProduct { Gui, Engine }

public static class InstallerProductExtensions
{
    public static string DisplayName(this InstallerProduct product) => product == InstallerProduct.Gui ? "GUI" : "ENGINE";
    public static string AssetName(this InstallerProduct product, string version) =>
        $"KifuwarabeGo2026.{product.DisplayName().Substring(0, 1) + product.DisplayName()[1..].ToLowerInvariant()}-v{version.TrimStart('v', 'V')}-win-x64.zip";
    public static string ExecutableName(this InstallerProduct product) =>
        product == InstallerProduct.Gui ? "KifuwarabeGo2026.GameOasis.Gui.Windows.exe" : "KifuwarabeGo2026.Engine.exe";
}
