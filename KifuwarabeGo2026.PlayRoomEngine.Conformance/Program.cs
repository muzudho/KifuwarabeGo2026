using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.PlayRoomEngine.JsonLines;
using System.Text.Json;

var cases = ReadOption(args, "--manifest") is { } manifest && ReadOption(args, "--vector") is { } vector
    ? [(manifest, vector)]
    : DefaultCases(FindRepositoryRoot());
foreach (var testCase in cases) await RunAsync(testCase.Item1, testCase.Item2);
Console.WriteLine($"PASS: {cases.Length} Protocol S JSON Lines conformance case(s).");

static async Task RunAsync(string manifestPath, string vectorPath)
{
    var manifest = PlayRoomEngineHostManifest.Load(manifestPath);
    var vector = JsonSerializer.Deserialize<ConformanceVector>(File.ReadAllText(vectorPath), PlayRoomEngineJsonLinesProtocol.JsonOptions)
        ?? throw new InvalidDataException($"Cannot read vector: {vectorPath}");
    Require(vector.Version == 1, "Unsupported conformance vector version.");
    Require(manifest.PlaySpaceTypeId == vector.ExpectedTypeId, "Manifest and vector type IDs must match.");
    using var protocol = new JsonLinesPlayRoomEngineProtocol(manifest.CreateStartInfo(Path.GetDirectoryName(manifestPath)!));
    var descriptor = Success(await protocol.DescribeAsync());
    Require(descriptor.TypeId.Value == vector.ExpectedTypeId, "Descriptor type ID did not match.");
    Require(descriptor.ProtocolVersion == ContractVersion.V1_0, "Protocol S 1.0 is required.");
    var schema = Success(await protocol.GetConfigurationSchemaAsync());
    Require(schema.MediaType == "application/json" && !string.IsNullOrWhiteSpace(schema.SchemaId), "Configuration schema must be self-describing JSON.");
    var validation = Success(await protocol.ValidateConfigurationAsync(new(vector.Configuration)));
    Require(validation.IsValid, "The valid test configuration was rejected.");
    if (vector.InvalidConfiguration is { } invalid)
        Require(!Success(await protocol.ValidateConfigurationAsync(new(invalid))).IsValid, "The invalid test configuration was accepted.");
    var created = Success(await protocol.CreateSessionAsync(new(vector.Configuration)));
    Require(created.InitialSnapshot.Revision == vector.ExpectedInitialRevision, "Unexpected initial revision.");
    var before = Success(await protocol.GetSnapshotAsync(new(created.SessionId)));
    Require(before.State.Content == created.InitialSnapshot.State.Content, "Initial state was not stable.");
    var applied = Success(await protocol.ApplyActionAsync(new(created.SessionId, vector.Action, before.Revision)));
    Require(applied.IsAccepted && applied.Snapshot.Revision == vector.ExpectedAppliedRevision, "Action was not applied at the expected revision.");
    Require(applied.Snapshot.IsTerminal == vector.ExpectedTerminal, "Terminal flag did not match the vector.");
    var stale = await protocol.ApplyActionAsync(new(created.SessionId, vector.Action, before.Revision));
    Require(!stale.IsSuccess && stale.Error?.Code == "revision-conflict", "A stale revision must fail.");
    _ = Success(await protocol.CloseSessionAsync(new(created.SessionId)));
    var missing = await protocol.GetSnapshotAsync(new(created.SessionId));
    Require(!missing.IsSuccess && missing.Error?.Code == "session-not-found", "A closed session must not remain available.");
    Console.WriteLine($"PASS: {vector.Name} ({vector.ExpectedTypeId})");
}

static (string, string)[] DefaultCases(string root)
{
    var goOfficial = Path.Combine(root, "KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost", "bin", "Release", "net8.0");
    var ponnukiOfficial = Path.Combine(root, "KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost", "bin", "Release", "net8.0");
    var external = Path.Combine(root, "Samples", "External.PlayRoomEngine.Counter", "bin", "Release", "net8.0");
    var vectors = Path.Combine(root, "Conformance", "ProtocolS", "v1");
    return [
        (Path.Combine(goOfficial, "go.playspace.json"), Path.Combine(vectors, "go.json")),
        (Path.Combine(ponnukiOfficial, "ponnuki.playspace.json"), Path.Combine(vectors, "ponnuki.json")),
        (Path.Combine(external, "counter.playspace.json"), Path.Combine(vectors, "external-counter.json")),
    ];
}

static T Success<T>(ProtocolResponse<T> response) => response.IsSuccess && response.Value is { } value
    ? value
    : throw new InvalidOperationException($"Expected success: {response.Error?.Code} {response.Error?.Message}");
static string? ReadOption(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx"))) return directory.FullName;
    throw new DirectoryNotFoundException("Repository root was not found.");
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

sealed record ConformanceVector(int Version, string Name, string ExpectedTypeId, ContractDocument Configuration,
    ContractDocument? InvalidConfiguration, ContractDocument Action, long ExpectedInitialRevision,
    long ExpectedAppliedRevision, bool ExpectedTerminal);
