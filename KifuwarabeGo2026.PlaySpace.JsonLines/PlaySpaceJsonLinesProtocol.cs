namespace KifuwarabeGo2026.PlaySpace.JsonLines;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using System.Text.Json;

public static class PlaySpaceJsonLinesProtocol
{
    public const int Version = 1;
    public const string DescribeMethod = "describe";
    public const string GetConfigurationSchemaMethod = "getConfigurationSchema";
    public const string ValidateConfigurationMethod = "validateConfiguration";
    public const string CreateSessionMethod = "createSession";
    public const string GetSnapshotMethod = "getSnapshot";
    public const string ApplyActionMethod = "applyAction";
    public const string CloseSessionMethod = "closeSession";
    public const string GoodbyeMethod = "goodbye";
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record PlaySpaceProcessRequest(int ProtocolVersion, string RequestId, string Method, JsonElement? Parameters = null);
public sealed record PlaySpaceProcessResponse(int ProtocolVersion, string RequestId, bool Success, JsonElement? Result, PlaySpaceProcessError? Error);
public sealed record PlaySpaceProcessError(string Code, string Message);
public sealed record PlaySpaceProtocolResult<T>(T? Value, ProtocolError? Error);
