using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Storage;
using KifuwarabeGo2026.LobbyEngine.JsonLines;
using System.Text.Json;

var entryListPath = ReadOption(args, "--entry-list") ?? CatalogDocumentStorage.Paths.EntryListPath;
string? line;
while ((line = Console.ReadLine()) is not null)
{
    LobbyEngineResponse response;
    try
    {
        var request = JsonSerializer.Deserialize<LobbyEngineRequest>(line, LobbyEngineJsonLinesProtocol.JsonOptions)
            ?? throw new JsonException("要求が null です。");
        response = Handle(request, entryListPath);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        response = new LobbyEngineResponse(LobbyEngineJsonLinesProtocol.Version, TryReadRequestId(line), false, null,
            new LobbyEngineError("invalid-request", exception.Message));
    }
    Console.WriteLine(JsonSerializer.Serialize(response, LobbyEngineJsonLinesProtocol.JsonOptions));
}

static LobbyEngineResponse Handle(LobbyEngineRequest request, string entryListPath)
{
    if (request.ProtocolVersion != LobbyEngineJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応のロビープロトコル版です: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestId がありません。");
    object result = request.Method switch
    {
        LobbyEngineJsonLinesProtocol.ListEntriesMethod =>
            new LobbyEntryList(EntryCatalog.Load(CatalogDocumentStorage.Default, entryListPath).Profiles),
        _ => throw new InvalidDataException($"未対応のメソッドです: {request.Method}"),
    };
    return new LobbyEngineResponse(LobbyEngineJsonLinesProtocol.Version, request.RequestId, true,
        JsonSerializer.SerializeToElement(result, LobbyEngineJsonLinesProtocol.JsonOptions), null);
}

static string TryReadRequestId(string json)
{
    try
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("requestId", out var value) ? value.GetString() ?? "" : "";
    }
    catch (JsonException) { return ""; }
}

static string? ReadOption(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase)) return arguments[index + 1];
    return null;
}
