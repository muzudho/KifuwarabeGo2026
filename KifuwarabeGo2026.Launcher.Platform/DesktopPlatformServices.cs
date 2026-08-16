namespace KifuwarabeGo2026.Launcher.Platform;

using KifuwarabeGo2026.Launcher;
using System.Diagnostics;

public sealed class DesktopPlatformServices : IPlatformServices
{
    public string LocalApplicationData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public bool Start(string executable, string workingDirectory) => TryStart(new ProcessStartInfo(executable)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = true,
    });

    public bool OpenFolder(string directory)
    {
        if (!Directory.Exists(directory)) return false;
        return TryStart(CreateOpenInfo(directory));
    }

    public bool OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        return TryStart(OperatingSystem.IsWindows()
            ? new ProcessStartInfo(filePath) { UseShellExecute = true }
            : CreateOpenInfo(filePath));
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

    private static ProcessStartInfo CreateOpenInfo(string target) => OperatingSystem.IsWindows()
        ? new ProcessStartInfo("explorer.exe", target) { UseShellExecute = true }
        : OperatingSystem.IsMacOS()
            ? new ProcessStartInfo("open", target) { UseShellExecute = false }
            : new ProcessStartInfo("xdg-open", target) { UseShellExecute = false };

    private static bool TryStart(ProcessStartInfo info)
    {
        try { return Process.Start(info) is not null; }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException) { return false; }
    }
}
