namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

using System.Text.Json;
using System.Text.Json.Serialization;
using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>Game Oasis共通裁定結果の種類です。</summary>
public enum GameOasisAdjudicationKind
{
    /// <summary>一つの役割を勝者として確定します。</summary>
    Winner,

    /// <summary>勝者を定めず引き分けとして確定します。</summary>
    Draw,

    /// <summary>対局結果を無効または無勝負として確定します。</summary>
    Void,

    /// <summary>ゲームが成立する前後に運営上の理由で中止します。</summary>
    Cancelled,
}

/// <summary>検証済みのGame Oasis共通裁定結果です。</summary>
public sealed record GameOasisAdjudicationResult(
    GameOasisAdjudicationKind Kind,
    string? WinnerRoleId,
    string ReasonCode,
    string? Comment);

/// <summary>Game Oasis共通裁定結果文書の生成、スキーマ公開、検証を提供します。</summary>
public static class GameOasisAdjudicationDocuments
{
    /// <summary>JSON文書のメディアタイプです。</summary>
    public const string MediaType = "application/json";

    /// <summary>裁定結果文書v1のスキーマIDです。</summary>
    public const string ResultSchemaId = "urn:kifuwarabe:game-oasis:adjudication-result:v1";

    /// <summary>裁定結果JSON Schema文書のスキーマIDです。</summary>
    public const string ResultJsonSchemaId = "urn:kifuwarabe:game-oasis:adjudication-result-schema:v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>標準裁定結果文書を生成します。</summary>
    public static ContractDocument CreateResult(
        GameOasisAdjudicationKind kind,
        string reasonCode,
        string? winnerRoleId = null,
        string? comment = null)
    {
        var content = JsonSerializer.Serialize(new ResultDocument(
            1,
            ToWireName(kind),
            winnerRoleId,
            reasonCode,
            comment), JsonOptions);
        return new ContractDocument(MediaType, ResultSchemaId, content);
    }

    /// <summary>裁定結果文書を検証し、型付き結果へ変換します。</summary>
    public static ProtocolResponse<GameOasisAdjudicationResult> ValidateResult(ContractDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.MediaType != MediaType || document.SchemaId != ResultSchemaId)
            return Failure("invalid-adjudication-document-type", $"Expected '{MediaType}' and schema '{ResultSchemaId}'.");

        try
        {
            using var json = JsonDocument.Parse(document.Content);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Failure("invalid-adjudication-json", "The adjudication result must be a JSON object.");
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name is not ("version" or "kind" or "winnerRoleId" or "reasonCode" or "comment"))
                    return Failure("unknown-adjudication-property", $"Property '{property.Name}' is not part of adjudication result v1.");
            }
            if (!TryRequiredInt(root, "version", out var version) || version != 1)
                return Failure("unsupported-adjudication-version", "The adjudication result version must be 1.");
            if (!TryRequiredString(root, "kind", out var kindText) || !TryParseKind(kindText, out var kind))
                return Failure("invalid-adjudication-kind", "The adjudication kind must be winner, draw, void, or cancelled.");
            if (!TryRequiredString(root, "reasonCode", out var reasonCode))
                return Failure("invalid-adjudication-reason", "The adjudication reasonCode must be a non-empty string.");
            if (!TryOptionalString(root, "winnerRoleId", out var winnerRoleId))
                return Failure("invalid-adjudication-winner", "The optional winnerRoleId must be a non-empty string.");
            if (!TryOptionalString(root, "comment", out var comment))
                return Failure("invalid-adjudication-comment", "The optional comment must be a string.");
            if (kind == GameOasisAdjudicationKind.Winner && winnerRoleId is null)
                return Failure("adjudication-winner-required", "A winner result requires winnerRoleId.");
            if (kind != GameOasisAdjudicationKind.Winner && winnerRoleId is not null)
                return Failure("adjudication-winner-not-allowed", "Only a winner result may contain winnerRoleId.");

            return ProtocolResponse<GameOasisAdjudicationResult>.Success(new(kind, winnerRoleId, reasonCode, comment));
        }
        catch (JsonException exception)
        {
            return Failure("invalid-adjudication-json", $"The adjudication result is not valid JSON: {exception.Message}");
        }
    }

    /// <summary>裁定結果文書を記述するJSON Schemaを返します。</summary>
    public static ContractDocument GetResultSchema() => new(
        MediaType,
        ResultJsonSchemaId,
        """
        {
          "$schema":"https://json-schema.org/draft/2020-12/schema",
          "$id":"urn:kifuwarabe:game-oasis:adjudication-result:v1",
          "title":"Kifuwarabe Game Oasis adjudication result v1",
          "type":"object",
          "required":["version","kind","reasonCode"],
          "properties":{
            "version":{"const":1},
            "kind":{"enum":["winner","draw","void","cancelled"]},
            "winnerRoleId":{"type":"string","minLength":1},
            "reasonCode":{"type":"string","minLength":1},
            "comment":{"type":"string"}
          },
          "allOf":[
            {
              "if":{"properties":{"kind":{"const":"winner"}}},
              "then":{"required":["winnerRoleId"]},
              "else":{"not":{"required":["winnerRoleId"]}}
            }
          ],
          "additionalProperties":false
        }
        """);

    private static bool TryRequiredInt(JsonElement root, string name, out int value)
    {
        value = default;
        return root.TryGetProperty(name, out var property) && property.TryGetInt32(out value);
    }

    private static bool TryRequiredString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        return root.TryGetProperty(name, out var property) &&
               property.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(value = property.GetString()!);
    }

    private static bool TryOptionalString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(name, out var property))
            return true;
        if (property.ValueKind != JsonValueKind.String)
            return false;
        value = property.GetString();
        return name == "comment" || !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseKind(string value, out GameOasisAdjudicationKind kind)
    {
        kind = value switch
        {
            "winner" => GameOasisAdjudicationKind.Winner,
            "draw" => GameOasisAdjudicationKind.Draw,
            "void" => GameOasisAdjudicationKind.Void,
            "cancelled" => GameOasisAdjudicationKind.Cancelled,
            _ => default,
        };
        return value is "winner" or "draw" or "void" or "cancelled";
    }

    private static string ToWireName(GameOasisAdjudicationKind kind) => kind switch
    {
        GameOasisAdjudicationKind.Winner => "winner",
        GameOasisAdjudicationKind.Draw => "draw",
        GameOasisAdjudicationKind.Void => "void",
        GameOasisAdjudicationKind.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown adjudication kind."),
    };

    private static ProtocolResponse<GameOasisAdjudicationResult> Failure(string code, string message) =>
        ProtocolResponse<GameOasisAdjudicationResult>.Failure(new ProtocolError(code, message));

    private sealed record ResultDocument(
        int Version,
        string Kind,
        string? WinnerRoleId,
        string ReasonCode,
        string? Comment);
}
