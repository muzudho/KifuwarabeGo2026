namespace KifuwarabeGo2026.Launcher;

using System.Diagnostics;

internal sealed class ProductLauncher(LauncherPaths paths, LauncherSettingsStore settingsStore, LauncherLog log)
{
    public LaunchResult StartGui()
    {
        var settings = settingsStore.Load();
        var current = TryStart(settings.GuiCurrentVersion);
        if (current.Success) return current;
        var previous = TryStart(settings.GuiPreviousVersion);
        if (previous.Success) return previous with { UsedPrevious = true, Message = $"現在版を起動できなかったため、直前版 v{settings.GuiPreviousVersion} を起動しました。" };
        return new(false, false, "起動できるGUIがありません。［GUI UPDATE］で最新版を取得してください。");
    }

    public string? CurrentDirectory(LauncherProduct product)
    {
        var version = settingsStore.Load().Current(product);
        return string.IsNullOrWhiteSpace(version) ? null : paths.VersionDirectory(product, version);
    }

    private LaunchResult TryStart(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return new(false, false, "バージョン未設定です。");
        var directory = paths.VersionDirectory(LauncherProduct.Gui, version);
        var executable = Path.Combine(directory, LauncherProduct.Gui.ExecutableName());
        if (!File.Exists(executable)) return new(false, false, $"{executable} がありません。");
        try
        {
            Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = true });
            log.Write($"GUI START v{version} {executable}");
            return new(true, false, $"GUI v{version} を起動しました。");
        }
        catch (Exception exception) { log.Write($"GUI START FAILED v{version}: {exception}"); return new(false, false, exception.Message); }
    }
}

internal sealed record LaunchResult(bool Success, bool UsedPrevious, string Message);
