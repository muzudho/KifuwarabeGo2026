namespace KifuwarabeGo2026.LauncherGui;

using KifuwarabeGo2026.LauncherEngine;
using KifuwarabeGo2026.LauncherEngine.JsonLines;
using KifuwarabeGo2026.LauncherEngine.Platform;
using KifuwarabeGo2026.LauncherGui.Platform;
using System.Diagnostics;

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
        var inProcessEngine = new InProcessLauncherEngine(enginePlatform, httpClient);
        using var jsonLinesEngine = args.Contains("--engine-stdio", StringComparer.OrdinalIgnoreCase)
            ? new JsonLinesLauncherEngine(CreateEngineHostStartInfo(), inProcessEngine)
            : null;
        ILauncherEngine engine = jsonLinesEngine is null ? inProcessEngine : jsonLinesEngine;
        using var game = new LauncherGame(guiPlatform, engine);
        game.Run();
    }

    private static ProcessStartInfo CreateEngineHostStartInfo()
    {
        var configuredPath = Environment.GetEnvironmentVariable("KIFUWARABE_LAUNCHER_ENGINE_HOST");
        var executable = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "KifuwarabeGo2026.LauncherEngine.JsonLinesHost.exe")
            : Path.GetFullPath(configuredPath);
        if (!File.Exists(executable))
            throw new FileNotFoundException(
                "標準入出力版ランチャーエンジンホストが見つかりません。KIFUWARABE_LAUNCHER_ENGINE_HOST で場所を指定できます。",
                executable);
        return new ProcessStartInfo(executable);
    }
}
