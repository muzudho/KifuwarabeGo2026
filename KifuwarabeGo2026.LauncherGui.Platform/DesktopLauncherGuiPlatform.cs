namespace KifuwarabeGo2026.LauncherGui.Platform;

using KifuwarabeGo2026.LauncherEngine;
using System.Diagnostics;

public sealed class DesktopLauncherGuiPlatform : ILauncherGuiPlatform
{
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

    public string? SelectFolder(string title, string initialDirectory)
    {
        var info = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("powershell.exe")
            : OperatingSystem.IsMacOS() ? new ProcessStartInfo("osascript") : new ProcessStartInfo("zenity");
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.CreateNoWindow = true;
        if (OperatingSystem.IsWindows())
        {
            info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-STA"); info.ArgumentList.Add("-Command");
            info.ArgumentList.Add("Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.FolderBrowserDialog; $d.Description=$args[0]; $d.SelectedPath=$args[1]; if($d.ShowDialog() -eq 'OK'){$d.SelectedPath}");
            info.ArgumentList.Add(title); info.ArgumentList.Add(initialDirectory);
        }
        else if (OperatingSystem.IsMacOS())
        {
            info.ArgumentList.Add("-e"); info.ArgumentList.Add($"POSIX path of (choose folder with prompt {AppleScriptQuote(title)} default location POSIX file {AppleScriptQuote(initialDirectory)})");
        }
        else
        {
            info.ArgumentList.Add("--file-selection"); info.ArgumentList.Add("--directory"); info.ArgumentList.Add("--title=" + title); info.ArgumentList.Add("--filename=" + initialDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        }
        try
        {
            using var process = Process.Start(info);
            if (process is null) return null;
            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && Directory.Exists(result) ? Path.GetFullPath(result) : null;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException) { return null; }
    }

    private static string AppleScriptQuote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

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
