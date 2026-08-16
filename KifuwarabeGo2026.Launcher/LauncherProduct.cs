namespace KifuwarabeGo2026.Launcher;

internal enum LauncherProduct { Gui, Engine }

internal static class LauncherProductExtensions
{
    public static string DisplayName(this LauncherProduct product) => product == LauncherProduct.Gui ? "GUI" : "ENGINE";
    public static string AssetName(this LauncherProduct product, string version) =>
        $"KifuwarabeGo2026.{product.DisplayName().Substring(0, 1) + product.DisplayName()[1..].ToLowerInvariant()}-v{version.TrimStart('v', 'V')}-win-x64.zip";
    public static string ExecutableName(this LauncherProduct product) =>
        product == LauncherProduct.Gui ? "KifuwarabeGo2026.Gui.exe" : "KifuwarabeGo2026.Engine.exe";
}
