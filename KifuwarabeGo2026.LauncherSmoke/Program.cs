using System.IO.Compression;
using KifuwarabeGo2026.Launcher;

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
    Require(LauncherProduct.Gui.AssetName("v3.14.0") == "KifuwarabeGo2026.Gui-v3.14.0-win-x64.zip", "GUI exact asset name");
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

    Console.WriteLine("PASS: launcher settings, atomic save, safe extraction, and ZIP Slip checks.");
    return 0;
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void Require(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + name);
}
