using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

var winnerDocument = GameOasisAdjudicationDocuments.CreateResult(
    GameOasisAdjudicationKind.Winner,
    "disqualification",
    "white",
    "Black exceeded the permitted interruption time.");
var winner = RequireSuccess(GameOasisAdjudicationDocuments.ValidateResult(winnerDocument));
Require(winner.Kind == GameOasisAdjudicationKind.Winner, "Winner kind must round-trip.");
Require(winner.WinnerRoleId == "white", "Winner role must round-trip.");
Require(winner.ReasonCode == "disqualification", "Reason code must round-trip.");

foreach (var kind in new[]
         {
             GameOasisAdjudicationKind.Draw,
             GameOasisAdjudicationKind.Void,
             GameOasisAdjudicationKind.Cancelled,
         })
{
    var document = GameOasisAdjudicationDocuments.CreateResult(kind, "operator-decision");
    var result = RequireSuccess(GameOasisAdjudicationDocuments.ValidateResult(document));
    Require(result.Kind == kind && result.WinnerRoleId is null, $"{kind} must round-trip without a winner.");
}

var schema = GameOasisAdjudicationDocuments.GetResultSchema();
Require(schema.MediaType == "application/json", "The schema must be JSON.");
using (var schemaJson = JsonDocument.Parse(schema.Content))
    Require(schemaJson.RootElement.GetProperty("$id").GetString() == GameOasisAdjudicationDocuments.ResultSchemaId, "The JSON Schema ID must describe the result document.");

RequireFailure(
    Document("""{"version":1,"kind":"winner","reasonCode":"disqualification"}"""),
    "adjudication-winner-required");
RequireFailure(
    Document("""{"version":1,"kind":"draw","winnerRoleId":"black","reasonCode":"agreement"}"""),
    "adjudication-winner-not-allowed");
RequireFailure(
    Document("""{"version":1,"kind":"void","reasonCode":"","comment":"bad"}"""),
    "invalid-adjudication-reason");
RequireFailure(
    Document("""{"version":1,"kind":"cancelled","reasonCode":"operator-decision","extra":true}"""),
    "unknown-adjudication-property");
RequireFailure(Document("{"), "invalid-adjudication-json");
RequireFailure(
    new ContractDocument("text/plain", GameOasisAdjudicationDocuments.ResultSchemaId, "winner"),
    "invalid-adjudication-document-type");

Console.WriteLine("PASS: Game Oasis adjudication documents validated winner, draw, void, cancelled, and malformed cases without external dependencies.");
return;

static ContractDocument Document(string json) => new(
    GameOasisAdjudicationDocuments.MediaType,
    GameOasisAdjudicationDocuments.ResultSchemaId,
    json);

static T RequireSuccess<T>(ProtocolResponse<T> response)
{
    if (!response.IsSuccess || response.Value is null)
        throw new InvalidOperationException($"Expected success: {response.Error?.Code} {response.Error?.Message}");
    return response.Value;
}

static void RequireFailure(ContractDocument document, string expectedCode)
{
    var response = GameOasisAdjudicationDocuments.ValidateResult(document);
    Require(!response.IsSuccess && response.Error?.Code == expectedCode, $"Expected {expectedCode}, got {response.Error?.Code}.");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
