namespace KifuwarabeGo2026.Reference.PlaySpace.Go;

using System.Collections.Concurrent;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>中国式面積計算を採用する通常囲碁のProtocol S参照実装です。</summary>
public sealed class GoPlaySpaceProtocol : IPlaySpaceProtocol
{
    private const string JsonMediaType = "application/json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<PlaySpaceSessionId, Session> _sessions = new();

    public ValueTask<ProtocolResponse<PlaySpaceDescriptor>> DescribeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<PlaySpaceDescriptor>.Success(new(
            new PlaySpaceTypeId("org.kifuwarabe.games.go"),
            "通常囲碁",
            ContractVersion.V1_0,
            "KifuwarabeGo2026.Reference.PlaySpace.Go",
            typeof(GoPlaySpaceProtocol).Assembly.GetName().Version?.ToString() ?? "4.0.0",
            ["explicit-setup", "move-history-observation", "simple-ko", "positional-superko", "two-pass-scoring", "resignation", "chinese-area-scoring"])));
    }

    public ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<ContractDocument>.Success(Document(
            GoSchemas.Configuration,
            """
            {
              "$schema":"https://json-schema.org/draft/2020-12/schema",
              "$id":"org.kifuwarabe.games.go.configuration.v1",
              "type":"object",
              "required":["version","boardSize","komi","ruleset","startingPlayer"],
              "properties":{
                "version":{"const":1},
                "boardSize":{"enum":[9,13,19]},
                "komi":{"type":"number"},
                "ruleset":{"const":"chinese-area"},
                "startingPlayer":{"enum":["black","white"]},
                "setupStones":{"type":"array","items":{"type":"object","required":["x","y","color"],"properties":{"x":{"type":"integer","minimum":0},"y":{"type":"integer","minimum":0},"color":{"enum":["black","white"]}},"additionalProperties":false}}
              },
              "additionalProperties":false
            }
            """)));
    }

    public ValueTask<ProtocolResponse<PlaySpaceConfigurationValidation>> ValidateConfigurationAsync(
        ValidatePlaySpaceConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = ParseConfiguration(request.Configuration);
        return ValueTask.FromResult(ProtocolResponse<PlaySpaceConfigurationValidation>.Success(
            parsed.IsSuccess
                ? new PlaySpaceConfigurationValidation(true, [])
                : new PlaySpaceConfigurationValidation(false, [parsed.Error!])));
    }

    public ValueTask<ProtocolResponse<PlaySpaceSessionCreated>> CreateSessionAsync(
        CreatePlaySpaceSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = ParseConfiguration(request.Configuration);
        if (!parsed.IsSuccess)
            return ValueTask.FromResult(ProtocolResponse<PlaySpaceSessionCreated>.Failure(parsed.Error!));

        var configuration = parsed.Value!;
        var board = new GoBoard(configuration.BoardSize);
        foreach (var setup in configuration.SetupStones ?? [])
        {
            TryStone(setup.Color, out var stone);
            board.TrySetSetupStone(new(setup.X, setup.Y), stone);
        }
        TryStone(configuration.StartingPlayer, out var nextToPlay);
        var session = new Session
        {
            Board = board,
            NextToPlay = nextToPlay,
            Komi = configuration.Komi,
        };
        foreach (var setup in configuration.SetupStones ?? [])
        {
            var point = new GoPointDocument(setup.X, setup.Y);
            (string.Equals(setup.Color, "black", StringComparison.OrdinalIgnoreCase)
                ? session.SetupBlack
                : session.SetupWhite).Add(point);
        }
        session.PositionHistory.Add(board.PositionKey());
        var sessionId = new PlaySpaceSessionId(Guid.NewGuid().ToString("N"));
        session.SessionId = sessionId;
        if (!_sessions.TryAdd(sessionId, session))
            return ValueTask.FromResult(Failure<PlaySpaceSessionCreated>("session-id-conflict", "Could not allocate a session ID."));
        return ValueTask.FromResult(ProtocolResponse<PlaySpaceSessionCreated>.Success(new(
            sessionId,
            CreateSnapshot(session))));
    }

    public ValueTask<ProtocolResponse<PlaySpaceSnapshot>> GetSnapshotAsync(
        GetPlaySpaceSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(request.SessionId, out var session))
            return ValueTask.FromResult(SessionNotFound<PlaySpaceSnapshot>(request.SessionId));
        lock (session.Sync)
            return ValueTask.FromResult(ProtocolResponse<PlaySpaceSnapshot>.Success(CreateSnapshot(session)));
    }

    public ValueTask<ProtocolResponse<PlaySpaceActionApplied>> ApplyActionAsync(
        ApplyPlaySpaceActionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(request.SessionId, out var session))
            return ValueTask.FromResult(SessionNotFound<PlaySpaceActionApplied>(request.SessionId));
        var parsed = ParseAction(request.Action);
        if (!parsed.IsSuccess)
            return ValueTask.FromResult(ProtocolResponse<PlaySpaceActionApplied>.Failure(parsed.Error!));

        lock (session.Sync)
        {
            if (request.ExpectedRevision != session.Revision)
                return ValueTask.FromResult(Failure<PlaySpaceActionApplied>(
                    "revision-conflict",
                    $"Expected revision {request.ExpectedRevision}, but current revision is {session.Revision}."));
            if (session.IsTerminal)
                return ValueTask.FromResult(Rejected(session, "game-already-terminal", "The game is already terminal."));

            var action = parsed.Value!;
            if (!TryStone(action.Player, out var player) || player != session.NextToPlay)
                return ValueTask.FromResult(Rejected(session, "wrong-player", "The action player does not have the turn."));

            var actionType = action.Type.ToLowerInvariant();
            var captured = 0;
            GoPoint? point = null;
            if (actionType == "play" && action.X is { } x && action.Y is { } y)
            {
                point = new(x, y);
                var trial = session.Board.Clone();
                if (!trial.TryPlaceStone(point.Value, player, session.KoPoint, out captured, out var nextKoPoint))
                    return ValueTask.FromResult(Rejected(session, "illegal-action", "The stone cannot be played at that point."));
                var positionKey = trial.PositionKey();
                if (session.PositionHistory.Contains(positionKey))
                    return ValueTask.FromResult(Rejected(session, "positional-superko", "The move would repeat an earlier board position."));
                session.Board = trial;
                session.PositionHistory.Add(positionKey);
                session.KoPoint = nextKoPoint;
                session.ConsecutivePasses = 0;
                if (player == GoStone.Black) session.BlackCaptures += captured;
                else session.WhiteCaptures += captured;
            }
            else if (actionType == "pass")
            {
                session.KoPoint = null;
                session.ConsecutivePasses++;
            }
            else if (actionType == "resign")
            {
                session.KoPoint = null;
                session.ConsecutivePasses = 0;
                session.IsTerminal = true;
                session.Outcome = new(
                    1,
                    "winner",
                    StoneName(Opposite(player)),
                    "resignation");
            }
            else
            {
                return ValueTask.FromResult(Rejected(session, "unsupported-action", "Supported actions are play, pass, and resign."));
            }

            if (actionType is "play" or "pass")
                session.MoveHistory.Add(new(
                    StoneName(player),
                    actionType,
                    point?.X,
                    point?.Y));

            session.NextToPlay = Opposite(player);
            session.Revision++;
            if (!session.IsTerminal && session.ConsecutivePasses >= 2)
                FinalizeByAreaScore(session);
            var eventDocument = Document(GoSchemas.Event, new GoEventDocument(
                1,
                actionType,
                StoneName(player),
                point is { } played ? new(played.X, played.Y) : null,
                captured,
                StoneName(session.NextToPlay)));
            return ValueTask.FromResult(ProtocolResponse<PlaySpaceActionApplied>.Success(new(
                true,
                CreateSnapshot(session),
                [eventDocument],
                null)));
        }
    }

    public ValueTask<ProtocolResponse<PlaySpaceSessionClosed>> CloseSessionAsync(
        ClosePlaySpaceSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_sessions.TryRemove(request.SessionId, out _)
            ? ProtocolResponse<PlaySpaceSessionClosed>.Success(new(request.SessionId))
            : SessionNotFound<PlaySpaceSessionClosed>(request.SessionId));
    }

    private static void FinalizeByAreaScore(Session session)
    {
        var (blackArea, whiteArea) = session.Board.ScoreArea();
        var blackScore = (decimal)blackArea;
        var whiteScore = whiteArea + session.Komi;
        var difference = blackScore - whiteScore;
        session.IsTerminal = true;
        session.Outcome = difference switch
        {
            > 0 => new(1, "winner", "black", "two-passes-area-score", blackScore, whiteScore, difference),
            < 0 => new(1, "winner", "white", "two-passes-area-score", blackScore, whiteScore, -difference),
            _ => new(1, "draw", null, "two-passes-area-score", blackScore, whiteScore, 0),
        };
    }

    private static ProtocolResponse<GoConfigurationDocument> ParseConfiguration(ContractDocument document)
    {
        if (document.MediaType != JsonMediaType || document.SchemaId != GoSchemas.Configuration)
            return Failure<GoConfigurationDocument>("unsupported-configuration-document", "The configuration document type is not supported.");
        GoConfigurationDocument? configuration;
        try { configuration = JsonSerializer.Deserialize<GoConfigurationDocument>(document.Content, JsonOptions); }
        catch (JsonException exception) { return Failure<GoConfigurationDocument>("invalid-configuration-json", exception.Message); }
        if (configuration is null) return Failure<GoConfigurationDocument>("empty-configuration", "The configuration document is empty.");
        if (configuration.Version != 1) return Failure<GoConfigurationDocument>("unsupported-configuration-version", "Only Go configuration version 1 is supported.");
        if (configuration.BoardSize is not (9 or 13 or 19)) return Failure<GoConfigurationDocument>("invalid-board-size", "BoardSize must be 9, 13, or 19.");
        if (configuration.Komi < -100 || configuration.Komi > 100) return Failure<GoConfigurationDocument>("invalid-komi", "Komi must be between -100 and 100.");
        if (configuration.Ruleset != "chinese-area") return Failure<GoConfigurationDocument>("unsupported-ruleset", "Only chinese-area is supported in v1.");
        if (!TryStone(configuration.StartingPlayer, out _)) return Failure<GoConfigurationDocument>("invalid-starting-player", "StartingPlayer must be black or white.");
        var occupied = new HashSet<GoPoint>();
        foreach (var setup in configuration.SetupStones ?? [])
        {
            var point = new GoPoint(setup.X, setup.Y);
            if (point.X < 0 || point.X >= configuration.BoardSize || point.Y < 0 || point.Y >= configuration.BoardSize)
                return Failure<GoConfigurationDocument>("setup-point-outside-board", $"Setup point ({point.X},{point.Y}) is outside the board.");
            if (!TryStone(setup.Color, out _)) return Failure<GoConfigurationDocument>("invalid-setup-color", "A setup stone color must be black or white.");
            if (!occupied.Add(point)) return Failure<GoConfigurationDocument>("duplicate-setup-point", $"Setup point ({point.X},{point.Y}) is duplicated.");
        }
        return ProtocolResponse<GoConfigurationDocument>.Success(configuration);
    }

    private static ProtocolResponse<GoActionDocument> ParseAction(ContractDocument document)
    {
        if (document.MediaType != JsonMediaType || document.SchemaId != GoSchemas.Action)
            return Failure<GoActionDocument>("unsupported-action-document", "The action document type is not supported.");
        try
        {
            var action = JsonSerializer.Deserialize<GoActionDocument>(document.Content, JsonOptions);
            return action is null
                ? Failure<GoActionDocument>("empty-action", "The action document is empty.")
                : action.Version != 1
                    ? Failure<GoActionDocument>("unsupported-action-version", "Only Go action version 1 is supported.")
                    : ProtocolResponse<GoActionDocument>.Success(action);
        }
        catch (JsonException exception) { return Failure<GoActionDocument>("invalid-action-json", exception.Message); }
    }

    private static PlaySpaceSnapshot CreateSnapshot(Session session)
    {
        var black = new List<GoPointDocument>();
        var white = new List<GoPointDocument>();
        foreach (var (point, stone) in session.Board.EnumerateStones())
            (stone == GoStone.Black ? black : white).Add(new(point.X, point.Y));
        var state = Document(GoSchemas.State, new GoStateDocument(
            1,
            session.Board.Size,
            "chinese-area",
            session.Komi,
            black,
            white,
            StoneName(session.NextToPlay),
            session.BlackCaptures,
            session.WhiteCaptures,
            session.ConsecutivePasses,
            session.KoPoint is { } ko ? new(ko.X, ko.Y) : null,
            session.IsTerminal,
            session.SetupBlack,
            session.SetupWhite,
            session.MoveHistory));
        var outcome = session.Outcome is null ? null : Document(GoSchemas.Outcome, session.Outcome);
        return new(session.SessionId, session.Revision, state, session.IsTerminal, outcome);
    }

    private static ProtocolResponse<PlaySpaceActionApplied> Rejected(Session session, string code, string message) =>
        ProtocolResponse<PlaySpaceActionApplied>.Success(new(false, CreateSnapshot(session), [], new(code, message)));

    private static ContractDocument Document<T>(string schemaId, T value) =>
        Document(schemaId, JsonSerializer.Serialize(value, JsonOptions));

    private static ContractDocument Document(string schemaId, string content) => new(JsonMediaType, schemaId, content);

    private static ProtocolResponse<T> SessionNotFound<T>(PlaySpaceSessionId id) =>
        Failure<T>("session-not-found", $"Play-space session '{id}' was not found.");

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new(code, message));

    private static bool TryStone(string? value, out GoStone stone)
    {
        if (string.Equals(value, "black", StringComparison.OrdinalIgnoreCase)) { stone = GoStone.Black; return true; }
        if (string.Equals(value, "white", StringComparison.OrdinalIgnoreCase)) { stone = GoStone.White; return true; }
        stone = GoStone.Empty;
        return false;
    }

    private static string StoneName(GoStone stone) => stone == GoStone.Black ? "black" : "white";
    private static GoStone Opposite(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private sealed class Session
    {
        public object Sync { get; } = new();
        public PlaySpaceSessionId SessionId { get; set; }
        public required GoBoard Board { get; set; }
        public required GoStone NextToPlay { get; set; }
        public required decimal Komi { get; init; }
        public HashSet<string> PositionHistory { get; } = [];
        public List<GoPointDocument> SetupBlack { get; } = [];
        public List<GoPointDocument> SetupWhite { get; } = [];
        public List<GoMoveDocument> MoveHistory { get; } = [];
        public GoPoint? KoPoint { get; set; }
        public int BlackCaptures { get; set; }
        public int WhiteCaptures { get; set; }
        public int ConsecutivePasses { get; set; }
        public long Revision { get; set; }
        public bool IsTerminal { get; set; }
        public GoOutcomeDocument? Outcome { get; set; }
    }
}
