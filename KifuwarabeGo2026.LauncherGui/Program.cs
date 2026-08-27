namespace KifuwarabeGo2026.LauncherGui;

using KifuwarabeGo2026.LauncherEngine;
using KifuwarabeGo2026.LauncherEngine.Platform;
using KifuwarabeGo2026.LauncherGui.Platform;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new Mutex(initiallyOwned: true, "KifuwarabeGo2026.Launcher", out var createdNew);
        if (!createdNew && !args.Contains("--allow-multiple", StringComparer.OrdinalIgnoreCase)) return;
        var enginePlatform = new DesktopLauncherEnginePlatform();
        var guiPlatform = new DesktopLauncherGuiPlatform();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        var engine = new InProcessLauncherEngine(enginePlatform, httpClient);
        using var game = new LauncherGame(guiPlatform, engine);
        game.Run();
    }
}
