namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.IO;
using System.Runtime.InteropServices;

internal sealed class WindowsShellLinkService
{
    private const string LauncherFileName = "KifuwarabeGo2026.Launcher.exe";

    public string ReadTarget(string shortcutPath)
    {
        ValidateShortcutPath(shortcutPath);
        dynamic? shortcut = null;
        try
        {
            shortcut = CreateShortcut(shortcutPath);
            return Path.GetFullPath((string)shortcut.TargetPath);
        }
        finally
        {
            Release(shortcut);
        }
    }

    public void RewriteLauncherTarget(string shortcutPath, string expectedOldTarget, string newTarget)
    {
        ValidateShortcutPath(shortcutPath);
        if (!File.Exists(newTarget)) throw new FileNotFoundException("The installed launcher was not found.", newTarget);

        dynamic? source = null;
        try
        {
            source = CreateShortcut(shortcutPath);
            var currentTarget = Path.GetFullPath((string)source.TargetPath);
            if (!string.Equals(Path.GetFileName(currentTarget), LauncherFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The shortcut no longer points to KifuwarabeGo2026.Launcher.exe.");
            if (!string.IsNullOrWhiteSpace(expectedOldTarget) &&
                !string.Equals(currentTarget, Path.GetFullPath(expectedOldTarget), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The shortcut target changed after it was registered.");

            var arguments = (string)source.Arguments;
            var description = (string)source.Description;
            var hotkey = (string)source.Hotkey;
            var iconLocation = (string)source.IconLocation;
            var windowStyle = (int)source.WindowStyle;
            var workingDirectory = (string)source.WorkingDirectory;
            var temporary = shortcutPath + ".updating.lnk";
            var backup = shortcutPath + ".backup";
            if (File.Exists(temporary)) File.Delete(temporary);
            if (File.Exists(backup)) File.Delete(backup);

            dynamic? replacement = null;
            try
            {
                replacement = CreateShortcut(temporary);
                replacement.TargetPath = newTarget;
                replacement.Arguments = arguments;
                replacement.Description = description;
                replacement.Hotkey = hotkey;
                replacement.WindowStyle = windowStyle;
                replacement.WorkingDirectory = ShouldFollowLauncher(workingDirectory, currentTarget)
                    ? Path.GetDirectoryName(newTarget)!
                    : workingDirectory;
                replacement.IconLocation = ShouldFollowLauncherIcon(iconLocation, currentTarget)
                    ? newTarget + ",0"
                    : iconLocation;
                replacement.Save();
            }
            finally
            {
                Release(replacement);
            }

            var writtenTarget = ReadTarget(temporary);
            if (!string.Equals(writtenTarget, Path.GetFullPath(newTarget), StringComparison.OrdinalIgnoreCase))
                throw new IOException("The replacement shortcut could not be verified.");
            File.Replace(temporary, shortcutPath, backup);
            try
            {
                File.Delete(backup);
            }
            catch (IOException)
            {
                // Replacement already succeeded; a stale backup is harmless.
            }
            catch (UnauthorizedAccessException)
            {
                // Replacement already succeeded; a stale backup is harmless.
            }
        }
        finally
        {
            Release(source);
        }
    }

    private static dynamic CreateShortcut(string path)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ??
            throw new PlatformNotSupportedException("Windows Script Host is not available.");
        dynamic shell = Activator.CreateInstance(shellType) ??
            throw new InvalidOperationException("Windows Script Host could not be started.");
        try
        {
            return shell.CreateShortcut(path);
        }
        finally
        {
            Release(shell);
        }
    }

    private static void ValidateShortcutPath(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Only ordinary Windows .lnk shortcuts are supported.");
        if (!File.Exists(path)) throw new FileNotFoundException("The shortcut was not found.", path);
    }

    private static bool ShouldFollowLauncher(string value, string oldTarget) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(Path.GetFullPath(value), Path.GetDirectoryName(oldTarget), StringComparison.OrdinalIgnoreCase);

    private static bool ShouldFollowLauncherIcon(string value, string oldTarget)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var path = value.Split(',')[0].Trim().Trim('"');
        return string.Equals(Path.GetFullPath(path), oldTarget, StringComparison.OrdinalIgnoreCase);
    }

    private static void Release(dynamic? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance)) Marshal.FinalReleaseComObject(instance);
    }
}
