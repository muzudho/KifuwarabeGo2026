namespace KifuwarabeGo2026.PlayRoom.JsonLines;

using System.Text.Json;

public static class PlayRoomJsonLinesProtocol
{
    public const int Version = 1;
    public const string OpenMethod = "open";
    public const string ReplacePositionMethod = "replacePosition";
    public const string AdoptMethod = "adopt";
    public const string DiscardMethod = "discard";
    public const string GoodbyeMethod = "goodbye";
    public const string NavigateMethod = "navigate";
    public const string UsePositionMethod = "usePosition";
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record PlayRoomProcessRequest(int ProtocolVersion, string RequestId, string Method, JsonElement? Parameters = null);
public sealed record PlayRoomProcessResponse(int ProtocolVersion, string RequestId, bool Success, JsonElement? Result, PlayRoomProcessError? Error);
public sealed record PlayRoomProcessError(string Code, string Message);
