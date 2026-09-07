using System.Text.Json;
using KifuwarabeGo2026.InstallerEngine;
using KifuwarabeGo2026.InstallerEngine.JsonLines;
using KifuwarabeGo2026.InstallerEngine.Platform;

var localApplicationData = ReadOption(args, "--local-application-data");
var myPictures = ReadOption(args, "--my-pictures");
var desktopPlatform = new DesktopInstallerEnginePlatform();
var defaultLocalApplicationData = string.IsNullOrWhiteSpace(desktopPlatform.LocalApplicationData)
    ? AppContext.BaseDirectory
    : desktopPlatform.LocalApplicationData;
var defaultMyPictures = string.IsNullOrWhiteSpace(desktopPlatform.MyPictures)
    ? Path.Combine(defaultLocalApplicationData, "Pictures")
    : desktopPlatform.MyPictures;
IInstallerEnginePlatform platform = localApplicationData is null && myPictures is null
    ? new HostPlatform(defaultLocalApplicationData, defaultMyPictures, desktopPlatform)
    : new HostPlatform(
        localApplicationData ?? defaultLocalApplicationData,
        myPictures ?? defaultMyPictures,
        desktopPlatform);
using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
var engine = new InProcessInstallerEngine(platform, httpClient);

string? line;
while ((line = Console.ReadLine()) is not null)
{
    InstallerEngineResponse response;
    try
    {
        var request = JsonSerializer.Deserialize<InstallerEngineRequest>(line, InstallerEngineJsonLinesProtocol.JsonOptions)
            ?? throw new JsonException("要求が null です。");
        response = Handle(request, engine);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        response = new InstallerEngineResponse(
            InstallerEngineJsonLinesProtocol.Version,
            TryReadRequestId(line),
            false,
            null,
            exception.Message);
    }

    Console.WriteLine(JsonSerializer.Serialize(response, InstallerEngineJsonLinesProtocol.JsonOptions));
}

static InstallerEngineResponse Handle(InstallerEngineRequest request, IInstallerEngine engine)
{
    if (request.ProtocolVersion != InstallerEngineJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応のプロトコルバージョンです: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestId がありません。");

    object? result = request.Method switch
    {
        InstallerEngineJsonLinesProtocol.GetStateMethod => engine.GetState(),
        InstallerEngineJsonLinesProtocol.GetInstalledVersionsMethod => engine.GetInstalledVersions(),
        InstallerEngineJsonLinesProtocol.GetCurrentDirectoryMethod =>
            engine.GetCurrentDirectory(ReadParameters<InstallerProductParameters>(request).Product),
        InstallerEngineJsonLinesProtocol.UninstallMethod =>
            engine.Uninstall(ReadParameters<UninstallParameters>(request).InstalledVersion),
        InstallerEngineJsonLinesProtocol.ChangeInstallationDirectoryMethod =>
            engine.ChangeInstallationDirectory(ReadParameters<InstallationDirectoryParameters>(request).Directory),
        InstallerEngineJsonLinesProtocol.ChangeScreenshotDirectoryMethod =>
            engine.ChangeScreenshotDirectory(ReadParameters<ScreenshotDirectoryParameters>(request).Directory),
        InstallerEngineJsonLinesProtocol.ChangeCloseAfterStartingGuiMethod =>
            engine.ChangeCloseAfterStartingGui(ReadParameters<CloseAfterStartingGuiParameters>(request).Value),
        _ => throw new InvalidDataException($"未対応のメソッドです: {request.Method}"),
    };
    return new InstallerEngineResponse(
        InstallerEngineJsonLinesProtocol.Version,
        request.RequestId,
        true,
        JsonSerializer.SerializeToElement(result, InstallerEngineJsonLinesProtocol.JsonOptions),
        null);
}

static T ReadParameters<T>(InstallerEngineRequest request)
{
    if (request.Parameters is null) throw new InvalidDataException("parameters がありません。");
    return request.Parameters.Value.Deserialize<T>(InstallerEngineJsonLinesProtocol.JsonOptions)
        ?? throw new InvalidDataException("parameters を読み取れませんでした。");
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

sealed class HostPlatform(string localApplicationData, string myPictures, DesktopInstallerEnginePlatform desktop) : IInstallerEnginePlatform
{
    public string LocalApplicationData { get; } = Path.GetFullPath(localApplicationData);
    public string MyPictures { get; } = Path.GetFullPath(myPictures);
    public bool Start(string executable, string workingDirectory) => desktop.Start(executable, workingDirectory);
    public bool IsProcessRunningFrom(string directory) => desktop.IsProcessRunningFrom(directory);
}
