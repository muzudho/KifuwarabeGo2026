using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.PlayRoomEngine.JsonLines;

var root = FindRepositoryRoot();
var goHostDirectory = Path.Combine(root, "KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost", "bin", "Release", "net8.0");
var ponnukiHostDirectory = Path.Combine(root, "KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost", "bin", "Release", "net8.0");
Require(File.Exists(Path.Combine(goHostDirectory, "KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.dll")),
    $"Go Play Room Engine host must be built: {goHostDirectory}");
Require(File.Exists(Path.Combine(ponnukiHostDirectory, "KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.dll")),
    $"Ponnuki Play Room Engine host must be built: {ponnukiHostDirectory}");
RequireNoConcreteDependencies(Path.Combine(root, "KifuwarabeGo2026.PlayRoomEngine.JsonLines", "bin", "Release", "net8.0",
    "KifuwarabeGo2026.PlayRoomEngine.JsonLines.dll"), "transport client");
RequireNoConcreteDependencies(Path.Combine(root, "KifuwarabeGo2026.GameOasis.Concierge", "bin", "Release", "net8.0",
    "KifuwarabeGo2026.GameOasis.Concierge.dll"), "Concierge");

var goManifest = PlayRoomEngineHostManifest.Load(Path.Combine(goHostDirectory, "go.playspace.json"));
Require(goManifest.SupportsMultipleSessions, "The Go manifest must advertise multi-session mode.");
using (var go = new JsonLinesPlayRoomEngineProtocol(goManifest.CreateStartInfo(goHostDirectory)))
{
    var descriptor = Success(await go.DescribeAsync());
    Require(descriptor.TypeId.Value == GameOasisOfficialNames.Go, "Go must be selected through the host argument.");
    var schema = Success(await go.GetConfigurationSchemaAsync());
    Require(schema.SchemaId == GameOasisOfficialNames.Go + ".configuration.v1", "Go schema must cross JSON Lines.");
    var configuration = Document(GameOasisOfficialNames.Go + ".configuration.v1",
        """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}""");
    Require(Success(await go.ValidateConfigurationAsync(new(configuration))).IsValid, "Go configuration must validate remotely.");
    var first = Success(await go.CreateSessionAsync(new(configuration)));
    var second = Success(await go.CreateSessionAsync(new(configuration)));
    Require(first.SessionId != second.SessionId, "The default host must support multiple sessions in one process.");
    var applied = Success(await go.ApplyActionAsync(new(first.SessionId,
        Document(GameOasisOfficialNames.Go + ".action.v1", """{"version":1,"type":"pass","player":"black"}"""), 0)));
    Require(applied.IsAccepted && applied.Snapshot.Revision == 1, "A Go action must be applied by the remote PlaySpace.");
    var snapshot = Success(await go.GetSnapshotAsync(new(first.SessionId)));
    Require(snapshot.Revision == 1 && snapshot.State.Content == applied.Snapshot.State.Content,
        "The remote PlaySpace must remain the authoritative state owner.");
    _ = Success(await go.CloseSessionAsync(new(first.SessionId)));
    _ = Success(await go.CloseSessionAsync(new(second.SessionId)));
}

var ponnukiManifest = PlayRoomEngineHostManifest.Load(Path.Combine(ponnukiHostDirectory, "ponnuki.playspace.json"));
Require(!ponnukiManifest.SupportsMultipleSessions, "The Ponnuki manifest must advertise one-session mode.");
using (var ponnuki = new JsonLinesPlayRoomEngineProtocol(ponnukiManifest.CreateStartInfo(ponnukiHostDirectory)))
{
    var descriptor = Success(await ponnuki.DescribeAsync());
    Require(descriptor.TypeId.Value == GameOasisOfficialNames.Ponnuki, "Ponnuki must be selected through the host argument.");
    var configuration = Document(GameOasisOfficialNames.Ponnuki + ".configuration.v1",
        """{"version":1,"boardSize":9,"initialMoveCount":0,"randomSeed":7,"captureTarget":1,"startingPlayer":"black","setupStones":[{"x":0,"y":0,"color":"black"},{"x":2,"y":0,"color":"black"},{"x":1,"y":0,"color":"white"}]}""");
    Require(Success(await ponnuki.ValidateConfigurationAsync(new(configuration))).IsValid,
        "Ponnuki configuration must validate remotely.");
    var session = Success(await ponnuki.CreateSessionAsync(new(configuration)));
    var busy = await ponnuki.CreateSessionAsync(new(configuration));
    Require(!busy.IsSuccess && busy.Error?.Code == "single-session-busy",
        "A one-session host must reject a second active session explicitly.");
    var capture = Success(await ponnuki.ApplyActionAsync(new(session.SessionId,
        Document(GameOasisOfficialNames.Ponnuki + ".action.v1", """{"version":1,"type":"play","player":"black","x":1,"y":1}"""), 0)));
    Require(capture.Snapshot.IsTerminal && capture.Snapshot.Outcome is not null,
        "Ponnuki must reach its terminal state in the separate process.");
    _ = Success(await ponnuki.CloseSessionAsync(new(session.SessionId)));
    var replacement = Success(await ponnuki.CreateSessionAsync(new(configuration)));
    _ = Success(await ponnuki.CloseSessionAsync(new(replacement.SessionId)));
}

var abnormalStart = goManifest.CreateStartInfo(goHostDirectory);
abnormalStart.ArgumentList.Add("--exit-after-describe");
using (var abnormal = new JsonLinesPlayRoomEngineProtocol(abnormalStart))
{
    _ = Success(await abnormal.DescribeAsync());
    var detected = false;
    try { _ = await abnormal.GetConfigurationSchemaAsync(); }
    catch (IOException) { detected = true; }
    Require(detected, "A caller must detect an abnormal PlaySpace exit.");
}

Console.WriteLine("PASS: Protocol S JSON Lines hosts passed Go multi-session, Ponnuki single-session, lifecycle, and abnormal-exit scenarios.");

static ContractDocument Document(string schemaId, string content) => new("application/json", schemaId, content);

static T Success<T>(ProtocolResponse<T> response)
{
    if (!response.IsSuccess || response.Value is null)
        throw new InvalidOperationException($"Expected success: {response.Error?.Code} {response.Error?.Message}");
    return response.Value;
}

static void RequireNoConcreteDependencies(string assemblyPath, string name)
{
    var references = System.Reflection.Assembly.LoadFile(assemblyPath).GetReferencedAssemblies();
    Require(!references.Any(reference => reference.Name?.Contains("Reference.PlaySpace", StringComparison.Ordinal) == true),
        $"The {name} must not reference a concrete PlaySpace assembly.");
}

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
