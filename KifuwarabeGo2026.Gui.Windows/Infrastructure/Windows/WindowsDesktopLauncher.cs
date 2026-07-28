namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using System;
using System.Diagnostics;

/// <summary>
/// Windows シェルを使ってファイルや保存場所を開きます。
/// </summary>
public sealed class WindowsDesktopLauncher : IDesktopLauncher
{
    public void OpenTextFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "notepad",
                UseShellExecute = true,
            };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }
    }

    public DesktopOpenResult OpenFileWithPreferredApplication(
        string path,
        string preferredApplication)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredApplication);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = preferredApplication,
                UseShellExecute = true,
            };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
            return DesktopOpenResult.PreferredApplication;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            return DesktopOpenResult.DefaultApplication;
        }
    }

    public void OpenDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add(path);
        Process.Start(startInfo);
    }

    public void RevealFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add("/select,");
        startInfo.ArgumentList.Add(path);
        Process.Start(startInfo);
    }

    public void TailTextFile(string path, string windowTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(windowTitle);

        var escapedPath = path.Replace("'", "''", StringComparison.Ordinal);
        var escapedTitle = windowTitle.Replace("'", "''", StringComparison.Ordinal);
        var command = $"$Host.UI.RawUI.WindowTitle = '{escapedTitle}'; Get-Content -LiteralPath '{escapedPath}' -Wait";
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = true,
        };
        startInfo.ArgumentList.Add("-NoExit");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);
        Process.Start(startInfo);
    }
}
