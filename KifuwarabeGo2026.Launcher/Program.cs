namespace KifuwarabeGo2026.Launcher;

using KifuwarabeGo2026.Launcher.Platform;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var singleInstance = new Mutex(initiallyOwned: true, "KifuwarabeGo2026.Launcher", out var createdNew);
        if (!createdNew && !args.Contains("--allow-multiple", StringComparer.OrdinalIgnoreCase)) return;
        using var game = new LauncherGame(new DesktopPlatformServices());
        game.Run();
    }
}
