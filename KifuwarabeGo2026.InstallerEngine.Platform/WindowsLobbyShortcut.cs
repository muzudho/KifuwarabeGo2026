namespace KifuwarabeGo2026.InstallerEngine.Platform;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public static class WindowsLobbyShortcut
{
    public const string Arguments = "--launch-lobby";

    public static bool IsInstaller(string target) =>
        string.Equals(Path.GetFileName(target), "KifuwarabeGo2026.Installer.exe", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Path.GetFileName(target), "KifuwarabeGo2026.Launcher.exe", StringComparison.OrdinalIgnoreCase);

    public static void Create(string path, string executable)
    {
        path = Path.GetFullPath(path);
        executable = Path.GetFullPath(executable);
        if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Expected a Windows .lnk shortcut.");
        if (!File.Exists(executable) || !IsInstaller(executable))
            throw new FileNotFoundException("The installer executable was not found.", executable);
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new PlatformNotSupportedException("Windows Script Host is unavailable.");
        dynamic shell = Activator.CreateInstance(type)!;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".lnk";
        try
        {
            if (File.Exists(path))
            {
                dynamic old = shell.CreateShortcut(path);
                try
                {
                    // A matching display name is not proof that this is our shortcut.
                    if (!IsInstaller((string)old.TargetPath) ||
                        !string.Equals(Path.GetFullPath((string)old.TargetPath), executable, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(((string)old.Arguments).Trim(), Arguments, StringComparison.Ordinal))
                        throw new IOException("A different shortcut already exists. Rename it or choose a different location.");
                }
                finally { Marshal.FinalReleaseComObject(old); }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            dynamic link = shell.CreateShortcut(temporary);
            try
            {
                link.TargetPath = executable;
                link.Arguments = Arguments;
                link.WorkingDirectory = Path.GetDirectoryName(executable)!;
                link.Description = "きふわらべの碁２０２６ — ロビーを起動";
                link.IconLocation = executable + ",0";
                link.Save();
            }
            finally { Marshal.FinalReleaseComObject(link); }
            dynamic check = shell.CreateShortcut(temporary);
            try
            {
                if (!string.Equals((string)check.TargetPath, executable, StringComparison.OrdinalIgnoreCase) ||
                    (string)check.Arguments != Arguments)
                    throw new IOException("The lobby shortcut could not be verified.");
            }
            finally { Marshal.FinalReleaseComObject(check); }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
