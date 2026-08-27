using System.Text.Json;
using KifuwarabeGo2026.LauncherEngine;
using KifuwarabeGo2026.LauncherEngine.JsonLines;
using KifuwarabeGo2026.LauncherEngine.Platform;

var localApplicationData = ReadOption(args, "--local-application-data");
var myPictures = ReadOption(args, "--my-pictures");
var desktopPlatform = new DesktopLauncherEnginePlatform();
ILauncherEnginePlatform platform = localApplicationData is null && myPictures is null
    ? desktopPlatform
    : new HostPlatform(
        localApplicationData ?? desktopPlatform.LocalApplicationData,
        myPictures ?? desktopPlatform.MyPictures,
        desktopPlatform);
using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
var engine = new InProcessLauncherEngine(platform, httpClient);

string? line;
while ((line = Console.ReadLine()) is not null)
{
    LauncherEngineResponse response;
    try
    {
        var request = JsonSerializer.Deserialize<LauncherEngineRequest>(line, LauncherEngineJsonLinesProtocol.JsonOptions)
            ?? throw new JsonException("要求が null です。");
        response = Handle(request, engine);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        response = new LauncherEngineResponse(
            LauncherEngineJsonLinesProtocol.Version,
            TryReadRequestId(line),
            false,
            null,
            exception.Message);
    }

    Console.WriteLine(JsonSerializer.Serialize(response, LauncherEngineJsonLinesProtocol.JsonOptions));
}

static LauncherEngineResponse Handle(LauncherEngineRequest request, ILauncherEngine engine)
{
    if (request.ProtocolVersion != LauncherEngineJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応のプロトコルバージョンです: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestId がありません。");

    object result = request.Method switch
    {
        LauncherEngineJsonLinesProtocol.GetStateMethod => engine.GetState(),
        LauncherEngineJsonLinesProtocol.GetInstalledVersionsMethod => engine.GetInstalledVersions(),
        _ => throw new InvalidDataException($"未対応のメソッドです: {request.Method}"),
    };
    return new LauncherEngineResponse(
        LauncherEngineJsonLinesProtocol.Version,
        request.RequestId,
        true,
        JsonSerializer.SerializeToElement(result, LauncherEngineJsonLinesProtocol.JsonOptions),
        null);
}

static string TryReadRequestId(string json)
{
    try
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("requestId", out var requestId)
            ? requestId.GetString() ?? string.Empty
            : string.Empty;
    }
    catch (JsonException) { return string.Empty; }
}

static string? ReadOption(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase)) return arguments[index + 1];
    return null;
}

sealed class HostPlatform(string localApplicationData, string myPictures, DesktopLauncherEnginePlatform desktop) : ILauncherEnginePlatform
{
    public string LocalApplicationData { get; } = Path.GetFullPath(localApplicationData);
    public string MyPictures { get; } = Path.GetFullPath(myPictures);
    public bool Start(string executable, string workingDirectory) => desktop.Start(executable, workingDirectory);
    public bool IsProcessRunningFrom(string directory) => desktop.IsProcessRunningFrom(directory);
}
