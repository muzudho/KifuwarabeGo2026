namespace KifuwarabeGo2026.LauncherEngine.Platform;

using System.Diagnostics;

public sealed class DesktopLauncherEnginePlatform : ILauncherEnginePlatform
{
    public string LocalApplicationData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public string MyPictures => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    public bool Start(string executable, string workingDirectory)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        var launcherPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(launcherPath))
            info.Environment["KIFUWARABE_LAUNCHER_PATH"] = launcherPath;
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

