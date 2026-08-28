using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;
using KifuwarabeGo2026.LobbyEngine;
using KifuwarabeGo2026.LobbyEngine.JsonLines;
using System.Diagnostics;

var documents = new MemoryDocumentStore();
var paths = new TestCatalogPaths();
var connections = new MemoryCgosConnectionStore([
    new CgosConnectionProfile("Practice", "example.test", 6809, "1", "Test")
    {
        Id = "practice",
    },
]);
var defaults = new[]
{
    new GtpEngineProfile
    {
        Id = "engine-1",
        DisplayName = "Test Engine",
        ExecutablePath = "engine.exe",
        DefaultCgosLoginName = "test-login",
    },
};

var engine = new InProcessLobbyEngine(documents, paths, connections, defaults, "/release");
var state = engine.LoadState();
Require(state.GtpEngines.Count == 1 && state.GtpEngines[0].Id == "engine-1", "Release default engine must load through ILobbyEngine.");
Require(state.Entries.Any(entry => entry.Kind == EntryProfileKind.Computer && entry.EngineProfileId == "engine-1"), "A computer entry must be composed from the engine catalog.");
Require(state.ClientIdentities.Count > 0, "Client identities must be composed without GUI participation.");
Require(state.CgosConnections.Count == 1 && state.CgosConnections[0].Id == "practice", "CGOS connections must load through the settings adapter.");
Require(state.GtpEngineListPath == paths.GtpEngineListPath, "The GUI must receive the engine settings path as display state.");

var repositoryRoot = FindRepositoryRoot();
var hostPath = Path.Combine(repositoryRoot, "KifuwarabeGo2026.LobbyEngine.JsonLinesHost", "bin", "Release", "net8.0",
    OperatingSystem.IsWindows() ? "KifuwarabeGo2026.LobbyEngine.JsonLinesHost.exe" : "KifuwarabeGo2026.LobbyEngine.JsonLinesHost");
Require(File.Exists(hostPath), $"Lobby JSON Lines host must be built: {hostPath}");
var protocolTestDirectory = Path.Combine(Path.GetTempPath(), "kifuwarabe-lobby-jsonlines-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(protocolTestDirectory);
try
{
    var remoteEntryPath = Path.Combine(protocolTestDirectory, "player-list.json");
    File.WriteAllText(remoteEntryPath,
        """{"players":[{"id":"remote-entry","displayName":"Remote Player","identifier":"remote","kind":"human","engineProfileId":"","targetProfileIds":[]}]}""");
    var remoteEngine = new JsonLinesLobbyEngine(
        () => new ProcessStartInfo(hostPath) { ArgumentList = { "--entry-list", remoteEntryPath } },
        engine,
        TimeSpan.FromSeconds(5));
    var remoteState = remoteEngine.LoadState();
    Require(remoteState.Entries.Count == 1 && remoteState.Entries[0].Id == "remote-entry" && remoteEngine.CommunicationWarning is null,
        "Registered entries must make a real JSON Lines round trip through the lobby child process.");

    var failedEngine = new JsonLinesLobbyEngine(
        () => new ProcessStartInfo(Path.Combine(protocolTestDirectory, "missing-host.exe")), engine, TimeSpan.FromMilliseconds(200));
    var recoveredState = failedEngine.LoadState();
    Require(recoveredState.Entries.Count == state.Entries.Count && failedEngine.CommunicationWarning is not null,
        "A lobby host failure must recover through the in-process engine without terminating the GUI path.");
}
finally
{
    Directory.Delete(protocolTestDirectory, recursive: true);
}

var changedEngines = state.GtpEngines.Select(profile => profile.Clone()).ToArray();
changedEngines[0].DisplayName = "Changed Engine";
engine.SaveGtpEngines(changedEngines);
var changedEntries = state.Entries.Select(profile => profile.Clone()).ToArray();
changedEntries[0].DisplayName = "Changed Player";
var changedTargets = state.ClientIdentities.Select(profile => profile.Clone()).ToArray();
changedTargets[0].DisplayName = "Changed Identity";
engine.SaveEntriesAndClientIdentities(changedEntries, changedTargets);
engine.SaveCgosConnections([
    new CgosConnectionProfile("Local", "localhost", 6810, "2", "Changed") { Id = "local" },
]);

var reloaded = new InProcessLobbyEngine(documents, paths, connections, defaults, "/release").LoadState();
Require(reloaded.GtpEngines[0].DisplayName == "Changed Engine", "Engine changes must survive a new lobby engine instance.");
Require(reloaded.Entries.Any(entry => entry.DisplayName == "Changed Player"), "Entry changes must survive a new lobby engine instance.");
Require(reloaded.ClientIdentities.Any(target => target.DisplayName == "Changed Identity"), "Client identity changes must survive a new lobby engine instance.");
Require(reloaded.CgosConnections.Count == 1 && reloaded.CgosConnections[0].Id == "local", "CGOS changes must survive a new lobby engine instance.");

var unloaded = new InProcessLobbyEngine(new MemoryDocumentStore(), paths, connections, defaults, "/release");
var rejectedBeforeLoad = false;
try { unloaded.SaveGtpEngines(defaults); }
catch (InvalidOperationException) { rejectedBeforeLoad = true; }
Require(rejectedBeforeLoad, "Save before LoadState must be rejected deterministically.");

Console.WriteLine("PASS: ILobbyEngine loaded and saved lobby catalogs without GUI or MonoGame dependencies.");

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx"))) return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Repository root was not found.");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

file sealed class MemoryDocumentStore : ICatalogDocumentStore
{
    private readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal);
    public bool Exists(string path) => _documents.ContainsKey(path);
    public string ReadAllText(string path) => _documents[path];
    public void WriteAllText(string path, string content) => _documents[path] = content;
}

file sealed class TestCatalogPaths : ICatalogPathProvider
{
    public string GtpEngineListPath => "/data/gtp-engines.json";
    public string EntryListPath => "/data/entries.json";
    public string ClientIdentityListPath => "/data/client-identities.json";
    public string? FindDevelopmentGtpEngineListPath() => null;
}

file sealed class MemoryCgosConnectionStore(IReadOnlyList<CgosConnectionProfile> profiles) : ICgosConnectionProfileStore
{
    private IReadOnlyList<CgosConnectionProfile> _profiles = profiles;
    public string ListPath => "/data/application-settings.json";
    public IReadOnlyList<CgosConnectionProfile> Load() => _profiles;
    public void Save(IEnumerable<CgosConnectionProfile> values) => _profiles = values.ToArray();
}
