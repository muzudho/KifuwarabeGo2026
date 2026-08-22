namespace KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>Protocol Sで利用できるポン抜きプレイスペースの参照実装です。</summary>
public sealed class PonnukiPlaySpaceProtocol : IPlaySpaceProtocol
{
    private const string JsonMediaType = "application/json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<PlaySpaceSessionId, Session> _sessions = new();

    public ValueTask<ProtocolResponse<PlaySpaceDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<PlaySpaceDescriptor>.Success(new PlaySpaceDescriptor(
            new PlaySpaceTypeId(GameOasisOfficialNames.Ponnuki),
            "ポン抜き",
            ContractVersion.V1_0,
            "KifuwarabeGo2026.Reference.PlaySpace.Ponnuki",
            typeof(PonnukiPlaySpaceProtocol).Assembly.GetName().Version?.ToString() ?? "4.0.0",
            ["deterministic-seed", "explicit-setup", "optimistic-revision", "capture-target"]
        )));
    }

    public ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<ContractDocument>.Success(Document(
            PonnukiSchemas.Configuration,
            JsonSerializer.Serialize(new
            {
                type = "object",
                required = new[] { "version", "boardSize", "initialMoveCount", "captureTarget" },
                properties = new
                {
                    version = new { type = "integer", @const = 1 },
                    boardSize = new { type = "integer", @enum = new[] { 9, 13, 19 }, @default = 9 },
                    initialMoveCount = new { type = "integer", minimum = 0, description = "Maximum is boardSize squared divided by four.", @default = 20 },
                    randomSeed = new { type = new[] { "integer", "null" }, description = "A generated seed is returned in state when omitted." },
                    captureTarget = new { type = "integer", minimum = 1, @default = 20 },
                    startingPlayer = new { type = new[] { "string", "null" }, @enum = new string?[] { "black", "white", null } },
                    setupStones = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            required = new[] { "x", "y", "color" },
                            properties = new
                            {
                                x = new { type = "integer", minimum = 0 },
                                y = new { type = "integer", minimum = 0 },
                                color = new { type = "string", @enum = new[] { "black", "white" } },
                            },
                        },
                    },
                },
            }, JsonOptions))));
    }

    public ValueTask<ProtocolResponse<PlaySpaceConfigurationValidation>> ValidateConfigurationAsync(
        ValidatePlaySpaceConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = ParseConfiguration(request.Configuration);
        return ValueTask.FromResult(parsed.IsSuccess
            ? ProtocolResponse<PlaySpaceConfigurationValidation>.Success(
                new PlaySpaceConfigurationValidation(true, []))
            : ProtocolResponse<PlaySpaceConfigurationValidation>.Success(
                new PlaySpaceConfigurationValidation(false, [parsed.Error!]))) ;
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
        var seed = configuration.RandomSeed ?? RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        var session = CreateSession(configuration, seed);
        var sessionId = new PlaySpaceSessionId(Guid.NewGuid().ToString("N"));
        session.SessionId = sessionId;
        if (!_sessions.TryAdd(sessionId, session))
            return ValueTask.FromResult(Failure<PlaySpaceSessionCreated>("session-id-conflict", "Could not allocate a session ID."));

        return ValueTask.FromResult(ProtocolResponse<PlaySpaceSessionCreated>.Success(
            new PlaySpaceSessionCreated(sessionId, CreateSnapshot(session))));
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

            var captured = 0;
            PonnukiPoint? point = null;
            if (string.Equals(action.Type, "pass", StringComparison.OrdinalIgnoreCase))
            {
                session.KoPoint = null;
            }
            else if (string.Equals(action.Type, "play", StringComparison.OrdinalIgnoreCase) &&
                     action.X is { } x && action.Y is { } y)
            {
                point = new PonnukiPoint(x, y);
                if (!session.Board.TryPlaceStone(point.Value, player, session.KoPoint, out captured, out var nextKoPoint))
                    return ValueTask.FromResult(Rejected(session, "illegal-action", "The stone cannot be played at that point."));
                session.KoPoint = nextKoPoint;
            }
            else
            {
                return ValueTask.FromResult(Rejected(session, "unsupported-action", "Supported actions are play and pass."));
            }

            if (player == PonnukiStone.Black) session.BlackCaptures += captured;
            else session.WhiteCaptures += captured;
            session.NextToPlay = Opposite(player);
            session.Revision++;
            session.IsTerminal = session.BlackCaptures >= session.CaptureTarget || session.WhiteCaptures >= session.CaptureTarget;

            var eventDocument = Document(PonnukiSchemas.Event, new PonnukiEventDocument(
                1,
                action.Type.ToLowerInvariant(),
                StoneName(player),
                point is { } playedPoint ? new PonnukiPointDocument(playedPoint.X, playedPoint.Y) : null,
                captured,
                StoneName(session.NextToPlay)));
            return ValueTask.FromResult(ProtocolResponse<PlaySpaceActionApplied>.Success(
                new PlaySpaceActionApplied(true, CreateSnapshot(session), [eventDocument], null)));
        }
    }

    public ValueTask<ProtocolResponse<PlaySpaceSessionClosed>> CloseSessionAsync(
        ClosePlaySpaceSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_sessions.TryRemove(request.SessionId, out _)
            ? ProtocolResponse<PlaySpaceSessionClosed>.Success(new PlaySpaceSessionClosed(request.SessionId))
            : SessionNotFound<PlaySpaceSessionClosed>(request.SessionId));
    }

    private static Session CreateSession(PonnukiConfigurationDocument configuration, int seed)
    {
        var board = new PonnukiBoard(configuration.BoardSize);
        foreach (var setup in configuration.SetupStones ?? [])
        {
            TryStone(setup.Color, out var stone);
            board.TrySetSetupStone(new PonnukiPoint(setup.X, setup.Y), stone);
        }

        var random = new Random(seed);
        var nextToPlay = PonnukiStone.Black;
        PonnukiPoint? koPoint = null;
        for (var ply = 0; ply < configuration.InitialMoveCount; ply++)
        {
            var candidates = Enumerable.Range(0, configuration.BoardSize * configuration.BoardSize)
                .Select(index => new PonnukiPoint(index % configuration.BoardSize, index / configuration.BoardSize))
                .ToArray();
            random.Shuffle(candidates);
            var selected = false;
            foreach (var candidate in candidates)
            {
                var trial = board.Clone();
                if (!trial.TryPlaceStone(candidate, nextToPlay, koPoint, out _, out _)) continue;
                board.TryPlaceStone(candidate, nextToPlay, koPoint, out _, out koPoint);
                selected = true;
                break;
            }
            if (!selected) break;
            nextToPlay = Opposite(nextToPlay);
        }

        // The generated moves are setup history, not moves visible to players in this session.
        koPoint = null;

        if (configuration.StartingPlayer is not null)
        {
            TryStone(configuration.StartingPlayer, out nextToPlay);
            koPoint = null;
        }

        return new Session
        {
            Board = board,
            NextToPlay = nextToPlay,
            KoPoint = koPoint,
            CaptureTarget = configuration.CaptureTarget,
            RandomSeed = seed,
        };
    }

    private static ProtocolResponse<PonnukiConfigurationDocument> ParseConfiguration(ContractDocument document)
    {
        if (document.MediaType != JsonMediaType || document.SchemaId != PonnukiSchemas.Configuration)
            return Failure<PonnukiConfigurationDocument>("unsupported-configuration-document", "The configuration document type is not supported.");

        PonnukiConfigurationDocument? configuration;
        try { configuration = JsonSerializer.Deserialize<PonnukiConfigurationDocument>(document.Content, JsonOptions); }
        catch (JsonException ex) { return Failure<PonnukiConfigurationDocument>("invalid-configuration-json", ex.Message); }
        if (configuration is null)
            return Failure<PonnukiConfigurationDocument>("empty-configuration", "The configuration document is empty.");
        if (configuration.Version != 1)
            return Failure<PonnukiConfigurationDocument>("unsupported-configuration-version", "Only Ponnuki configuration version 1 is supported.");
        if (configuration.BoardSize is not (9 or 13 or 19))
            return Failure<PonnukiConfigurationDocument>("invalid-board-size", "BoardSize must be 9, 13, or 19.");
        if (configuration.InitialMoveCount < 0 || configuration.InitialMoveCount > configuration.BoardSize * configuration.BoardSize / 4)
            return Failure<PonnukiConfigurationDocument>("invalid-initial-move-count", "InitialMoveCount must be between zero and BoardSize squared divided by four.");
        if (configuration.CaptureTarget < 1)
            return Failure<PonnukiConfigurationDocument>("invalid-capture-target", "CaptureTarget must be positive.");
        if (configuration.StartingPlayer is not null && !TryStone(configuration.StartingPlayer, out _))
            return Failure<PonnukiConfigurationDocument>("invalid-starting-player", "StartingPlayer must be black, white, or null.");

        var occupied = new HashSet<PonnukiPoint>();
        foreach (var setup in configuration.SetupStones ?? [])
        {
            var point = new PonnukiPoint(setup.X, setup.Y);
            if (point.X < 0 || point.X >= configuration.BoardSize || point.Y < 0 || point.Y >= configuration.BoardSize)
                return Failure<PonnukiConfigurationDocument>("setup-point-outside-board", $"Setup point ({point.X},{point.Y}) is outside the board.");
            if (!TryStone(setup.Color, out _))
                return Failure<PonnukiConfigurationDocument>("invalid-setup-color", "A setup stone color must be black or white.");
            if (!occupied.Add(point))
                return Failure<PonnukiConfigurationDocument>("duplicate-setup-point", $"Setup point ({point.X},{point.Y}) is duplicated.");
        }
        return ProtocolResponse<PonnukiConfigurationDocument>.Success(configuration);
    }

    private static ProtocolResponse<PonnukiActionDocument> ParseAction(ContractDocument document)
    {
        if (document.MediaType != JsonMediaType || document.SchemaId != PonnukiSchemas.Action)
            return Failure<PonnukiActionDocument>("unsupported-action-document", "The action document type is not supported.");
        try
        {
            var action = JsonSerializer.Deserialize<PonnukiActionDocument>(document.Content, JsonOptions);
            return action is null
                ? Failure<PonnukiActionDocument>("empty-action", "The action document is empty.")
                : action.Version != 1
                    ? Failure<PonnukiActionDocument>("unsupported-action-version", "Only Ponnuki action version 1 is supported.")
                    : ProtocolResponse<PonnukiActionDocument>.Success(action);
        }
        catch (JsonException ex)
        {
            return Failure<PonnukiActionDocument>("invalid-action-json", ex.Message);
        }
    }

    private static PlaySpaceSnapshot CreateSnapshot(Session session)
    {
        var black = new List<PonnukiPointDocument>();
        var white = new List<PonnukiPointDocument>();
        foreach (var (point, stone) in session.Board.EnumerateStones())
        {
            var target = stone == PonnukiStone.Black ? black : white;
            target.Add(new PonnukiPointDocument(point.X, point.Y));
        }
        var state = Document(PonnukiSchemas.State, new PonnukiStateDocument(
            1, session.Board.Size, black, white, StoneName(session.NextToPlay),
            session.BlackCaptures, session.WhiteCaptures, session.CaptureTarget, session.RandomSeed,
            session.KoPoint is { } ko ? new PonnukiPointDocument(ko.X, ko.Y) : null,
            session.IsTerminal));
        ContractDocument? outcome = null;
        if (session.IsTerminal)
        {
            var winner = session.BlackCaptures >= session.CaptureTarget ? "black" : "white";
            outcome = Document(PonnukiSchemas.Outcome, new PonnukiOutcomeDocument(
                1, winner, $"{winner} captured {session.CaptureTarget} stones", session.BlackCaptures, session.WhiteCaptures));
        }
        return new PlaySpaceSnapshot(session.SessionId, session.Revision, state, session.IsTerminal, outcome);
    }

    private static ProtocolResponse<PlaySpaceActionApplied> Rejected(Session session, string code, string message) =>
        ProtocolResponse<PlaySpaceActionApplied>.Success(new PlaySpaceActionApplied(
            false, CreateSnapshot(session), [], new ProtocolError(code, message)));

    private static ContractDocument Document<T>(string schemaId, T value) =>
        Document(schemaId, JsonSerializer.Serialize(value, JsonOptions));

    private static ContractDocument Document(string schemaId, string content) => new(JsonMediaType, schemaId, content);

    private static ProtocolResponse<T> SessionNotFound<T>(PlaySpaceSessionId sessionId) =>
        Failure<T>("session-not-found", $"Play-space session '{sessionId}' was not found.");

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new ProtocolError(code, message));

    private static bool TryStone(string? value, out PonnukiStone stone)
    {
        if (string.Equals(value, "black", StringComparison.OrdinalIgnoreCase)) { stone = PonnukiStone.Black; return true; }
        if (string.Equals(value, "white", StringComparison.OrdinalIgnoreCase)) { stone = PonnukiStone.White; return true; }
        stone = PonnukiStone.Empty;
        return false;
    }

    private static string StoneName(PonnukiStone stone) => stone == PonnukiStone.Black ? "black" : "white";
    private static PonnukiStone Opposite(PonnukiStone stone) => stone == PonnukiStone.Black ? PonnukiStone.White : PonnukiStone.Black;

    private sealed class Session
    {
        public object Sync { get; } = new();
        public PlaySpaceSessionId SessionId { get; set; }
        public required PonnukiBoard Board { get; init; }
        public PonnukiStone NextToPlay { get; set; }
        public PonnukiPoint? KoPoint { get; set; }
        public int CaptureTarget { get; init; }
        public int RandomSeed { get; init; }
        public int BlackCaptures { get; set; }
        public int WhiteCaptures { get; set; }
        public long Revision { get; set; }
        public bool IsTerminal { get; set; }
    }
}
