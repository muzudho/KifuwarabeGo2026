using System.IO.Compression;
using KifuwarabeGo2026.Launcher;
using KifuwarabeGo2026.Launcher.Platform;

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

    var platform = new DesktopPlatformServices();
    Require(!string.IsNullOrWhiteSpace(platform.LocalApplicationData), "OS application-data path");
    Require(platform.IsProcessRunningFrom(AppContext.BaseDirectory), "running process detection");

    var boundaryRoot = Path.Combine(root, "engine-boundary");
    var boundaryPaths = new LauncherPaths(boundaryRoot);
    var boundaryStore = new LauncherSettingsStore(boundaryPaths);
    var boundarySettings = new LauncherSettings();
    boundarySettings.Promote(LauncherProduct.Gui, "4.9.0");
    boundarySettings.Promote(LauncherProduct.Gui, "5.0.0");
    boundaryStore.Save(boundarySettings);
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

    var boundaryVersions = engineBoundary.GetInstalledVersions();
    var removable = boundaryVersions.Single(version => version.Version == "v4.8.0");
    engineBoundary.Uninstall(removable);
    Require(!Directory.Exists(removable.DirectoryPath), "engine boundary uninstall");
    var protectedVersion = boundaryVersions.Single(version => version.IsCurrent);
    var protectedRejected = false;
    try { engineBoundary.Uninstall(protectedVersion); }
    catch (InvalidOperationException) { protectedRejected = true; }
    Require(protectedRejected, "engine boundary protected uninstall rejection");

    var launch = engineBoundary.StartGui();
    Require(launch.Success && launch.UsedPrevious, "engine boundary previous-version fallback");
    Require(fakePlatform.StartedExecutables.Count == 2, "engine boundary launch attempts");

    var changedInstallation = Path.Combine(root, "changed-installation");
    boundaryState = engineBoundary.ChangeInstallationDirectory(changedInstallation);
    Require(boundaryState.InstallationRoot == Path.GetFullPath(changedInstallation), "engine boundary installation setting change");

    Console.WriteLine("PASS: launcher core, platform, and in-process engine boundary checks.");
    return 0;
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void Require(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + name);
}

static void CreateExecutable(LauncherPaths paths, LauncherProduct product, string version)
{
    var directory = paths.VersionDirectory(product, version);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, product.ExecutableName()), "test executable");
}

sealed class FakePlatformServices(string localApplicationData) : IPlatformServices
{
    public Func<string, bool> StartBehavior { get; init; } = _ => true;
    public List<string> StartedExecutables { get; } = [];
    public string LocalApplicationData { get; } = Path.GetFullPath(localApplicationData);

    public bool Start(string executable, string workingDirectory)
    {
        StartedExecutables.Add(executable);
        return StartBehavior(executable);
    }

    public bool OpenFolder(string directory) => false;
    public bool OpenFile(string filePath) => false;
    public string? SelectFolder(string title, string initialDirectory) => null;
    public bool IsProcessRunningFrom(string directory) => false;
}

sealed class RejectNetworkHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Network access is not expected in this test.");
}
