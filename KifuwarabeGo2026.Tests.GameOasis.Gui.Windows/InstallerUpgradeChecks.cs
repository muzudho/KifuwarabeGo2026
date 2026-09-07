namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Windows;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

internal static class InstallerUpgradeChecks
{
    public static void Run(string archivePath, string version)
    {
        var root = Path.Combine(Path.GetTempPath(), "Installer upgrade 日本語 " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var zip = File.ReadAllBytes(archivePath);
            foreach (var assetKind in new[] { "Installer", "Launcher" })
            {
                var data = Path.Combine(root, assetKind);
                var handler = new ReleaseFixture(zip, version, assetKind);
                using var client = new HttpClient(handler);
                var installer = new WindowsInstallerPackageInstaller(data, client);
                var result = installer.InstallLatestAsync().GetAwaiter().GetResult();
                if (!File.Exists(result.ExecutablePath) || Path.GetFileName(result.ExecutablePath) != "KifuwarabeGo2026.Installer.exe")
                    throw new Exception("Fresh install did not select the canonical executable.");
                var shortcut = Path.Combine(data, "desktop.lnk");
                var links = new WindowsShellLinkService();
                links.CreateOrReplaceInstallerShortcut(shortcut, result.ExecutablePath);
                var initialTarget = links.ReadTarget(shortcut);

                // Model a user's older fixed Current containing only the legacy entry point.
                File.Move(Path.Combine(installer.CurrentDirectory, "KifuwarabeGo2026.Installer.exe"),
                    Path.Combine(installer.CurrentDirectory, "canonical.saved"));
                if (Path.GetFileName(installer.CurrentExecutable) != "KifuwarabeGo2026.Launcher.exe")
                    throw new Exception("Legacy-only Current was not detected.");
                WindowsInstallerPackageInstaller.ValidatePackage(installer.CurrentDirectory, version);

                handler.BadChecksum = true;
                try
                {
                    installer.InstallLatestAsync().GetAwaiter().GetResult();
                    throw new Exception("A corrupt update was accepted.");
                }
                catch (InvalidDataException) { }
                if (Path.GetFileName(installer.CurrentExecutable) != "KifuwarabeGo2026.Launcher.exe")
                    throw new Exception("Rejected update changed the existing installation.");

                handler.BadChecksum = false;
                installer.InstallLatestAsync().GetAwaiter().GetResult();
                if (links.ReadTarget(shortcut) != initialTarget || links.ReadArguments(shortcut) != "--launch-lobby" || !File.Exists(initialTarget))
                    throw new Exception("Installer replacement broke the lobby shortcut.");
                var previous = Path.Combine(data, "KifuwarabeGo2026", "Launcher", "Previous");
                if (!File.Exists(Path.Combine(previous, "KifuwarabeGo2026.Launcher.exe")))
                    throw new Exception("Previous installer was not retained for recovery.");
                Directory.Delete(previous, recursive: true);
                if (!File.Exists(links.ReadTarget(shortcut))) throw new Exception("Removing the previous installer broke the stable shortcut.");
            }
            Console.WriteLine("PASS: fresh install, legacy asset fallback, rejected checksum, legacy Current upgrade and stable shortcuts after old-version removal.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private sealed class ReleaseFixture(byte[] zip, string version, string assetKind) : HttpMessageHandler
    {
        public bool BadChecksum { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var asset = $"KifuwarabeGo2026.{assetKind}-v{version}-win-x64.zip";
            HttpContent body = request.RequestUri!.AbsolutePath switch
            {
                "/repos/muzudho/KifuwarabeGo2026/releases/latest" => new StringContent(JsonSerializer.Serialize(new
                {
                    tag_name = "v" + version,
                    assets = new[]
                    {
                        new { name = asset, browser_download_url = "https://fixture.invalid/package.zip" },
                        new { name = asset + ".sha256", browser_download_url = "https://fixture.invalid/package.sha256" },
                    },
                })),
                "/package.zip" => new ByteArrayContent(zip),
                "/package.sha256" => new StringContent((BadChecksum ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(zip))) + "  " + asset),
                _ => throw new Exception("Unexpected network request: " + request.RequestUri),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = body });
        }
    }
}
