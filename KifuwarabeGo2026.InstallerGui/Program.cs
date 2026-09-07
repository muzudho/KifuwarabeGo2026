namespace KifuwarabeGo2026.InstallerGui;

using KifuwarabeGo2026.InstallerEngine;
using KifuwarabeGo2026.InstallerEngine.JsonLines;
using KifuwarabeGo2026.InstallerEngine.Platform;
using KifuwarabeGo2026.InstallerGui.Platform;
using System.Diagnostics;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Environment.SetEnvironmentVariable("KIFUWARABE_INSTALLER_PATH", Environment.ProcessPath);
        var dataOption = Array.FindIndex(args, value => value.Equals("--local-application-data", StringComparison.OrdinalIgnoreCase));
        var dataDirectory = dataOption < 0 ? null : Path.GetFullPath(args.ElementAtOrDefault(dataOption + 1)
            ?? throw new ArgumentException("--local-application-data requires a directory."));
        var enginePlatform = new DesktopInstallerEnginePlatform(dataDirectory);
        var guiPlatform = new DesktopInstallerGuiPlatform();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        var inProcessEngine = new InProcessInstallerEngine(enginePlatform, httpClient);
        using var jsonLinesEngine = args.Contains("--engine-stdio", StringComparer.OrdinalIgnoreCase)
            ? new JsonLinesInstallerEngine(CreateEngineHostStartInfo(dataDirectory), inProcessEngine)
            : null;
        IInstallerEngine engine = jsonLinesEngine is null ? inProcessEngine : jsonLinesEngine;
        string? startupMessage = null;
        // Dispatch before the management-window mutex: an open installer must not swallow play requests.
        if (args.Contains("--launch-lobby", StringComparer.OrdinalIgnoreCase))
        {
            var result = engine.StartGui();
            if (result.IsSuccess) return;
            startupMessage = result.Message;
        }
        // Retain the legacy mutex while the old and new entry points coexist.
        using var singleInstance = new Mutex(initiallyOwned: true, "KifuwarabeGo2026.Launcher", out var createdNew);
        using var activation = OperatingSystem.IsWindows()
            ? new EventWaitHandle(false, EventResetMode.AutoReset, "KifuwarabeGo2026.Installer.Activate") : null;
        if (!createdNew && !args.Contains("--allow-multiple", StringComparer.OrdinalIgnoreCase))
        {
            DesktopInstallerGuiPlatform.ActivateExistingInstaller();
            activation?.Set(); // Retained until an initializing management window starts its update loop.
            return;
        }
        using var game = new InstallerGame(guiPlatform, engine, startupMessage, activation);
        game.Run();
    }

    private static ProcessStartInfo CreateEngineHostStartInfo(string? dataDirectory)
    {
        var configuredPath = Environment.GetEnvironmentVariable("KIFUWARABE_INSTALLER_ENGINE_HOST");
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = Environment.GetEnvironmentVariable("KIFUWARABE_LAUNCHER_ENGINE_HOST");
        var hostPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "KifuwarabeGo2026.InstallerEngine.JsonLinesHost.dll")
            : Path.GetFullPath(configuredPath);
        if (!File.Exists(hostPath))
            throw new FileNotFoundException(
                "標準入出力版インストーラーエンジンホストが見つかりません。KIFUWARABE_INSTALLER_ENGINE_HOST で場所を指定できます。",
                hostPath);
        var isDll = string.Equals(Path.GetExtension(hostPath), ".dll", StringComparison.OrdinalIgnoreCase);
        var startInfo = new ProcessStartInfo(isDll ? "dotnet" : hostPath);
        if (isDll) startInfo.ArgumentList.Add(hostPath);
        if (dataDirectory is not null)
        {
            startInfo.ArgumentList.Add("--local-application-data");
            startInfo.ArgumentList.Add(dataDirectory);
        }
        return startInfo;
    }
}
