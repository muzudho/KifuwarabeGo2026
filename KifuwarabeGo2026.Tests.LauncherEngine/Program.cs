using System.IO.Compression;
using KifuwarabeGo2026.LauncherEngine;
using KifuwarabeGo2026.LauncherEngine.JsonLines;
using KifuwarabeGo2026.LauncherEngine.Platform;
using System.Diagnostics;

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

var root = Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026-LauncherSmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var paths = new LauncherPaths(root);
    var store = new LauncherSettingsStore(paths);
    var settings = new LauncherSettings();
    settings.Promote(LauncherProduct.Gui, "3.13.0");
    settings.Promote(LauncherProduct.Gui, "v3.14.0");
    store.Save(settings);
    var loaded = store.Load();
    Require(loaded.GuiCurrentVersion == "3.14.0" && loaded.GuiPreviousVersion == "3.13.0", "current/previous setting");
    Require(!File.Exists(paths.SettingsFile + ".tmp"), "atomic settings temporary cleanup");
    var customInstall = Path.Combine(root, "custom-install");
    var customPaths = new LauncherPaths(root, customInstall);
    Require(customPaths.InstallationRoot == Path.GetFullPath(customInstall), "custom installation root");
    Require(customPaths.ProductRoot(LauncherProduct.Gui).StartsWith(Path.GetFullPath(customInstall), StringComparison.OrdinalIgnoreCase), "GUI uses custom installation root");
    Require(customPaths.SettingsFile == paths.SettingsFile, "settings remain in application-data root");
    Require(LauncherProduct.Gui.AssetName("v3.14.0") == "KifuwarabeGo2026.Gui-v3.14.0-win-x64.zip", "GUI exact asset name");
    Require(LauncherProduct.Gui.ExecutableName() == "KifuwarabeGo2026.GameOasis.Gui.Windows.exe", "GUI executable name");
    Require(LauncherProduct.Engine.AssetName("3.14.0") == "KifuwarabeGo2026.Engine-v3.14.0-win-x64.zip", "Engine exact asset name");

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

    var platform = new DesktopLauncherEnginePlatform();
    Require(!string.IsNullOrWhiteSpace(platform.LocalApplicationData), "OS application-data path");
    Require(platform.IsProcessRunningFrom(AppContext.BaseDirectory), "running process detection");

    var boundaryRoot = Path.Combine(root, "engine-boundary");
    var boundaryPaths = new LauncherPaths(boundaryRoot);
    var boundaryStore = new LauncherSettingsStore(boundaryPaths);
    var boundarySettings = new LauncherSettings();
    boundarySettings.Promote(LauncherProduct.Gui, "4.9.0");
    boundarySettings.Promote(LauncherProduct.Gui, "5.0.0");
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
    CreateExecutable(boundaryPaths, LauncherProduct.Gui, "4.9.0");
    CreateExecutable(boundaryPaths, LauncherProduct.Gui, "5.0.0");
    Directory.CreateDirectory(boundaryPaths.VersionDirectory(LauncherProduct.Gui, "4.8.0"));

    var fakePlatform = new FakePlatformServices(boundaryRoot)
    {
        StartBehavior = executable => executable.Contains("v4.9.0", StringComparison.OrdinalIgnoreCase),
    };
    using var boundaryHttpClient = new HttpClient(new RejectNetworkHandler());
    ILauncherEngine engineBoundary = new InProcessLauncherEngine(fakePlatform, boundaryHttpClient);
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

    var changedInstallation = Path.Combine(root, "changed-installation");
    var installationChange = engineBoundary.ChangeInstallationDirectory(changedInstallation);
    Require(installationChange.IsSuccess && installationChange.Value?.InstallationRoot == Path.GetFullPath(changedInstallation), "engine boundary installation setting change");
    Require(!engineBoundary.StartGui().IsSuccess, "engine boundary launch failure result");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var canceledUpdate = await engineBoundary.UpdateAsync(LauncherProduct.Gui, cancellationToken: canceled.Token);
    Require(canceledUpdate.IsCanceled, "engine boundary update cancellation result");
    var failedUpdate = await engineBoundary.UpdateAsync(LauncherProduct.Gui);
    Require(!failedUpdate.IsSuccess && !failedUpdate.IsCanceled, "engine boundary update failure result");

    var hostDll = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "KifuwarabeGo2026.LauncherEngine.JsonLinesHost", "bin", "Release", "net8.0",
        "KifuwarabeGo2026.LauncherEngine.JsonLinesHost.dll"));
    Require(File.Exists(hostDll), "JSON Lines host build output");
    var hostStartInfo = new ProcessStartInfo("dotnet");
    hostStartInfo.ArgumentList.Add(hostDll);
    hostStartInfo.ArgumentList.Add("--local-application-data");
    hostStartInfo.ArgumentList.Add(boundaryRoot);
    hostStartInfo.ArgumentList.Add("--my-pictures");
    hostStartInfo.ArgumentList.Add(Path.Combine(boundaryRoot, "Pictures"));
    using (var jsonLinesEngine = new JsonLinesLauncherEngine(hostStartInfo, engineBoundary))
    {
        var remoteState = jsonLinesEngine.GetState();
        Require(remoteState.InstallationRoot == engineBoundary.GetState().InstallationRoot, "JSON Lines state round trip");
        Require(remoteState.CloseAfterStartingGui == engineBoundary.GetState().CloseAfterStartingGui, "JSON Lines state values");
        var remoteVersions = jsonLinesEngine.GetInstalledVersions();
        Require(remoteVersions.Count == engineBoundary.GetInstalledVersions().Count, "JSON Lines version list round trip");
        Require(jsonLinesEngine.GetCurrentDirectory(LauncherProduct.Engine) is null, "JSON Lines current directory round trip");

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

static JsonLinesLauncherEngine CreateFakeJsonLinesEngine(
    string testAssembly,
    string behavior,
    ILauncherEngine fallback,
    TimeSpan? timeout = null)
{
    var startInfo = new ProcessStartInfo("dotnet");
    startInfo.ArgumentList.Add(testAssembly);
    startInfo.ArgumentList.Add("--fake-json-lines-host");
    startInfo.ArgumentList.Add(behavior);
    return new JsonLinesLauncherEngine(startInfo, fallback, timeout);
}

static void RequireCommunicationFallback(JsonLinesLauncherEngine engine, ILauncherEngine fallback, string name)
{
    using (engine)
    {
        var state = engine.GetState();
        Require(state == fallback.GetState(), name + " state");
        Require(!string.IsNullOrWhiteSpace(engine.CommunicationWarning), name + " warning");
    }
}

static void CreateExecutable(LauncherPaths paths, LauncherProduct product, string version)
{
    var directory = paths.VersionDirectory(product, version);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, product.ExecutableName()), "test executable");
}

sealed class FakePlatformServices(string localApplicationData) : ILauncherEnginePlatform
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
