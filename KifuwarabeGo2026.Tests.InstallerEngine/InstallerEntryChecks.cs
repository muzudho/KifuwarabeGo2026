using System.Diagnostics;
using KifuwarabeGo2026.InstallerEngine;

internal static class InstallerEntryChecks
{
    public static void Run(string publishedDirectory)
    {
        var root = Path.Combine(Path.GetTempPath(), "Installer entry 日本語 " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var paths = new InstallerPaths(root);
            var settings = new InstallerSettings { GuiCurrentVersion = "9.0.0", GuiPreviousVersion = "8.0.0" };
            var previous = paths.VersionDirectory(InstallerProduct.Gui, "8.0.0");
            Directory.CreateDirectory(previous);
            // A real apphost stands in for the lobby, so the test checks OS process creation and lifetime.
            foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory))
                File.Copy(file, Path.Combine(previous, Path.GetFileName(file)));
            File.Copy(Path.Combine(previous, "KifuwarabeGo2026.Tests.InstallerEngine.exe"),
                Path.Combine(previous, InstallerProduct.Gui.ExecutableName()));
            new InstallerSettingsStore(paths).Save(settings);
            // Management is already open: a launch request must bypass its mutex.
            using var management = new Mutex(true, "KifuwarabeGo2026.Launcher", out _);
            foreach (var name in new[] { "KifuwarabeGo2026.Installer.exe", "KifuwarabeGo2026.Launcher.exe" })
            {
                var marker = Path.Combine(root, name + ".started");
                var executable = Path.Combine(Path.GetFullPath(publishedDirectory), name);
                var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
                start.ArgumentList.Add("--launch-lobby");
                start.ArgumentList.Add("--local-application-data");
                start.ArgumentList.Add(root);
                start.Environment["KIFUWARABE_TEST_LOBBY_MARKER"] = marker;
                using var process = Process.Start(start) ?? throw new Exception("Installer did not start.");
                var errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(15000))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit();
                    var log = File.Exists(paths.LogFile) ? File.ReadAllText(paths.LogFile) : "No installer log.";
                    throw new Exception("Launch-only entry opened a management loop instead of exiting. " + log + errors.GetAwaiter().GetResult());
                }
                if (process.ExitCode != 0) throw new Exception($"Installer exited {process.ExitCode}: {errors.GetAwaiter().GetResult()}");
                if (!SpinWait.SpinUntil(() => File.Exists(marker), 5000)) throw new Exception("Lobby was not launched.");
                var inherited = File.ReadAllLines(marker);
                if (inherited.Length != 2 || inherited.Any(value => value != executable))
                    throw new Exception("The lobby did not inherit both new and legacy installer paths.");
            }
            Console.WriteLine("PASS: published Installer and Launcher entries launch the previous lobby while management is open.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
