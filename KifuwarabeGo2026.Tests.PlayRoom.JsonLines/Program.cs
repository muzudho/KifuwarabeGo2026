using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.JsonLines;
using System.Diagnostics;

var root = FindRepositoryRoot();
var hostPath = Path.Combine(root, "KifuwarabeGo2026.PlayRoom.BoardEditor.JsonLinesHost", "bin", "Release", "net8.0",
    OperatingSystem.IsWindows() ? "KifuwarabeGo2026.PlayRoom.BoardEditor.JsonLinesHost.exe" : "KifuwarabeGo2026.PlayRoom.BoardEditor.JsonLinesHost");
Require(File.Exists(hostPath), $"Board Editor host must be built: {hostPath}");
var hostAssemblyPath = Path.ChangeExtension(hostPath, ".dll");
var forbiddenReferences = System.Reflection.Assembly.LoadFile(hostAssemblyPath).GetReferencedAssemblies()
    .Select(reference => reference.Name)
    .Where(name => name is not null && (name.Contains("Lobby", StringComparison.Ordinal) ||
        name.Contains("Storage", StringComparison.Ordinal) || name.Contains("GameOasis.Gui", StringComparison.Ordinal) ||
        name.Contains("MonoGame", StringComparison.Ordinal)))
    .ToArray();
Require(forbiddenReferences.Length == 0,
    "The separate Board Editor must not reference Lobby, Storage, the legacy GUI, or MonoGame.");

var initial = new ContractDocument("application/x-go-sgf", GameOasisOfficialNames.Go + ".sgf.v1", "(;GM[1]SZ[9])");
var launch = CreateLaunch("launch-adopt", initial);
using (var room = BoardEditorProcessSession.Open(new ProcessStartInfo(hostPath), launch))
{
    Require(room.Ready.RequestId == launch.RequestId && room.Ready.RoomTypeId == PlayRoomIds.BoardEditor,
        "The separate Board Editor must report ready for the launch request.");
    var edited = initial with { Content = "(;GM[1]SZ[9]AB[aa])" };
    room.ReplacePosition(new BoardEditorPositionUpdate(room.Ready.SessionId, edited));
    var adopted = room.Adopt();
    Require(adopted.Status == BoardEditorCompletionStatus.Adopted && adopted.Position?.Content == edited.Content,
        "ADOPT must return the copied and edited position document to the fake lobby.");
    Require(initial.Content == "(;GM[1]SZ[9])", "Editing in the separate process must not mutate the lobby's source document.");
}

using (var room = BoardEditorProcessSession.Open(new ProcessStartInfo(hostPath), CreateLaunch("launch-discard", initial)))
{
    var discarded = room.Discard();
    Require(discarded.Status == BoardEditorCompletionStatus.Discarded && discarded.Position is null,
        "DISCARD must return to the fake lobby without changing the original position.");
}

var abnormalStart = new ProcessStartInfo(hostPath);
abnormalStart.ArgumentList.Add("--exit-after-open");
using (var room = BoardEditorProcessSession.Open(abnormalStart, CreateLaunch("launch-abnormal", initial)))
{
    var returnedToLobby = false;
    try { _ = room.Adopt(); }
    catch (IOException) { returnedToLobby = true; }
    Require(returnedToLobby, "An abnormal Play Room exit must be detectable so the caller can return to the lobby.");
}

Console.WriteLine("PASS: A fake lobby opened the separate Board Editor, adopted or discarded a position, and detected abnormal exit.");

static PlayRoomLaunchRequest CreateLaunch(string requestId, ContractDocument initial) =>
    new(1, requestId, PlayRoomIds.BoardEditor, GameOasisOfficialNames.Go,
        new PlaySpaceTypeId(GameOasisOfficialNames.Go),
        new ContractDocument("application/json", GameOasisOfficialNames.Go + ".configuration.v1", "{}"), initial, []);

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
