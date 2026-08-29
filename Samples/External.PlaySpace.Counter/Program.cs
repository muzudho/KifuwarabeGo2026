using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.PlayRoomEngine.JsonLines;
using System.Text.Json;

await PlayRoomEngineJsonLinesHost.RunAsync(new CounterPlaySpace());

sealed class CounterPlaySpace : IPlaySpaceProtocol
{
    private const string TypeId = "example.external.games.counter";
    private const string ConfigurationSchema = TypeId + ".configuration.v1";
    private const string ActionSchema = TypeId + ".action.v1";
    private const string StateSchema = TypeId + ".state.v1";
    private readonly Dictionary<PlaySpaceSessionId, CounterSession> _sessions = [];

    public ValueTask<ProtocolResponse<PlaySpaceDescriptor>> DescribeAsync(CancellationToken cancellationToken = default) =>
        Success(new PlaySpaceDescriptor(new(TypeId), "External counter sample", ContractVersion.V1_0,
            "External.PlaySpace.Counter", "1.0.0", ["increment-action"]));

    public ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(CancellationToken cancellationToken = default) =>
        Success(Document(ConfigurationSchema,
            """{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object","required":["target"],"properties":{"target":{"type":"integer","minimum":1}},"additionalProperties":false}"""));

    public ValueTask<ProtocolResponse<PlaySpaceConfigurationValidation>> ValidateConfigurationAsync(
        ValidatePlaySpaceConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var target = ReadInteger(request.Configuration, ConfigurationSchema, "target");
        return Success(target is > 0
            ? new PlaySpaceConfigurationValidation(true, [])
            : new PlaySpaceConfigurationValidation(false, [new ProtocolError("invalid-target", "target must be positive.")]));
    }

    public ValueTask<ProtocolResponse<PlaySpaceSessionCreated>> CreateSessionAsync(
        CreatePlaySpaceSessionRequest request, CancellationToken cancellationToken = default)
    {
        var target = ReadInteger(request.Configuration, ConfigurationSchema, "target");
        if (target is not > 0) return Failure<PlaySpaceSessionCreated>("invalid-target", "target must be positive.");
        var id = new PlaySpaceSessionId(Guid.NewGuid().ToString("N"));
        var session = new CounterSession(id, target.Value);
        _sessions.Add(id, session);
        return Success(new PlaySpaceSessionCreated(id, Snapshot(session)));
    }

    public ValueTask<ProtocolResponse<PlaySpaceSnapshot>> GetSnapshotAsync(
        GetPlaySpaceSnapshotRequest request, CancellationToken cancellationToken = default) =>
        _sessions.TryGetValue(request.SessionId, out var session)
            ? Success(Snapshot(session))
            : Failure<PlaySpaceSnapshot>("session-not-found", "Session was not found.");

    public ValueTask<ProtocolResponse<PlaySpaceActionApplied>> ApplyActionAsync(
        ApplyPlaySpaceActionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(request.SessionId, out var session))
            return Failure<PlaySpaceActionApplied>("session-not-found", "Session was not found.");
        if (request.ExpectedRevision != session.Revision)
            return Failure<PlaySpaceActionApplied>("revision-conflict", "Revision did not match.");
        var amount = ReadInteger(request.Action, ActionSchema, "amount");
        if (amount is not > 0) return Failure<PlaySpaceActionApplied>("invalid-amount", "amount must be positive.");
        session.Value += amount.Value;
        session.Revision++;
        return Success(new PlaySpaceActionApplied(true, Snapshot(session), [], null));
    }

    public ValueTask<ProtocolResponse<PlaySpaceSessionClosed>> CloseSessionAsync(
        ClosePlaySpaceSessionRequest request, CancellationToken cancellationToken = default) =>
        _sessions.Remove(request.SessionId)
            ? Success(new PlaySpaceSessionClosed(request.SessionId))
            : Failure<PlaySpaceSessionClosed>("session-not-found", "Session was not found.");

    private static PlaySpaceSnapshot Snapshot(CounterSession session)
    {
        var terminal = session.Value >= session.Target;
        return new(session.Id, session.Revision,
            Document(StateSchema, JsonSerializer.Serialize(new { version = 1, value = session.Value, target = session.Target, terminal })),
            terminal, terminal ? Document(TypeId + ".outcome.v1", "{\"result\":\"target-reached\"}") : null);
    }

    private static int? ReadInteger(ContractDocument document, string schemaId, string property)
    {
        if (document.MediaType != "application/json" || document.SchemaId != schemaId) return null;
        try { using var json = JsonDocument.Parse(document.Content); return json.RootElement.GetProperty(property).GetInt32(); }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException) { return null; }
    }
    private static ContractDocument Document(string schemaId, string content) => new("application/json", schemaId, content);
    private static ValueTask<ProtocolResponse<T>> Success<T>(T value) => ValueTask.FromResult(ProtocolResponse<T>.Success(value));
    private static ValueTask<ProtocolResponse<T>> Failure<T>(string code, string message) =>
        ValueTask.FromResult(ProtocolResponse<T>.Failure(new(code, message)));
    private sealed class CounterSession(PlaySpaceSessionId id, int target)
    {
        public PlaySpaceSessionId Id { get; } = id;
        public int Target { get; } = target;
        public int Value { get; set; }
        public long Revision { get; set; }
    }
}
