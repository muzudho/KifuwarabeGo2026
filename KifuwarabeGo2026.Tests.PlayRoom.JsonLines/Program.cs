using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.JsonLines;
using System.Diagnostics;

var root = FindRepositoryRoot();
var hostPath = Path.Combine(root, "KifuwarabeGo2026.PlayRoom.BoardEditor.JsonLinesHost", "bin", "Release", "net8.0",
    "KifuwarabeGo2026.PlayRoom.BoardEditor.JsonLinesHost.dll");
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
using (var room = BoardEditorProcessSession.Open(CreateHostStartInfo(hostPath), launch))
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

using (var room = BoardEditorProcessSession.Open(CreateHostStartInfo(hostPath), CreateLaunch("launch-discard", initial)))
{
    var discarded = room.Discard();
    Require(discarded.Status == BoardEditorCompletionStatus.Discarded && discarded.Position is null,
        "DISCARD must return to the fake lobby without changing the original position.");
}

var abnormalStart = CreateHostStartInfo(hostPath);
abnormalStart.ArgumentList.Add("--exit-after-open");
using (var room = BoardEditorProcessSession.Open(abnormalStart, CreateLaunch("launch-abnormal", initial)))
{
    var returnedToLobby = false;
    try { _ = room.Adopt(); }
    catch (IOException) { returnedToLobby = true; }
    Require(returnedToLobby, "An abnormal Play Room exit must be detectable so the caller can return to the lobby.");
}

var reviewHostPath = Path.Combine(root, "KifuwarabeGo2026.PlayRoom.Review.JsonLinesHost", "bin", "Release", "net8.0",
    "KifuwarabeGo2026.PlayRoom.Review.JsonLinesHost.dll");
Require(File.Exists(reviewHostPath), $"Review host must be built: {reviewHostPath}");
RequireNoForbiddenReferences(reviewHostPath, "Review");
var gameRecord = new ContractDocument("application/x-go-sgf", GameOasisOfficialNames.Go + ".sgf.v1", "(;GM[1]SZ[9];B[aa];W[bb])");
var reviewLaunch = CreateReviewLaunch("review-position", gameRecord);
using (var review = ReviewProcessSession.Open(CreateHostStartInfo(reviewHostPath), reviewLaunch))
{
    Require(review.Ready.RequestId == reviewLaunch.RequestId && review.Ready.RoomTypeId == PlayRoomIds.Review,
        "The separate Review must report ready for the launch request.");
    var view = review.Navigate(2);
    Require(view.MoveIndex == 2 && view.GameRecord.Content == gameRecord.Content,
        "Review navigation must retain the read-only game record document.");
    var selected = new ContractDocument("application/json", GameOasisOfficialNames.Go + ".position.v1",
        "{\"boardSize\":9,\"moveIndex\":2}");
    var completion = review.UsePosition(2, selected);
    Require(completion.Status == ReviewCompletionStatus.PositionSelected && completion.MoveIndex == 2 &&
            completion.Position?.Content == selected.Content,
        "USE POSITION must return a copied position document to the fake lobby.");
    Require(gameRecord.Content == "(;GM[1]SZ[9];B[aa];W[bb])", "Review must not mutate the source game record.");
}

using (var review = ReviewProcessSession.Open(CreateHostStartInfo(reviewHostPath), CreateReviewLaunch("review-close", gameRecord)))
{
    _ = review.Navigate(1);
}

var abnormalReviewStart = CreateHostStartInfo(reviewHostPath);
abnormalReviewStart.ArgumentList.Add("--exit-after-open");
using (var review = ReviewProcessSession.Open(abnormalReviewStart, CreateReviewLaunch("review-abnormal", gameRecord)))
{
    var returnedToLobby = false;
    try { _ = review.Navigate(0); }
    catch (IOException) { returnedToLobby = true; }
    Require(returnedToLobby, "An abnormal Review exit must be detectable so the caller can return to the lobby.");
}

Console.WriteLine("PASS: Fake lobby scenarios passed for separate Board Editor and Review Play Rooms, including abnormal exits.");

static PlayRoomLaunchRequest CreateLaunch(string requestId, ContractDocument initial) =>
    new(1, requestId, PlayRoomIds.BoardEditor, GameOasisOfficialNames.Go,
        new PlaySpaceTypeId(GameOasisOfficialNames.Go),
        new ContractDocument("application/json", GameOasisOfficialNames.Go + ".configuration.v1", "{}"), initial, []);

static PlayRoomLaunchRequest CreateReviewLaunch(string requestId, ContractDocument gameRecord) =>
    new(1, requestId, PlayRoomIds.Review, GameOasisOfficialNames.Go,
        new PlaySpaceTypeId(GameOasisOfficialNames.Go),
        new ContractDocument("application/json", GameOasisOfficialNames.Go + ".configuration.v1", "{}"), gameRecord, []);

static void RequireNoForbiddenReferences(string executablePath, string roomName)
{
    var assemblyPath = Path.ChangeExtension(executablePath, ".dll");
    var forbidden = System.Reflection.Assembly.LoadFile(assemblyPath).GetReferencedAssemblies()
        .Select(reference => reference.Name)
        .Where(name => name is not null && (name.Contains("Lobby", StringComparison.Ordinal) ||
            name.Contains("Storage", StringComparison.Ordinal) || name.Contains("GameOasis.Gui", StringComparison.Ordinal) ||
            name.Contains("MonoGame", StringComparison.Ordinal)))
        .ToArray();
    Require(forbidden.Length == 0, $"The separate {roomName} must not reference Lobby, Storage, the legacy GUI, or MonoGame.");
}

static ProcessStartInfo CreateHostStartInfo(string assemblyPath)
{
    var startInfo = new ProcessStartInfo("dotnet");
    startInfo.ArgumentList.Add(assemblyPath);
    return startInfo;
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
