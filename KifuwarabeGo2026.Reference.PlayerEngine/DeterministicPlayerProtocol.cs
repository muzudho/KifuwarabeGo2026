namespace KifuwarabeGo2026.Reference.PlayerEngine;

using System.Collections.Concurrent;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

/// <summary>通常囲碁とポン抜きへ参加できる、決定的なProtocol P参照プレイヤーです。</summary>
public sealed class DeterministicPlayerProtocol : IPlayerProtocol
{
    public static readonly PlayerEngineId StableEngineId = new("org.kifuwarabe.reference.deterministic-player");
    public static readonly PlaySpaceTypeId GoTypeId = new("org.kifuwarabe.games.go");
    public static readonly PlaySpaceTypeId PonnukiTypeId = new("org.kifuwarabe.games.ponnuki");

    private const string GoStateSchema = "org.kifuwarabe.games.go.state.v1";
    private const string GoActionSchema = "org.kifuwarabe.games.go.action.v1";
    private const string PonnukiStateSchema = "org.kifuwarabe.games.ponnuki.state.v1";
    private const string PonnukiActionSchema = "org.kifuwarabe.games.ponnuki.action.v1";
    private readonly ConcurrentDictionary<PlayerBindingId, BindingState> _bindings = new();

    public ValueTask<ProtocolResponse<PlayerEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<PlayerEngineDescriptor>.Success(new(
            StableEngineId,
            "きふわらべ決定的参照プレイヤー",
            ContractVersion.V1_0,
            "KifuwarabeGo2026.Reference.PlayerEngine",
            typeof(DeterministicPlayerProtocol).Assembly.GetName().Version?.ToString() ?? "4.0.0",
            [GoTypeId, PonnukiTypeId],
            ["deterministic", "rejection-recovery", "go", "ponnuki"])));
    }

    public ValueTask<ProtocolResponse<PlayerSessionStarted>> StartSessionAsync(
        PlayerSessionStartRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var typeId = request.InitialObservation.PlaySpaceTypeId;
        if (typeId != GoTypeId && typeId != PonnukiTypeId)
            return ValueTask.FromResult(Failure<PlayerSessionStarted>("unsupported-play-space", $"Play-space '{request.InitialObservation.PlaySpaceTypeId}' is not supported."));
        if (!_bindings.TryAdd(request.BindingId, new(request.RoleId, request.InitialObservation)))
            return ValueTask.FromResult(Failure<PlayerSessionStarted>("player-binding-already-started", $"Binding '{request.BindingId}' is already active."));
        return ValueTask.FromResult(ProtocolResponse<PlayerSessionStarted>.Success(new(request.BindingId)));
    }

    public ValueTask<ProtocolResponse<PlayerActionSelected>> SelectActionAsync(
        PlayerActionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(request.BindingId, out var binding))
            return ValueTask.FromResult(Failure<PlayerActionSelected>("player-binding-not-found", $"Binding '{request.BindingId}' is not active."));
        if (request.Observation.IsTerminal)
            return ValueTask.FromResult(Failure<PlayerActionSelected>("game-already-terminal", "A terminal game has no next action."));
        if (request.Observation.OperationalState != GameOasisOperationalState.Running)
            return ValueTask.FromResult(Failure<PlayerActionSelected>("game-not-running", "The game is not accepting player actions."));

        lock (binding.Sync)
        {
            binding.Observation = request.Observation;
            var parsed = ParseBoard(request.Observation);
            if (!parsed.IsSuccess || parsed.Value is null)
                return ValueTask.FromResult(ProtocolResponse<PlayerActionSelected>.Failure(parsed.Error!));
            var board = parsed.Value;
            for (var y = 0; y < board.Size; y++)
            for (var x = 0; x < board.Size; x++)
            {
                if (board.Occupied.Contains((x, y)) || board.KoPoint == (x, y) ||
                    binding.Rejected.Contains((request.Observation.Revision, x, y)))
                    continue;
                return ValueTask.FromResult(ProtocolResponse<PlayerActionSelected>.Success(new(
                    request.BindingId,
                    request.Observation.Revision,
                    Action(request.Observation.PlaySpaceTypeId, request.RoleId, "play", x, y))));
            }
            return ValueTask.FromResult(ProtocolResponse<PlayerActionSelected>.Success(new(
                request.BindingId,
                request.Observation.Revision,
                Action(request.Observation.PlaySpaceTypeId, request.RoleId, "pass"))));
        }
    }

    public ValueTask<ProtocolResponse<PlayerActionNotified>> NotifyActionAsync(
        PlayerActionNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(notification.BindingId, out var binding))
            return ValueTask.FromResult(Failure<PlayerActionNotified>("player-binding-not-found", $"Binding '{notification.BindingId}' is not active."));
        lock (binding.Sync)
        {
            if (!notification.WasAccepted && TryReadPoint(notification.Action, out var x, out var y))
                binding.Rejected.Add((notification.Observation.Revision, x, y));
            if (notification.WasAccepted)
                binding.Rejected.RemoveWhere(candidate => candidate.Revision < notification.Observation.Revision);
            binding.Observation = notification.Observation;
        }
        return ValueTask.FromResult(ProtocolResponse<PlayerActionNotified>.Success(new(
            notification.BindingId,
            notification.Observation.Revision)));
    }

    public ValueTask<ProtocolResponse<PlayerStateNotified>> NotifyStateAsync(
        PlayerStateNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(notification.BindingId, out var binding))
            return ValueTask.FromResult(Failure<PlayerStateNotified>("player-binding-not-found", $"Binding '{notification.BindingId}' is not active."));
        lock (binding.Sync)
            binding.Observation = notification.Observation;
        return ValueTask.FromResult(ProtocolResponse<PlayerStateNotified>.Success(new(
            notification.BindingId,
            notification.Observation.Revision,
            notification.Observation.OperationRevision)));
    }

    public ValueTask<ProtocolResponse<PlayerSessionEnded>> EndSessionAsync(
        PlayerSessionEndRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_bindings.TryRemove(request.BindingId, out _)
            ? ProtocolResponse<PlayerSessionEnded>.Success(new(request.BindingId))
            : Failure<PlayerSessionEnded>("player-binding-not-found", $"Binding '{request.BindingId}' is not active."));
    }

    private static ProtocolResponse<BoardView> ParseBoard(PlayerGameObservation observation)
    {
        var expectedSchema = observation.PlaySpaceTypeId == GoTypeId ? GoStateSchema : PonnukiStateSchema;
        if (observation.State.MediaType != "application/json" || observation.State.SchemaId != expectedSchema)
            return Failure<BoardView>("unsupported-state-document", $"State schema '{observation.State.SchemaId}' is not supported for '{observation.PlaySpaceTypeId}'.");
        try
        {
            using var json = JsonDocument.Parse(observation.State.Content);
            var root = json.RootElement;
            var size = root.GetProperty("boardSize").GetInt32();
            if (size is < 1 or > 100)
                return Failure<BoardView>("invalid-board-size", "The observed boardSize must be between 1 and 100.");
            var occupied = new HashSet<(int X, int Y)>();
            AddPoints(root.GetProperty("black"), occupied);
            AddPoints(root.GetProperty("white"), occupied);
            (int X, int Y)? ko = null;
            if (root.TryGetProperty("koPoint", out var koElement) && koElement.ValueKind == JsonValueKind.Object)
                ko = (koElement.GetProperty("x").GetInt32(), koElement.GetProperty("y").GetInt32());
            return ProtocolResponse<BoardView>.Success(new(size, occupied, ko));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            return Failure<BoardView>("invalid-state-document", exception.Message);
        }
    }

    private static void AddPoints(JsonElement points, HashSet<(int X, int Y)> occupied)
    {
        foreach (var point in points.EnumerateArray())
            occupied.Add((point.GetProperty("x").GetInt32(), point.GetProperty("y").GetInt32()));
    }

    private static ContractDocument Action(
        PlaySpaceTypeId typeId,
        string roleId,
        string actionType,
        int? x = null,
        int? y = null)
    {
        var content = JsonSerializer.Serialize(new { version = 1, type = actionType, player = roleId, x, y },
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        return new("application/json", typeId == GoTypeId ? GoActionSchema : PonnukiActionSchema, content);
    }

    private static bool TryReadPoint(ContractDocument action, out int x, out int y)
    {
        x = y = default;
        try
        {
            using var json = JsonDocument.Parse(action.Content);
            return json.RootElement.TryGetProperty("x", out var xElement) && xElement.TryGetInt32(out x) &&
                   json.RootElement.TryGetProperty("y", out var yElement) && yElement.TryGetInt32(out y);
        }
        catch (JsonException) { return false; }
    }

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new(code, message));

    private sealed record BoardView(int Size, HashSet<(int X, int Y)> Occupied, (int X, int Y)? KoPoint);

    private sealed class BindingState(string roleId, PlayerGameObservation observation)
    {
        public object Sync { get; } = new();
        public string RoleId { get; } = roleId;
        public PlayerGameObservation Observation { get; set; } = observation;
        public HashSet<(long Revision, int X, int Y)> Rejected { get; } = [];
    }
}
