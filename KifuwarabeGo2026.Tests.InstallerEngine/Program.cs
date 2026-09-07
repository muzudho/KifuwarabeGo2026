using System.IO.Compression;
using KifuwarabeGo2026.InstallerEngine;
using KifuwarabeGo2026.InstallerEngine.JsonLines;
using KifuwarabeGo2026.InstallerEngine.Platform;
using System.Diagnostics;

if (Environment.GetEnvironmentVariable("KIFUWARABE_TEST_LOBBY_MARKER") is { } marker)
{
    File.WriteAllLines(marker, [Environment.GetEnvironmentVariable("KIFUWARABE_INSTALLER_PATH") ?? "",
        Environment.GetEnvironmentVariable("KIFUWARABE_LAUNCHER_PATH") ?? ""]);
    return 0;
}
if (args.FirstOrDefault() == "--installer-entry-smoke")
{
    InstallerEntryChecks.Run(args[1]);
    return 0;
}

if (args.FirstOrDefault() == "--fake-json-lines-host")
{
    _ = Console.ReadLine();
    switch (args.ElementAtOrDefault(1))
    {
        case "invalid-json":
            Console.WriteLine("this is not json");
            break;
        case "timeout":
            await Task.Delay(TimeSpan.FromSeconds(30));
            break;
        case "exit":
            break;
    }
    return 0;
}

var root = Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026-InstallerSmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var paths = new InstallerPaths(root);
    var store = new InstallerSettingsStore(paths);
    var settings = new InstallerSettings();
    settings.Promote(InstallerProduct.Gui, "3.13.0");
    settings.Promote(InstallerProduct.Gui, "v3.14.0");
    store.Save(settings);
    var loaded = store.Load();
    Require(loaded.GuiCurrentVersion == "3.14.0" && loaded.GuiPreviousVersion == "3.13.0", "current/previous setting");
    Require(!File.Exists(paths.SettingsFile + ".tmp"), "atomic settings temporary cleanup");
    var customInstall = Path.Combine(root, "custom-install");
    var customPaths = new InstallerPaths(root, customInstall);
    Require(customPaths.InstallationRoot == Path.GetFullPath(customInstall), "custom installation root");
    Require(customPaths.ProductRoot(InstallerProduct.Gui).StartsWith(Path.GetFullPath(customInstall), StringComparison.OrdinalIgnoreCase), "GUI uses custom installation root");
    Require(customPaths.SettingsFile == paths.SettingsFile, "settings remain in application-data root");
    Require(InstallerProduct.Gui.AssetName("v3.14.0") == "KifuwarabeGo2026.Gui-v3.14.0-win-x64.zip", "GUI exact asset name");
    Require(InstallerProduct.Gui.ExecutableName() == "KifuwarabeGo2026.GameOasis.Gui.Windows.exe", "GUI executable name");
    Require(InstallerProduct.Engine.AssetName("3.14.0") == "KifuwarabeGo2026.Engine-v3.14.0-win-x64.zip", "Engine exact asset name");

    var goodZip = Path.Combine(root, "good.zip");
    using (var archive = ZipFile.Open(goodZip, ZipArchiveMode.Create)) archive.CreateEntry("folder/file.txt");
    var extract = Path.Combine(root, "extract");
    Directory.CreateDirectory(extract);
    PackageInstaller.ExtractSafely(goodZip, extract);
    Require(File.Exists(Path.Combine(extract, "folder", "file.txt")), "safe ZIP extraction");

    var badZip = Path.Combine(root, "bad.zip");
    using (var archive = ZipFile.Open(badZip, ZipArchiveMode.Create)) archive.CreateEntry("../outside.txt");
    var rejected = false;
    try { PackageInstaller.ExtractSafely(badZip, extract); }
    catch (InvalidDataException) { rejected = true; }
    Require(rejected && !File.Exists(Path.Combine(root, "outside.txt")), "ZIP Slip rejection");

    var hashFile = Path.Combine(root, "hash.txt");
    File.WriteAllText(hashFile, "kifuwarabe");
    PackageInstaller.VerifySha256(hashFile, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(hashFile))));
    var hashRejected = false;
    try { PackageInstaller.VerifySha256(hashFile, new string('0', 64)); }
    catch (InvalidDataException) { hashRejected = true; }
    Require(hashRejected, "SHA-256 mismatch rejection");

    var platform = new DesktopInstallerEnginePlatform();
    Require(!string.IsNullOrWhiteSpace(platform.LocalApplicationData), "OS application-data path");
    Require(platform.IsProcessRunningFrom(AppContext.BaseDirectory), "running process detection");

    var boundaryRoot = Path.Combine(root, "engine-boundary");
    var boundaryPaths = new InstallerPaths(boundaryRoot);
    var boundaryStore = new InstallerSettingsStore(boundaryPaths);
    var boundarySettings = new InstallerSettings();
    boundarySettings.Promote(InstallerProduct.Gui, "4.9.0");
    boundarySettings.Promote(InstallerProduct.Gui, "5.0.0");
    boundaryStore.Save(boundarySettings);
    var legacyScreenshotDirectory = Path.Combine(root, "legacy-screenshots");
    Directory.CreateDirectory(Path.GetDirectoryName(boundaryPaths.Root)!);
    var sharedSettingsFile = Path.Combine(boundaryPaths.Root, "application-settings.json");
    File.WriteAllText(sharedSettingsFile,
        $$"""
        {
          "LogRootDirectory": "preserve-this-value",
          "ScreenshotSaveDirectory": "{{legacyScreenshotDirectory.Replace("\\", "\\\\")}}",
          "CloseLauncherAfterStartingGui": false
        }
        """);
    CreateExecutable(boundaryPaths, InstallerProduct.Gui, "4.9.0");
    CreateExecutable(boundaryPaths, InstallerProduct.Gui, "5.0.0");
    Directory.CreateDirectory(boundaryPaths.VersionDirectory(InstallerProduct.Gui, "4.8.0"));

    var fakePlatform = new FakePlatformServices(boundaryRoot)
    {
        StartBehavior = executable => executable.Contains("v4.9.0", StringComparison.OrdinalIgnoreCase),
    };
    using var boundaryHttpClient = new HttpClient(new RejectNetworkHandler());
    IInstallerEngine engineBoundary = new InProcessInstallerEngine(fakePlatform, boundaryHttpClient);
    var boundaryState = engineBoundary.GetState();
    Require(boundaryState.GuiCurrentVersion == "5.0.0", "engine boundary state");
    Require(boundaryState.InstallationRoot == boundaryPaths.InstallationRoot, "engine boundary installation root");
    Require(boundaryState.ScreenshotSaveDirectory == Path.GetFullPath(legacyScreenshotDirectory), "shared GUI screenshot setting");
    Require(!boundaryState.CloseAfterStartingGui, "legacy launcher setting migration");
    Require(boundaryStore.Load().CloseLauncherAfterStartingGui == false, "launcher setting migrated to launcher settings");

    var changedScreenshotDirectory = Path.Combine(root, "changed-screenshots");
    var screenshotChange = engineBoundary.ChangeScreenshotDirectory(changedScreenshotDirectory);
    Require(screenshotChange.IsSuccess && screenshotChange.Value?.ScreenshotSaveDirectory == Path.GetFullPath(changedScreenshotDirectory), "shared GUI setting change");
    Require(File.ReadAllText(sharedSettingsFile).Contains("preserve-this-value", StringComparison.Ordinal), "shared GUI unknown setting preservation");
    var invalidScreenshotChange = engineBoundary.ChangeScreenshotDirectory(" ");
    Require(!invalidScreenshotChange.IsSuccess, "shared GUI setting failure result");

    var closeSettingChange = engineBoundary.ChangeCloseAfterStartingGui(true);
    Require(closeSettingChange.IsSuccess && closeSettingChange.Value?.CloseAfterStartingGui == true, "launcher-only setting change");
    Require(boundaryStore.Load().CloseLauncherAfterStartingGui == true, "launcher-only setting persistence");

    var boundaryVersions = engineBoundary.GetInstalledVersions();
    var removable = boundaryVersions.Single(version => version.Version == "v4.8.0");
    var uninstall = engineBoundary.Uninstall(removable);
    Require(uninstall.IsSuccess && !Directory.Exists(removable.DirectoryPath), "engine boundary uninstall");
    var protectedVersion = boundaryVersions.Single(version => version.IsCurrent);
    var protectedResult = engineBoundary.Uninstall(protectedVersion);
    Require(!protectedResult.IsSuccess, "engine boundary protected uninstall rejection");

    var launch = engineBoundary.StartGui();
    Require(launch.IsSuccess && launch.Value?.UsedPrevious == true, "engine boundary previous-version fallback");
    Require(fakePlatform.StartedExecutables.Count == 2, "engine boundary launch attempts");

    // Launch-only processes must never enter a partially updated package directory.
    using (var lease = InstallerOperationLease.Acquire(boundaryPaths))
    {
        var busyLaunch = engineBoundary.StartGui();
        Require(!busyLaunch.IsSuccess && !string.IsNullOrWhiteSpace(busyLaunch.Message), "busy installer exposes a recovery message");
        Require(fakePlatform.StartedExecutables.Count == 2, "busy launch did not start a process");
        Require(!engineBoundary.ChangeInstallationDirectory(Path.Combine(root, "busy-change")).IsSuccess, "concurrent settings mutation rejected");
    }
    Require(engineBoundary.StartGui().IsSuccess, "launch resumes after update lease is released");
    Require(Path.GetFileName(boundaryPaths.SettingsFile) == "launcher-settings.json", "legacy settings path preserved");
    Require(File.ReadAllText(boundaryPaths.SettingsFile).Contains("CloseLauncherAfterStartingGui"), "legacy JSON key preserved");

    var updatedRoot = Path.Combine(root, "updated-gui");
    var updatedPaths = new InstallerPaths(updatedRoot);
    var updatedSettings = new InstallerSettings();
    var updatedPlatform = new FakePlatformServices(updatedRoot);
    IInstallerEngine updatedEngine = new InProcessInstallerEngine(updatedPlatform, boundaryHttpClient);
    foreach (var version in new[] { "1.0.0", "2.0.0", "3.0.0" })
    {
        CreateExecutable(updatedPaths, InstallerProduct.Gui, version);
        updatedSettings.Promote(InstallerProduct.Gui, version);
    }
    new InstallerSettingsStore(updatedPaths).Save(updatedSettings);
    var obsolete = updatedEngine.GetInstalledVersions().Single(item => item.Version == "v1.0.0");
    Require(updatedEngine.Uninstall(obsolete).IsSuccess, "old GUI removal after promotion");
    Require(updatedEngine.StartGui().IsSuccess && updatedPlatform.StartedExecutables.Single().Contains("v3.0.0"),
        "offline launch uses the new current GUI after old-version removal");

    var changedInstallation = Path.Combine(root, "changed-installation");
    var installationChange = engineBoundary.ChangeInstallationDirectory(changedInstallation);
    Require(installationChange.IsSuccess && installationChange.Value?.InstallationRoot == Path.GetFullPath(changedInstallation), "engine boundary installation setting change");
    Require(!engineBoundary.StartGui().IsSuccess, "engine boundary launch failure result");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var canceledUpdate = await engineBoundary.UpdateAsync(InstallerProduct.Gui, cancellationToken: canceled.Token);
    Require(canceledUpdate.IsCanceled, "engine boundary update cancellation result");
    var failedUpdate = await engineBoundary.UpdateAsync(InstallerProduct.Gui);
    Require(!failedUpdate.IsSuccess && !failedUpdate.IsCanceled, "engine boundary update failure result");

    var hostDll = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "KifuwarabeGo2026.InstallerEngine.JsonLinesHost", "bin", "Release", "net8.0",
        "KifuwarabeGo2026.InstallerEngine.JsonLinesHost.dll"));
    Require(File.Exists(hostDll), "JSON Lines host build output");
    var hostStartInfo = new ProcessStartInfo("dotnet");
    hostStartInfo.ArgumentList.Add(hostDll);
    hostStartInfo.ArgumentList.Add("--local-application-data");
    hostStartInfo.ArgumentList.Add(boundaryRoot);
    hostStartInfo.ArgumentList.Add("--my-pictures");
    hostStartInfo.ArgumentList.Add(Path.Combine(boundaryRoot, "Pictures"));
    using (var jsonLinesEngine = new JsonLinesInstallerEngine(hostStartInfo, engineBoundary))
    {
        var remoteState = jsonLinesEngine.GetState();
        Require(remoteState.InstallationRoot == engineBoundary.GetState().InstallationRoot, "JSON Lines state round trip");
        Require(remoteState.CloseAfterStartingGui == engineBoundary.GetState().CloseAfterStartingGui, "JSON Lines state values");
        var remoteVersions = jsonLinesEngine.GetInstalledVersions();
        Require(remoteVersions.Count == engineBoundary.GetInstalledVersions().Count, "JSON Lines version list round trip");
        Require(jsonLinesEngine.GetCurrentDirectory(InstallerProduct.Engine) is null, "JSON Lines current directory round trip");

        var remoteScreenshotDirectory = Path.Combine(root, "remote-screenshots");
        var remoteScreenshotChange = jsonLinesEngine.ChangeScreenshotDirectory(remoteScreenshotDirectory);
        Require(remoteScreenshotChange.IsSuccess && remoteScreenshotChange.Value?.ScreenshotSaveDirectory == Path.GetFullPath(remoteScreenshotDirectory), "JSON Lines screenshot setting change");
        var remoteCloseChange = jsonLinesEngine.ChangeCloseAfterStartingGui(false);
        Require(remoteCloseChange.IsSuccess && remoteCloseChange.Value?.CloseAfterStartingGui == false, "JSON Lines launcher setting change");
        var remoteBusinessFailure = jsonLinesEngine.ChangeScreenshotDirectory(" ");
        Require(!remoteBusinessFailure.IsSuccess, "JSON Lines business failure result");
        Require(jsonLinesEngine.CommunicationWarning is null, "business failure is not a communication failure: " + jsonLinesEngine.CommunicationWarning);

        var remoteRemovalDirectory = Path.Combine(remoteState.InstallationRoot, "Packages", "Gui", "v6.0.0");
        Directory.CreateDirectory(remoteRemovalDirectory);
        var remoteRemovalTarget = jsonLinesEngine.GetInstalledVersions().Single(version => version.DirectoryPath == Path.GetFullPath(remoteRemovalDirectory));
        var remoteUninstall = jsonLinesEngine.Uninstall(remoteRemovalTarget);
        Require(remoteUninstall.IsSuccess && !Directory.Exists(remoteRemovalDirectory), "JSON Lines guarded uninstall");
        var repeatedUninstall = jsonLinesEngine.Uninstall(remoteRemovalTarget);
        Require(repeatedUninstall.IsSuccess, "JSON Lines uninstall retry safety");

        var remoteInstallationDirectory = Path.Combine(root, "remote-installation");
        var remoteInstallationChange = jsonLinesEngine.ChangeInstallationDirectory(remoteInstallationDirectory);
        Require(remoteInstallationChange.IsSuccess && remoteInstallationChange.Value?.InstallationRoot == Path.GetFullPath(remoteInstallationDirectory), "JSON Lines installation setting change");
        Require(engineBoundary.GetState().InstallationRoot == Path.GetFullPath(remoteInstallationDirectory), "fallback state synchronized after remote setting change");
    }

    var testAssembly = typeof(FakePlatformServices).Assembly.Location;
    RequireCommunicationFallback(CreateFakeJsonLinesEngine(testAssembly, "invalid-json", engineBoundary), engineBoundary, "JSON Lines invalid JSON recovery");
    RequireCommunicationFallback(CreateFakeJsonLinesEngine(testAssembly, "exit", engineBoundary), engineBoundary, "JSON Lines child process exit recovery");
    RequireCommunicationFallback(CreateFakeJsonLinesEngine(testAssembly, "timeout", engineBoundary, TimeSpan.FromMilliseconds(200)), engineBoundary, "JSON Lines response timeout recovery");

    Console.WriteLine("PASS: launcher core, platform, in-process boundary, and JSON Lines protocol checks.");
    return 0;
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void Require(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + name);
}

static JsonLinesInstallerEngine CreateFakeJsonLinesEngine(
    string testAssembly,
    string behavior,
    IInstallerEngine fallback,
    TimeSpan? timeout = null)
{
    var startInfo = new ProcessStartInfo("dotnet");
    startInfo.ArgumentList.Add(testAssembly);
    startInfo.ArgumentList.Add("--fake-json-lines-host");
    startInfo.ArgumentList.Add(behavior);
    return new JsonLinesInstallerEngine(startInfo, fallback, timeout);
}

static void RequireCommunicationFallback(JsonLinesInstallerEngine engine, IInstallerEngine fallback, string name)
{
    using (engine)
    {
        var state = engine.GetState();
        Require(state == fallback.GetState(), name + " state");
        Require(!string.IsNullOrWhiteSpace(engine.CommunicationWarning), name + " warning");
    }
}

static void CreateExecutable(InstallerPaths paths, InstallerProduct product, string version)
{
    var directory = paths.VersionDirectory(product, version);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, product.ExecutableName()), "test executable");
}

sealed class FakePlatformServices(string localApplicationData) : IInstallerEnginePlatform
{
    public Func<string, bool> StartBehavior { get; init; } = _ => true;
    public List<string> StartedExecutables { get; } = [];
    public string LocalApplicationData { get; } = Path.GetFullPath(localApplicationData);
    public string MyPictures { get; } = Path.Combine(Path.GetFullPath(localApplicationData), "Pictures");

    public bool Start(string executable, string workingDirectory)
    {
        StartedExecutables.Add(executable);
        return StartBehavior(executable);
    }

    public bool IsProcessRunningFrom(string directory) => false;
}

sealed class RejectNetworkHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Network access is not expected in this test.");
}
