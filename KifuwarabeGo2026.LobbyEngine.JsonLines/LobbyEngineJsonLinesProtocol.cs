namespace KifuwarabeGo2026.LobbyEngine.JsonLines;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using System.Text.Json;

public static class LobbyEngineJsonLinesProtocol
{
    public const int Version = 1;
    public const string ListEntriesMethod = "listEntries";
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record LobbyEngineRequest(int ProtocolVersion, string RequestId, string Method, JsonElement? Parameters = null);
public sealed record LobbyEngineResponse(int ProtocolVersion, string RequestId, bool Success, JsonElement? Result, LobbyEngineError? Error);
public sealed record LobbyEngineError(string Code, string Message);
public sealed record LobbyEntryList(IReadOnlyList<EntryProfile> Entries);
