namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.LobbyEngine;
using KifuwarabeGo2026.LobbyEngine.JsonLines;
using KifuwarabeGo2026.LobbyGui.Application;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>互換GUI向けのLobby GUI実装を組み立てる構成点です。</summary>
internal static class LobbyGuiComposition
{
    public static LobbyGuiController CreateDefault()
    {
        var releaseDefaultDirectory = Path.GetDirectoryName(ReleaseDefaultSettings.FilePath) ?? AppContext.BaseDirectory;
        ILobbyEngine engine = InProcessLobbyEngine.CreateDefault(
            ApplicationSettingsCgosConnectionStore.Instance,
            ReleaseDefaultSettings.Current.EngineSettings.GtpEngines,
            releaseDefaultDirectory);
        if (TryCreateHostStartInfo(out var hostStartInfoFactory))
            engine = new JsonLinesLobbyEngine(hostStartInfoFactory, engine);

        var selectedEngine = engine;
        return new LobbyGuiController(
            selectedEngine,
            ApplicationSettings.FilePath,
            () => (selectedEngine as JsonLinesLobbyEngine)?.CommunicationWarning);
    }

    private static bool TryCreateHostStartInfo(out Func<ProcessStartInfo> factory)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Tools", "LobbyEngine", "KifuwarabeGo2026.LobbyEngine.JsonLinesHost.exe"),
            Path.Combine(AppContext.BaseDirectory, "KifuwarabeGo2026.LobbyEngine.JsonLinesHost.exe"),
        };
        var executablePath = candidates.FirstOrDefault(File.Exists);
        if (executablePath is null)
        {
            factory = null!;
            return false;
        }

        factory = () => new ProcessStartInfo(executablePath);
        return true;
    }
}
