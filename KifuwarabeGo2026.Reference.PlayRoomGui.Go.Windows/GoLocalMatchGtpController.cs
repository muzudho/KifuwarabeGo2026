namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using System.Globalization;
using System.Text.Json;
using KifuwarabeGo2026.FormalAdapter.Gtp.Client;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

public enum GoLocalMatchActionKind { Play, Pass, Resign }

public sealed record GoLocalMatchAction(GoLocalMatchActionKind Kind, GoStone Stone, GoPoint? Point = null);

/// <summary>保存済みPlayer Engine接続を専用HostのLocal Matchへ接続します。</summary>
public sealed class GoLocalMatchGtpController : IAsyncDisposable
{
    private readonly GoPlayRoomLaunchPlan _plan;
    private readonly Dictionary<GoStone, GtpEngineClient> _engines = [];

    public GoLocalMatchGtpController(GoPlayRoomLaunchPlan plan) => _plan = plan;

    public bool HasEngine(GoStone stone) => _engines.ContainsKey(stone);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var connection in _plan.PlayerConnections)
        {
            var stone = GoLocalMatchSession.RoleStone(connection.RoleId);
            if (stone is null || _engines.ContainsKey(stone.Value)) continue;
            var engine = new GtpEngineClient(new GtpEngineSettings(
                connection.DisplayName,
                connection.ExecutablePath,
                connection.WorkingDirectory,
                connection.Arguments,
                connection.EnableGtpLog,
                stone == GoStone.Black ? "[black-play-room]" : "[white-play-room]",
                ReadOptions(connection),
                "play",
                "player"), TimeSpan.FromSeconds(10));
            _engines.Add(stone.Value, engine);
            await engine.StartAsync(cancellationToken);
            await engine.SendCommandExpectSuccessAsync($"boardsize {_plan.BoardSize}", cancellationToken);
            await engine.SendCommandExpectSuccessAsync("clear_board", cancellationToken);
            await engine.SendCommandExpectSuccessAsync($"komi {_plan.Komi.ToString(CultureInfo.InvariantCulture)}", cancellationToken);
            foreach (var setup in _plan.SetupStones)
                await engine.SendCommandExpectSuccessAsync(
                    $"play {Color(setup.Stone)} {Vertex(setup.Point, _plan.BoardSize)}",
                    cancellationToken);
        }
    }

    public async Task<GoLocalMatchAction> PlayHumanAsync(
        GoStone stone,
        GoPoint point,
        CancellationToken cancellationToken = default)
    {
        await SendToAllAsync($"play {Color(stone)} {Vertex(point, _plan.BoardSize)}", cancellationToken);
        return new(GoLocalMatchActionKind.Play, stone, point);
    }

    public async Task<GoLocalMatchAction> PassHumanAsync(
        GoStone stone,
        CancellationToken cancellationToken = default)
    {
        await SendToAllAsync($"play {Color(stone)} pass", cancellationToken);
        return new(GoLocalMatchActionKind.Pass, stone);
    }

    public async Task<GoLocalMatchAction> GenerateMoveAsync(
        GoStone stone,
        CancellationToken cancellationToken = default)
    {
        if (!_engines.TryGetValue(stone, out var engine))
            throw new InvalidOperationException($"No GTP Player Engine is connected for {stone}.");
        var response = await engine.SendCommandAsync($"genmove {Color(stone)}", cancellationToken);
        response.ThrowIfError("genmove");
        var move = response.Payload.Trim();
        var action = move.Equals("pass", StringComparison.OrdinalIgnoreCase)
            ? new GoLocalMatchAction(GoLocalMatchActionKind.Pass, stone)
            : move.Equals("resign", StringComparison.OrdinalIgnoreCase)
                ? new GoLocalMatchAction(GoLocalMatchActionKind.Resign, stone)
                : TryParseVertex(move, _plan.BoardSize, out var point)
                    ? new GoLocalMatchAction(GoLocalMatchActionKind.Play, stone, point)
                    : throw new InvalidOperationException($"GTP returned invalid vertex '{move}'.");

        var synchronizationCommand = action.Kind switch
        {
            GoLocalMatchActionKind.Play => $"play {Color(stone)} {Vertex(action.Point!.Value, _plan.BoardSize)}",
            GoLocalMatchActionKind.Pass => $"play {Color(stone)} pass",
            _ => null,
        };
        if (synchronizationCommand is not null)
            await SendToAllExceptAsync(stone, synchronizationCommand, cancellationToken);
        return action;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var engine in _engines.Values)
            await engine.DisposeAsync();
        _engines.Clear();
    }

    private async Task SendToAllAsync(string command, CancellationToken cancellationToken)
    {
        foreach (var engine in _engines.Values)
            await engine.SendCommandExpectSuccessAsync(command, cancellationToken);
    }

    private async Task SendToAllExceptAsync(GoStone excluded, string command, CancellationToken cancellationToken)
    {
        foreach (var pair in _engines)
            if (pair.Key != excluded)
                await pair.Value.SendCommandExpectSuccessAsync(command, cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> ReadOptions(GoPlayerConnectionPlan connection)
    {
        if (connection.EngineOptions is null) return new Dictionary<string, string>();
        using var document = JsonDocument.Parse(connection.EngineOptions.Content);
        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement.EnumerateObject().ToDictionary(
                property => property.Name,
                property => property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? ""
                    : property.Value.ToString(),
                StringComparer.Ordinal)
            : new Dictionary<string, string>();
    }

    public static string Vertex(GoPoint point, int boardSize)
    {
        var column = point.X >= 8 ? point.X + 1 : point.X;
        return $"{(char)('A' + column)}{boardSize - point.Y}";
    }

    public static bool TryParseVertex(string vertex, int boardSize, out GoPoint point)
    {
        point = default;
        if (vertex.Length < 2) return false;
        var column = char.ToUpperInvariant(vertex[0]);
        if (column is < 'A' or > 'T' || column == 'I') return false;
        var x = column - 'A' - (column > 'I' ? 1 : 0);
        if (!int.TryParse(vertex[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var row)) return false;
        var y = boardSize - row;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        point = new GoPoint(x, y);
        return true;
    }

    private static string Color(GoStone stone) => stone == GoStone.Black ? "black" : "white";
}
