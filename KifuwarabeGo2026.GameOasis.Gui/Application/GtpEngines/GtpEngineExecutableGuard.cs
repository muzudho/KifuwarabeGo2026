namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using System.IO;

/// <summary>GUI自身をGTPエンジンとして起動しないための事前検査です。</summary>
public static class GtpEngineExecutableGuard
{
    public const string GuiSelectedMessage = "This is the GUI. Select KifuwarabeGo2026.Engine.exe.";

    public static bool IsGuiApplication(GtpEngineProfile profile) =>
        IsGuiApplication(profile.ExecutablePath, profile.Arguments);

    public static bool IsGuiApplication(string executablePath, string arguments = "")
    {
        if (HasGuiFileName(executablePath) || ContainsGuiAssemblyArgument(arguments))
            return true;

        try
        {
            var applicationPath = Environment.ProcessPath;
            return !string.IsNullOrWhiteSpace(applicationPath) &&
                   Path.IsPathFullyQualified(executablePath) &&
                   Path.GetFullPath(executablePath).Equals(Path.GetFullPath(applicationPath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    public static string? FindRuntimeSpecificSibling(GtpEngineProfile profile)
    {
        try
        {
            if (!Path.IsPathFullyQualified(profile.ExecutablePath)) return null;
            var directory = Path.GetDirectoryName(profile.ExecutablePath);
            if (string.IsNullOrWhiteSpace(directory)) return null;
            var candidate = Path.Combine(directory, "win-x64", Path.GetFileName(profile.ExecutablePath));
            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool HasGuiFileName(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Contains("KifuwarabeGo2026.GameOasis.Gui", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("KifuwarabeGo2026.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsGuiAssemblyArgument(string arguments) =>
        arguments.Contains("KifuwarabeGo2026.GameOasis.Gui.dll", StringComparison.OrdinalIgnoreCase) ||
        arguments.Contains("KifuwarabeGo2026.GameOasis.Gui.exe", StringComparison.OrdinalIgnoreCase);
}
