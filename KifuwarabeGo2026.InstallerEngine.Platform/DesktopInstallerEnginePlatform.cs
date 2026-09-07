namespace KifuwarabeGo2026.InstallerEngine.Platform;

using System.Diagnostics;

public sealed class DesktopInstallerEnginePlatform(string? localApplicationData = null) : IInstallerEnginePlatform
{
    public string LocalApplicationData => localApplicationData ?? GetFolderOrFallback(Environment.SpecialFolder.LocalApplicationData, AppContext.BaseDirectory);
    public string MyPictures => GetFolderOrFallback(Environment.SpecialFolder.MyPictures, Path.Combine(LocalApplicationData, "Pictures"));

    private static string GetFolderOrFallback(Environment.SpecialFolder folder, string fallback)
    {
        var directory = Environment.GetFolderPath(folder);
        return string.IsNullOrWhiteSpace(directory) ? fallback : directory;
    }

    public bool Start(string executable, string workingDirectory)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        var launcherPath = Environment.GetEnvironmentVariable("KIFUWARABE_INSTALLER_PATH") ?? Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(launcherPath))
        {
            info.Environment["KIFUWARABE_INSTALLER_PATH"] = launcherPath;
            info.Environment["KIFUWARABE_LAUNCHER_PATH"] = launcherPath;
        }
        return TryStart(info);
    }

    public bool IsProcessRunningFrom(string directory)
    {
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var prefix = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var executable = process.MainModule?.FileName;
                if (executable is not null && Path.GetFullPath(executable).StartsWith(prefix, comparison)) return true;
            }
            catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or UnauthorizedAccessException) { }
            finally { process.Dispose(); }
        }
        return false;
    }

    private static bool TryStart(ProcessStartInfo info)
    {
        try { return Process.Start(info) is not null; }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException) { return false; }
    }
}
