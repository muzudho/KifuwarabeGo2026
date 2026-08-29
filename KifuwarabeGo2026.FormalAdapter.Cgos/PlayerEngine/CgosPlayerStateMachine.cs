namespace KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine;

using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

/// <summary>Coordinates one CGOS player's game lifecycle independently of TCP and process implementation.</summary>
public sealed class CgosPlayerStateMachine : IAsyncDisposable
{
    private readonly string _username;
    private readonly CgosPlayerEngineFactory? _engineFactory;
    private readonly Func<int, string, CancellationToken, Task<string>>? _humanMoveProvider;
    private readonly Func<int, bool>? _consumeResignRequest;
    private readonly Action<string> _log;
    private ICgosPlayerEngine? _engine;
    private int _gameId;

    public CgosPlayerStateMachine(
        string username,
        CgosPlayerEngineFactory? engineFactory,
        Func<int, string, CancellationToken, Task<string>>? humanMoveProvider = null,
        Func<int, bool>? consumeResignRequest = null,
        Action<string>? log = null)
    {
        _username = string.IsNullOrWhiteSpace(username) ? throw new ArgumentException("A CGOS username is required.", nameof(username)) : username;
        _engineFactory = engineFactory;
        _humanMoveProvider = humanMoveProvider;
        _consumeResignRequest = consumeResignRequest;
        _log = log ?? (_ => { });
        if (engineFactory is null && humanMoveProvider is null)
            throw new ArgumentException("An engine factory or human move provider is required.");
    }

    public async Task<CgosClientCommand?> HandleAsync(
        CgosServerMessage message,
        bool serverSupportsAnalyze,
        CancellationToken cancellationToken = default)
    {
        return message switch
        {
            CgosMatchSetup setup => await HandleSetupAsync(setup, cancellationToken),
            CgosMovePlayed play => await HandlePlayAsync(play, cancellationToken),
            CgosGenMoveRequested genmove => await HandleGenMoveAsync(genmove, serverSupportsAnalyze, cancellationToken),
            CgosGameOver gameOver => await HandleGameOverAsync(gameOver),
            CgosInfoMessage => null,
            _ => throw new InvalidOperationException("Unexpected CGOS player message: " + message.RawLine),
        };
    }

    public async ValueTask DisposeAsync() => await ShutdownEngineAsync();

    private async Task<CgosClientCommand?> HandleSetupAsync(CgosMatchSetup setup, CancellationToken cancellationToken)
    {
        await ShutdownEngineAsync();
        _gameId = setup.GameId;
        var localColor = string.Equals(_username, setup.WhitePlayer, StringComparison.OrdinalIgnoreCase) ? "white" : "black";
        if (_engineFactory is not null)
        {
            _engine = await _engineFactory(
                new CgosPlayerEngineSetup(setup.GameId, setup.BoardSize, setup.Komi, localColor, setup.WhitePlayer, setup.BlackPlayer),
                cancellationToken);
            await _engine.ConfigureAsync(setup.BoardSize, setup.Komi, cancellationToken);
            foreach (var move in setup.MoveHistory)
                await _engine.PlayAsync(move.Color, move.Vertex, move.TimeLeftMilliseconds, cancellationToken);
        }
        _log($"# Setup game {setup.GameId}; localColor={localColor}.");
        return null;
    }

    private async Task<CgosClientCommand?> HandlePlayAsync(CgosMovePlayed play, CancellationToken cancellationToken)
    {
        if (_engine is not null) await _engine.PlayAsync(play.Color, play.Vertex, play.TimeLeftMilliseconds, cancellationToken);
        return null;
    }

    private async Task<CgosClientCommand> HandleGenMoveAsync(
        CgosGenMoveRequested genmove,
        bool serverSupportsAnalyze,
        CancellationToken cancellationToken)
    {
        if (_consumeResignRequest?.Invoke(_gameId) == true) return new CgosResign();
        if (_humanMoveProvider is not null)
        {
            var vertex = await _humanMoveProvider(_gameId, genmove.Color, cancellationToken);
            return string.Equals(vertex, "resign", StringComparison.OrdinalIgnoreCase) ? new CgosResign() : new CgosMove(vertex.ToLowerInvariant());
        }
        var engine = _engine ?? throw new InvalidOperationException("CGOS requested a move before engine setup.");
        var generated = await engine.GenerateMoveAsync(genmove.Color, serverSupportsAnalyze && engine.SupportsAnalyze, cancellationToken);
        if (_consumeResignRequest?.Invoke(_gameId) == true) return new CgosResign();
        return string.Equals(generated.Vertex, "resign", StringComparison.OrdinalIgnoreCase)
            ? new CgosResign()
            : new CgosMove(generated.Vertex.ToLowerInvariant(), generated.AnalysisJson);
    }

    private async Task<CgosClientCommand> HandleGameOverAsync(CgosGameOver gameOver)
    {
        _log("# Game over: " + gameOver.Result);
        await ShutdownEngineAsync();
        return new CgosReady();
    }

    private async Task ShutdownEngineAsync()
    {
        if (_engine is null) return;
        await _engine.DisposeAsync();
        _engine = null;
    }
}
