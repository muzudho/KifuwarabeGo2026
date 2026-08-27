namespace KifuwarabeGo2026.Launcher;

using KifuwarabeGo2026.Launcher.Platform;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new Mutex(initiallyOwned: true, "KifuwarabeGo2026.Launcher", out var createdNew);
        if (!createdNew && !args.Contains("--allow-multiple", StringComparer.OrdinalIgnoreCase)) return;
        var platform = new DesktopPlatformServices();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        var engine = new InProcessLauncherEngine(platform, httpClient);
        using var game = new LauncherGame(platform, engine);
        game.Run();
    }
}
