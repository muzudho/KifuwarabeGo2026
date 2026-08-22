namespace KifuwarabeGo2026.Reference.Communication.Gtp;

using System.Globalization;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

/// <summary>きふわらべ固有の原子的初期配置拡張を使って通常囲碁を指すProtocol Pアダプターです。</summary>
public sealed class KifuwarabeGtpPlayerProtocol(
    IGtpCommandTransport transport,
    PlayerEngineId engineId,
    string displayName = "きふわらべGTPプレイヤー") : IPlayerProtocol
{
    public static readonly PlaySpaceTypeId GoTypeId = new("org.kifuwarabe.games.go");
    private const string GoStateSchema = "org.kifuwarabe.games.go.state.v1";
    private const string GoActionSchema = "org.kifuwarabe.games.go.action.v1";
    private readonly IGtpCommandTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly object _sync = new();
    private ActiveBinding? _binding;
    private GtpInitialPositionCapabilities? _initialPositionCapabilities;

    public ValueTask<ProtocolResponse<PlayerEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<PlayerEngineDescriptor>.Success(new(
            engineId,
            displayName,
            ContractVersion.V1_0,
            "KifuwarabeGo2026.Reference.Communication.Gtp",
            typeof(KifuwarabeGtpPlayerProtocol).Assembly.GetName().Version?.ToString() ?? "4.0.0",
            [GoTypeId],
            ["gtp", "kifuwarabe-atomic-position", "standard-static-position-fallback", "single-session"])));

    public async ValueTask<ProtocolResponse<PlayerSessionStarted>> StartSessionAsync(
        PlayerSessionStartRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InitialObservation.PlaySpaceTypeId != GoTypeId)
            return Failure<PlayerSessionStarted>("unsupported-play-space", "The GTP adapter supports normal Go only.");
        lock (_sync)
        {
            if (_binding is not null)
                return Failure<PlayerSessionStarted>("gtp-engine-busy", "One GTP transport can serve only one active player binding.");
            _binding = new(request.BindingId, request.RoleId);
        }
        var synchronized = await SynchronizeAsync(request.InitialObservation, cancellationToken);
        if (!synchronized.IsSuccess)
        {
            lock (_sync) _binding = null;
            return ProtocolResponse<PlayerSessionStarted>.Failure(synchronized.Error!);
        }
        return ProtocolResponse<PlayerSessionStarted>.Success(new(request.BindingId));
    }

    public async ValueTask<ProtocolResponse<PlayerActionSelected>> SelectActionAsync(
        PlayerActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var binding = GetBinding(request.BindingId);
        if (!binding.IsSuccess || binding.Value is null)
            return ProtocolResponse<PlayerActionSelected>.Failure(binding.Error!);
        var response = await _transport.SendAsync($"genmove {request.RoleId}", cancellationToken);
        if (!response.IsSuccess)
            return Failure<PlayerActionSelected>("gtp-genmove-failed", response.Payload);
        var action = ParseGeneratedMove(response.Payload, request.RoleId, request.Observation);
        if (!action.IsSuccess || action.Value is null)
            return ProtocolResponse<PlayerActionSelected>.Failure(action.Error!);
        lock (_sync)
        {
            if (_binding is not null) _binding.PendingAction = action.Value;
        }
        return ProtocolResponse<PlayerActionSelected>.Success(new(
            request.BindingId,
            request.Observation.Revision,
            action.Value));
    }

    public async ValueTask<ProtocolResponse<PlayerActionNotified>> NotifyActionAsync(
        PlayerActionNotification notification,
        CancellationToken cancellationToken = default)
    {
        var found = GetBinding(notification.BindingId);
        if (!found.IsSuccess || found.Value is null)
            return ProtocolResponse<PlayerActionNotified>.Failure(found.Error!);
        var binding = found.Value;
        var isOwnPending = binding.PendingAction == notification.Action;
        if (isOwnPending && notification.WasAccepted)
        {
            lock (_sync) { if (_binding is not null) _binding.PendingAction = null; }
        }
        else if (!notification.WasAccepted)
        {
            var synchronized = await SynchronizeAsync(notification.Observation, cancellationToken);
            if (!synchronized.IsSuccess)
                return ProtocolResponse<PlayerActionNotified>.Failure(synchronized.Error!);
            lock (_sync) { if (_binding is not null) _binding.PendingAction = null; }
        }
        else
        {
            if (IsResignation(notification.Action))
                return ProtocolResponse<PlayerActionNotified>.Success(new(
                    notification.BindingId,
                    notification.Observation.Revision));
            var command = ToPlayCommand(notification.Action, notification.Observation);
            if (!command.IsSuccess || command.Value is null)
                return ProtocolResponse<PlayerActionNotified>.Failure(command.Error!);
            var played = await _transport.SendAsync(command.Value, cancellationToken);
            if (!played.IsSuccess)
                return Failure<PlayerActionNotified>("gtp-play-failed", played.Payload);
        }
        return ProtocolResponse<PlayerActionNotified>.Success(new(notification.BindingId, notification.Observation.Revision));
    }

    public ValueTask<ProtocolResponse<PlayerStateNotified>> NotifyStateAsync(
        PlayerStateNotification notification,
        CancellationToken cancellationToken = default)
    {
        var found = GetBinding(notification.BindingId);
        return ValueTask.FromResult(found.IsSuccess
            ? ProtocolResponse<PlayerStateNotified>.Success(new(
                notification.BindingId,
                notification.Observation.Revision,
                notification.Observation.OperationRevision))
            : ProtocolResponse<PlayerStateNotified>.Failure(found.Error!));
    }

    public ValueTask<ProtocolResponse<PlayerSessionEnded>> EndSessionAsync(
        PlayerSessionEndRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_binding?.BindingId != request.BindingId)
                return ValueTask.FromResult(Failure<PlayerSessionEnded>("player-binding-not-found", $"Binding '{request.BindingId}' is not active."));
            _binding = null;
        }
        return ValueTask.FromResult(ProtocolResponse<PlayerSessionEnded>.Success(new(request.BindingId)));
    }

    private async ValueTask<ProtocolResponse<bool>> SynchronizeAsync(
        PlayerGameObservation observation,
        CancellationToken cancellationToken)
    {
        var parsed = ParseState(observation);
        if (!parsed.IsSuccess || parsed.Value is null)
            return ProtocolResponse<bool>.Failure(parsed.Error!);
        var state = parsed.Value;
        var capabilities = await ResolveInitialPositionCapabilitiesAsync(cancellationToken);
        var mode = capabilities.SupportsAtomic
            ? InitialPositionMode.KifuwarabeAtomic
            : capabilities.SupportsFixedHandicap && IsStandardFixedHandicap(state)
                ? InitialPositionMode.FixedHandicap
            : capabilities.SupportsSetFreeHandicap && IsBlackHandicap(state)
                ? InitialPositionMode.SetFreeHandicap
                : InitialPositionMode.StandardSequentialPlay;
        var commands = new List<string>
        {
            $"boardsize {state.BoardSize}",
            $"komi {state.Komi.ToString(CultureInfo.InvariantCulture)}",
        };
        if (mode == InitialPositionMode.KifuwarabeAtomic)
        {
            commands.Add("kfw-begin-position");
            commands.AddRange(state.Black.Select(point => $"kfw-add-black {FormatVertex(point.X, point.Y, state.BoardSize)}"));
            commands.AddRange(state.White.Select(point => $"kfw-add-white {FormatVertex(point.X, point.Y, state.BoardSize)}"));
            commands.Add($"kfw-set-to-play {state.NextToPlay}");
            commands.Add("kfw-commit-position");
        }
        else if (mode is InitialPositionMode.FixedHandicap or InitialPositionMode.SetFreeHandicap)
        {
            commands.Insert(1, "clear_board");
            commands.Add(mode == InitialPositionMode.FixedHandicap
                ? $"fixed_handicap {state.Black.Count}"
                : $"set_free_handicap {string.Join(' ', state.Black.Select(point => FormatVertex(point.X, point.Y, state.BoardSize)))}");
        }
        else
        {
            commands.Insert(1, "clear_board");
            commands.AddRange(state.Black.Select(point => $"play black {FormatVertex(point.X, point.Y, state.BoardSize)}"));
            commands.AddRange(state.White.Select(point => $"play white {FormatVertex(point.X, point.Y, state.BoardSize)}"));
        }
        foreach (var command in commands)
        {
            var response = await _transport.SendAsync(command, cancellationToken);
            if (!response.IsSuccess)
            {
                if (mode == InitialPositionMode.KifuwarabeAtomic)
                    await _transport.SendAsync("kfw-abort-position", CancellationToken.None);
                return Failure<bool>("gtp-position-sync-failed", $"Command '{command}' failed: {response.Payload}");
            }
            if (mode == InitialPositionMode.FixedHandicap && command.StartsWith("fixed_handicap ", StringComparison.Ordinal) &&
                !FixedHandicapResponseMatches(response.Payload, state))
            {
                await _transport.SendAsync("clear_board", CancellationToken.None);
                return Failure<bool>("gtp-fixed-handicap-mismatch", "fixed_handicap returned vertices that do not match the requested setup.");
            }
        }
        return ProtocolResponse<bool>.Success(true);
    }

    private async ValueTask<GtpInitialPositionCapabilities> ResolveInitialPositionCapabilitiesAsync(CancellationToken cancellationToken)
    {
        if (_initialPositionCapabilities is { } resolved) return resolved;
        var atomic = await SupportsCommandAsync("kfw-begin-position", cancellationToken);
        var fixedHandicap = !atomic && await SupportsCommandAsync("fixed_handicap", cancellationToken);
        var setFreeHandicap = !atomic && await SupportsCommandAsync("set_free_handicap", cancellationToken);
        _initialPositionCapabilities = new(atomic, fixedHandicap, setFreeHandicap);
        return _initialPositionCapabilities;
    }

    private async ValueTask<bool> SupportsCommandAsync(string command, CancellationToken cancellationToken)
    {
        var response = await _transport.SendAsync($"known_command {command}", cancellationToken);
        return response.IsSuccess && bool.TryParse(response.Payload.Trim(), out var supported) && supported;
    }

    private static bool IsBlackHandicap(GoState state) =>
        state.Black.Count > 0 && state.White.Count == 0 && state.NextToPlay == "white";

    private static bool IsStandardFixedHandicap(GoState state)
    {
        if (!IsBlackHandicap(state) || state.Black.Count is < 2 or > 9 || state.BoardSize is not (9 or 13 or 19))
            return false;
        var low = state.BoardSize == 9 ? 2 : 3;
        var high = state.BoardSize - low - 1;
        var middle = state.BoardSize / 2;
        var lowerLeft = new Point(low, high);
        var upperRight = new Point(high, low);
        var upperLeft = new Point(low, low);
        var lowerRight = new Point(high, high);
        var middleLeft = new Point(low, middle);
        var middleRight = new Point(high, middle);
        var upperMiddle = new Point(middle, low);
        var lowerMiddle = new Point(middle, high);
        var center = new Point(middle, middle);
        IReadOnlyList<Point> expected = state.Black.Count switch
        {
            2 => [lowerLeft, upperRight],
            3 => [lowerLeft, upperRight, upperLeft],
            4 => [lowerLeft, upperRight, upperLeft, lowerRight],
            5 => [lowerLeft, upperRight, upperLeft, lowerRight, center],
            6 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight],
            7 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, center],
            8 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, upperMiddle, lowerMiddle],
            9 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, upperMiddle, lowerMiddle, center],
            _ => [],
        };
        return expected.ToHashSet().SetEquals(state.Black);
    }

    private static bool FixedHandicapResponseMatches(string payload, GoState state)
    {
        var actual = new HashSet<Point>();
        foreach (var vertex in payload.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseVertex(vertex, state.BoardSize, out var x, out var y) || !actual.Add(new(x, y))) return false;
        }
        return actual.SetEquals(state.Black);
    }

    private ProtocolResponse<ActiveBinding> GetBinding(PlayerBindingId bindingId)
    {
        lock (_sync)
            return _binding?.BindingId == bindingId
                ? ProtocolResponse<ActiveBinding>.Success(_binding)
                : Failure<ActiveBinding>("player-binding-not-found", $"Binding '{bindingId}' is not active.");
    }

    private static ProtocolResponse<ContractDocument> ParseGeneratedMove(
        string payload,
        string roleId,
        PlayerGameObservation observation)
    {
        var move = payload.Trim();
        if (move.Equals("pass", StringComparison.OrdinalIgnoreCase) || move.Equals("resign", StringComparison.OrdinalIgnoreCase))
            return ProtocolResponse<ContractDocument>.Success(Action(move.ToLowerInvariant(), roleId));
        var state = ParseState(observation);
        if (!state.IsSuccess || state.Value is null || !TryParseVertex(move, state.Value.BoardSize, out var x, out var y))
            return Failure<ContractDocument>("invalid-gtp-vertex", $"GTP returned invalid vertex '{move}'.");
        return ProtocolResponse<ContractDocument>.Success(Action("play", roleId, x, y));
    }

    private static ProtocolResponse<string> ToPlayCommand(ContractDocument action, PlayerGameObservation observation)
    {
        try
        {
            using var json = JsonDocument.Parse(action.Content);
            var root = json.RootElement;
            var type = root.GetProperty("type").GetString();
            var player = root.GetProperty("player").GetString();
            if (type == "pass") return ProtocolResponse<string>.Success($"play {player} pass");
            var state = ParseState(observation);
            if (!state.IsSuccess || state.Value is null) return ProtocolResponse<string>.Failure(state.Error!);
            var vertex = FormatVertex(root.GetProperty("x").GetInt32(), root.GetProperty("y").GetInt32(), state.Value.BoardSize);
            return ProtocolResponse<string>.Success($"play {player} {vertex}");
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return Failure<string>("invalid-action-document", exception.Message);
        }
    }

    private static bool IsResignation(ContractDocument action)
    {
        try
        {
            using var json = JsonDocument.Parse(action.Content);
            return json.RootElement.GetProperty("type").GetString() == "resign";
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return false;
        }
    }

    private static ProtocolResponse<GoState> ParseState(PlayerGameObservation observation)
    {
        if (observation.State.MediaType != "application/json" || observation.State.SchemaId != GoStateSchema)
            return Failure<GoState>("unsupported-go-state", $"State schema '{observation.State.SchemaId}' is not supported.");
        try
        {
            using var json = JsonDocument.Parse(observation.State.Content);
            var root = json.RootElement;
            return ProtocolResponse<GoState>.Success(new(
                root.GetProperty("boardSize").GetInt32(),
                root.GetProperty("komi").GetDecimal(),
                root.GetProperty("nextToPlay").GetString()!,
                ReadPoints(root.GetProperty("black")),
                ReadPoints(root.GetProperty("white"))));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return Failure<GoState>("invalid-go-state", exception.Message);
        }
    }

    private static IReadOnlyList<Point> ReadPoints(JsonElement array) =>
        array.EnumerateArray().Select(value => new Point(
            value.GetProperty("x").GetInt32(),
            value.GetProperty("y").GetInt32())).ToArray();

    private static ContractDocument Action(string type, string player, int? x = null, int? y = null) => new(
        "application/json",
        GoActionSchema,
        JsonSerializer.Serialize(new { version = 1, type, player, x, y }, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        }));

    private static string FormatVertex(int x, int y, int boardSize)
    {
        var columnIndex = x >= 8 ? x + 1 : x;
        return $"{(char)('A' + columnIndex)}{boardSize - y}";
    }

    private static bool TryParseVertex(string vertex, int boardSize, out int x, out int y)
    {
        x = y = default;
        if (vertex.Length < 2) return false;
        var column = char.ToUpperInvariant(vertex[0]);
        if (column is < 'A' or > 'T' || column == 'I') return false;
        x = column - 'A' - (column > 'I' ? 1 : 0);
        if (!int.TryParse(vertex[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var row)) return false;
        y = boardSize - row;
        return x >= 0 && x < boardSize && y >= 0 && y < boardSize;
    }

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new(code, message));

    private sealed record Point(int X, int Y);
    private sealed record GoState(int BoardSize, decimal Komi, string NextToPlay, IReadOnlyList<Point> Black, IReadOnlyList<Point> White);
    private sealed record GtpInitialPositionCapabilities(bool SupportsAtomic, bool SupportsFixedHandicap, bool SupportsSetFreeHandicap);
    private enum InitialPositionMode { KifuwarabeAtomic, FixedHandicap, SetFreeHandicap, StandardSequentialPlay }
    private sealed class ActiveBinding(PlayerBindingId bindingId, string roleId)
    {
        public PlayerBindingId BindingId { get; } = bindingId;
        public string RoleId { get; } = roleId;
        public ContractDocument? PendingAction { get; set; }
    }
}
