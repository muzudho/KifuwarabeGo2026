namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Domain;
using KifuwarabeGo2026.Gtp;
using KifuwarabeGo2026.Infrastructure.Logging;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// ［対局中画面］の処理
/// </summary>
public sealed class PlayingScene : IDisposable
{
    private readonly GoAppSession _session;
    private readonly Action<float, float, float> _playPlaceStoneSound;
    private readonly Dictionary<GoStone, GtpEngineClient> _gtpEngines = new();
    private readonly Queue<Func<CancellationToken, Task<EngineCommandResult>>> _engineCommandQueue = new();
    private CancellationTokenSource _engineCancellation = new();
    private Task<EngineCommandCompletion>? _pendingEngineCommand;
    private int _engineCommandGeneration;

    public PlayingScene(GoAppSession session, Action<float, float, float> playPlaceStoneSound)
    {
        _session = session;
        _playPlaceStoneSound = playPlaceStoneSound;
    }

    public void Update()
    {
        CompletePendingEngineCommand();
        RequestComputerMoveIfReady();
    }

    public void StartPlaying()
    {
        _session.StartPlaying();
        StartGtpGameIfNeeded();
    }

    public bool TryHandleMouseClick(Point point)
    {
        if (ShouldShowEnginePreparing() && GoScreenRenderer.GetCancelPlayingButtonHit(point))
        {
            CancelGtpGame();
            _session.CancelPlaying();
            return true;
        }

        if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        if (!_session.CanAcceptHumanMove)
        {
            // Engine turns and engine setup are handled from Update().
            return true;
        }

        if (GoScreenRenderer.GetPassButtonHit(point))
        {
            var passedBy = _session.CurrentTurn;
            if (_session.Pass())
            {
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
                SyncHumanPassIfNeeded(passedBy);
            }

            return true;
        }

        if (GoScreenRenderer.GetResignButtonHit(point))
        {
            if (_session.Resign())
            {
                PlayPlaceStoneSound(0.45f, -0.25f, 0f);
                StopGtpGame();
            }

            return true;
        }

        if (GoScreenRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
        {
            var placedBy = _session.CurrentTurn;
            if (_session.TryPlaceStone(intersection.X, intersection.Y))
            {
                PlayPlaceStoneSound();
                SyncHumanMoveIfNeeded(placedBy, new GoPoint(intersection.X, intersection.Y));
            }

            return true;
        }

        return false;
    }

    public void Dispose()
    {
        _engineCancellation.Cancel();
        foreach (var engine in _gtpEngines.Values)
        {
            engine.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _gtpEngines.Clear();
        _engineCancellation.Dispose();
    }

    private bool ShouldShowEnginePreparing() =>
        _session.CurrentMode.Kind == GoAppModeKind.Playing &&
        !_session.CanAcceptHumanMove;

    private void StartGtpGameIfNeeded()
    {
        if (!HasComputerPlayer())
        {
            _session.SetEngineReady(true);
            return;
        }

        _session.SetEngineReady(false);
        EnsureGtpEngineForComputerPlayer(GoStone.Black);
        EnsureGtpEngineForComputerPlayer(GoStone.White);

        var enginesToStart = GetEngineSnapshot();
        BeginEngineCommand(async cancellationToken =>
        {
            foreach (var engine in enginesToStart)
            {
                try
                {
                    await engine.Client.StartAsync(cancellationToken);
                    await engine.Client.SendCommandExpectSuccessAsync($"boardsize {_session.BoardSize}", cancellationToken);
                    await engine.Client.SendCommandExpectSuccessAsync($"komi {_session.Komi.ToString(CultureInfo.InvariantCulture)}", cancellationToken);
                    await engine.Client.SendCommandExpectSuccessAsync("clear_board", cancellationToken);
                }
                catch (Exception ex)
                {
                    throw CreateEngineCommandException(engine.Stone, ex);
                }
            }

            return EngineCommandResult.EngineReady();
        });
    }

    private void SyncHumanMoveIfNeeded(GoStone stone, GoPoint point)
    {
        var enginesToSync = GetEngineSnapshot();
        if (enginesToSync.Count == 0)
        {
            return;
        }

        var color = FormatColor(stone);
        var vertex = GtpCoordinate.FormatVertex(point, _session.BoardSize);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, vertex, cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void SyncHumanPassIfNeeded(GoStone stone)
    {
        var enginesToSync = GetEngineSnapshot();
        if (enginesToSync.Count == 0)
        {
            return;
        }

        var color = FormatColor(stone);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, "pass", cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void RequestComputerMoveIfReady()
    {
        var currentTurn = _session.CurrentTurn;
        if (_pendingEngineCommand is not null ||
            _session.CurrentMode.Kind != GoAppModeKind.Playing ||
            _session.IsEngineThinking ||
            !string.IsNullOrWhiteSpace(_session.EngineErrorMessage) ||
            _session.GetPlayerKind(currentTurn) != GoPlayerKind.Computer)
        {
            return;
        }

        var engine = GetEngine(currentTurn);
        if (engine is null)
        {
            SetEngineError($"{FormatColor(currentTurn)} GTP engine is not ready.", currentTurn);
            return;
        }

        var color = FormatColor(currentTurn);
        BeginEngineCommand(async cancellationToken =>
        {
            var response = await engine.SendCommandAsync($"genmove {color}", cancellationToken);
            response.ThrowIfError($"genmove {color}");
            return EngineCommandResult.EngineMove(response.Payload, currentTurn);
        });
    }

    private void BeginEngineCommand(Func<CancellationToken, Task<EngineCommandResult>> command)
    {
        if (_pendingEngineCommand is not null)
        {
            _engineCommandQueue.Enqueue(command);
            return;
        }

        StartEngineCommand(command);
    }

    private void StartEngineCommand(Func<CancellationToken, Task<EngineCommandResult>> command)
    {
        _session.ClearEngineError();
        _session.SetEngineThinking(true);
        var generation = _engineCommandGeneration;
        var cancellationToken = _engineCancellation.Token;
        _pendingEngineCommand = Task.Run(async () =>
        {
            try
            {
                return new EngineCommandCompletion(await command(cancellationToken), generation);
            }
            catch (Exception ex)
            {
                var errorStone = ex is EngineCommandException engineException
                    ? engineException.Stone
                    : _session.CurrentTurn;
                return new EngineCommandCompletion(EngineCommandResult.Failure(ex, errorStone), generation);
            }
        });
    }

    private void CompletePendingEngineCommand()
    {
        if (_pendingEngineCommand is not { IsCompleted: true } completedCommand)
        {
            return;
        }

        _pendingEngineCommand = null;
        var completion = completedCommand.GetAwaiter().GetResult();
        if (completion.Generation != _engineCommandGeneration)
        {
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        var result = completion.Result;
        _session.SetEngineThinking(false);
        if (result.Error is not null)
        {
            SetEngineError(result.Error.Message, result.ErrorStone ?? _session.CurrentTurn, result.Error);
            return;
        }

        if (result.MakesEngineReady)
        {
            _session.SetEngineReady(true);
        }

        if (result.MoveText is null)
        {
            if (result.ClosesEngine)
            {
                StopGtpGame();
                return;
            }

            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (GtpCoordinate.IsPass(result.MoveText))
        {
            var comment = result.PlayedBy is null ? "" : _session.GetOwnEyeForcedPassComment();
            if (_session.Pass(comment))
            {
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
            }

            SyncComputerMoveToOtherEnginesIfNeeded(result.PlayedBy, "pass");
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (!GtpCoordinate.TryParseVertex(result.MoveText, _session.BoardSize, out var point))
        {
            SetEngineError($"Invalid GTP vertex: {result.MoveText}", result.PlayedBy ?? _session.CurrentTurn);
            return;
        }

        if (!_session.TryPlaceStone(point.X, point.Y))
        {
            SetEngineError($"Illegal GTP move: {result.MoveText}", result.PlayedBy ?? _session.CurrentTurn);
            return;
        }

        PlayPlaceStoneSound();
        SyncComputerMoveToOtherEnginesIfNeeded(result.PlayedBy, GtpCoordinate.FormatVertex(point, _session.BoardSize));
        StartQueuedEngineCommandIfNeeded();
    }

    private void PlayPlaceStoneSound(float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        _playPlaceStoneSound(volume, pitch, pan);
    }

    private void SyncComputerMoveToOtherEnginesIfNeeded(GoStone? playedBy, string vertex)
    {
        if (playedBy is null)
        {
            StopGtpGameIfGameOver();
            return;
        }

        var enginesToSync = GetEngineSnapshotExcept(playedBy.Value);
        if (enginesToSync.Count == 0)
        {
            StopGtpGameIfGameOver();
            return;
        }

        var color = FormatColor(playedBy.Value);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, vertex, cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void StartQueuedEngineCommandIfNeeded()
    {
        if (_pendingEngineCommand is null && _engineCommandQueue.Count > 0)
        {
            StartEngineCommand(_engineCommandQueue.Dequeue());
        }
    }

    private bool HasComputerPlayer() =>
        _session.BlackPlayerKind == GoPlayerKind.Computer || _session.WhitePlayerKind == GoPlayerKind.Computer;

    private void EnsureGtpEngineForComputerPlayer(GoStone stone)
    {
        if (_session.GetPlayerKind(stone) != GoPlayerKind.Computer || _gtpEngines.ContainsKey(stone))
        {
            return;
        }

        _gtpEngines[stone] = new GtpEngineClient(CreateEngineSettings(stone), TimeSpan.FromSeconds(10));
    }

    private GtpEngineClient? GetEngine(GoStone stone) =>
        _gtpEngines.TryGetValue(stone, out var engine) ? engine : null;

    private List<EngineEntry> GetEngineSnapshot() =>
        _gtpEngines.Select(pair => new EngineEntry(pair.Key, pair.Value)).ToList();

    private List<EngineEntry> GetEngineSnapshotExcept(GoStone stone) =>
        _gtpEngines
            .Where(pair => pair.Key != stone)
            .Select(pair => new EngineEntry(pair.Key, pair.Value))
            .ToList();

    private static async Task SyncPlayToEnginesAsync(
        IReadOnlyList<EngineEntry> engines,
        string color,
        string vertex,
        CancellationToken cancellationToken)
    {
        foreach (var engine in engines)
        {
            try
            {
                await engine.Client.SendCommandExpectSuccessAsync($"play {color} {vertex}", cancellationToken);
            }
            catch (Exception ex)
            {
                throw new EngineCommandException(engine.Stone, ex.Message, ex);
            }
        }
    }

    private void CancelGtpGame()
    {
        StopGtpGame();
    }

    private void StopGtpGameIfGameOver()
    {
        if (_session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            StopGtpGame();
        }
    }

    private void StopGtpGame()
    {
        _engineCommandGeneration++;
        _engineCommandQueue.Clear();
        _pendingEngineCommand = null;
        _engineCancellation.Cancel();
        _engineCancellation.Dispose();
        _engineCancellation = new CancellationTokenSource();

        var engines = GetEngineSnapshot();
        _gtpEngines.Clear();
        foreach (var engine in engines)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.Client.DisposeAsync();
                }
                catch
                {
                    // Cancellation should return the GUI to setup even if the engine process is already gone.
                }
            });
        }
    }

    private static string FormatColor(GoStone stone) => stone == GoStone.Black ? "black" : "white";

    private EngineCommandException CreateEngineCommandException(GoStone stone, Exception exception)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        var message = $"{FormatColor(stone)} engine '{profile.DisplayName}' failed. Executable: {profile.ExecutablePath}";
        return new EngineCommandException(stone, message, exception);
    }

    private void SetEngineError(string message, GoStone stone, Exception? exception = null)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        ApplicationErrorLog.Write(
            "GTP ENGINE ERROR",
            $"Stone: {FormatColor(stone)}{Environment.NewLine}" +
            $"Engine: {profile.DisplayName}{Environment.NewLine}" +
            $"Executable: {profile.ExecutablePath}{Environment.NewLine}" +
            $"Message: {message}",
            exception);
        _session.SetEngineError(message, stone);
    }

    private GtpEngineSettings CreateEngineSettings(GoStone stone)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        var logPrefix = stone == GoStone.Black ? "[black-engine]" : "[white-engine]";
        return new GtpEngineSettings(
            profile.DisplayName,
            profile.ExecutablePath,
            profile.WorkingDirectoryModel,
            profile.Arguments,
            profile.EnableGtpLog,
            logPrefix,
            profile.GuiOptions);
    }

    private sealed record EngineCommandCompletion(EngineCommandResult Result, int Generation);

    private sealed record EngineCommandResult(string? MoveText, GoStone? PlayedBy, Exception? Error, GoStone? ErrorStone = null, bool MakesEngineReady = false, bool ClosesEngine = false)
    {
        public static EngineCommandResult Success(bool closesEngine = false) => new(null, null, null, ClosesEngine: closesEngine);

        public static EngineCommandResult EngineReady() => new(null, null, null, MakesEngineReady: true);

        public static EngineCommandResult EngineMove(string moveText, GoStone playedBy) => new(moveText, playedBy, null);

        public static EngineCommandResult Failure(Exception error, GoStone errorStone) => new(null, null, error, errorStone);
    }

    private sealed record EngineEntry(GoStone Stone, GtpEngineClient Client);

    private sealed class EngineCommandException(GoStone stone, string message, Exception innerException)
        : Exception(message, innerException)
    {
        public GoStone Stone { get; } = stone;
    }
}
