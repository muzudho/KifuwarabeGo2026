namespace KifuwarabeGo2026;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using KifuwarabeGo2026.Gtp;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GoAppSession _session = new();
    private CancellationTokenSource _engineCancellation = new();
    private GoScreenRenderer? _renderer;
    private SoundEffect? _placeStoneSound;
    private MouseState _previousMouse;
    private GtpEngineClient? _gtpEngine;
    private Task<EngineCommandCompletion>? _pendingEngineCommand;
    private readonly Queue<Func<CancellationToken, Task<EngineCommandResult>>> _engineCommandQueue = new();
    private int _engineCommandGeneration;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = VirtualScreen.Width;
        _graphics.PreferredBackBufferHeight = VirtualScreen.Height;
        _graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Kifuwarabe Go 2026";
        Window.AllowUserResizing = true;
    }

    protected override void LoadContent()
    {
        _renderer = new GoScreenRenderer(GraphicsDevice, Content);
        _placeStoneSound = CreatePlaceStoneSound();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
        }

        CompletePendingEngineCommand();
        RequestComputerMoveIfReady();

        if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            UpdateBoardSizeByKeyboard(keyboard);
        }
        UpdateMouseInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));
        _renderer?.Draw(_session, Mouse.GetState().Position);

        base.Draw(gameTime);
    }

    private void UpdateBoardSizeByKeyboard(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1))
        {
            _session.ChangeBoardSize(9);
        }
        else if (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2))
        {
            _session.ChangeBoardSize(13);
        }
        else if (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3))
        {
            _session.ChangeBoardSize(19);
        }
    }

    private void UpdateMouseInput()
    {
        var mouse = Mouse.GetState();
        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
            if (_session.CurrentMode.Kind != GoAppModeKind.Playing && GoScreenRenderer.GetBoardSizeButtonHit(point, _session.CurrentMode.Kind) is { } boardSize)
            {
                _session.ChangeBoardSize(boardSize);
            }
            else if (_session.CurrentMode.Kind != GoAppModeKind.Playing && GoScreenRenderer.GetStartPlayingButtonHit(point, _session.CurrentMode.Kind))
            {
                _session.StartPlaying();
                StartGtpGameIfNeeded();
            }
            else if (_session.CurrentMode.Kind != GoAppModeKind.Playing && GoScreenRenderer.GetBlackPlayerKindButtonHit(point) is { } blackPlayerKind)
            {
                _session.SetPlayerKind(Domain.GoStone.Black, blackPlayerKind);
            }
            else if (_session.CurrentMode.Kind != GoAppModeKind.Playing && GoScreenRenderer.GetWhitePlayerKindButtonHit(point) is { } whitePlayerKind)
            {
                _session.SetPlayerKind(Domain.GoStone.White, whitePlayerKind);
            }
            else if (ShouldShowEnginePreparing() && GoScreenRenderer.GetCancelPlayingButtonHit(point))
            {
                CancelGtpGame();
                _session.CancelPlaying();
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.Playing && !CanAcceptHumanMove())
            {
                // Engine turns and engine setup are handled from Update().
            }
            else if (GoScreenRenderer.GetPassButtonHit(point))
            {
                var passedBy = _session.CurrentTurn;
                if (_session.Pass())
                {
                    _placeStoneSound?.Play(0.45f, 0.25f, 0f);
                    SyncHumanPassIfNeeded(passedBy);
                }
            }
            else if (GoScreenRenderer.GetResignButtonHit(point))
            {
                if (_session.Resign())
                {
                    _placeStoneSound?.Play(0.45f, -0.25f, 0f);
                    StopGtpGame();
                }
            }
            else if (GoScreenRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
            {
                var placedBy = _session.CurrentTurn;
                if (_session.TryPlaceStone(intersection.X, intersection.Y))
                {
                    _placeStoneSound?.Play();
                    SyncHumanMoveIfNeeded(placedBy, new GoPoint(intersection.X, intersection.Y));
                }
            }
        }

        _previousMouse = mouse;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engineCancellation.Cancel();
            _gtpEngine?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _engineCancellation.Dispose();
        }

        base.Dispose(disposing);
    }

    private bool CanAcceptHumanMove() => _session.CanAcceptHumanMove;

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
        _session.SetEngineLogPath(GetDefaultGtpLogPath());
        _gtpEngine ??= new GtpEngineClient(CreateDefaultEngineSettings(), TimeSpan.FromSeconds(10));
        BeginEngineCommand(async cancellationToken =>
        {
            await _gtpEngine.StartAsync(cancellationToken);
            await _gtpEngine.SendCommandExpectSuccessAsync($"boardsize {_session.BoardSize}", cancellationToken);
            await _gtpEngine.SendCommandExpectSuccessAsync("clear_board", cancellationToken);
            return EngineCommandResult.EngineReady();
        });
    }

    private void SyncHumanMoveIfNeeded(GoStone stone, GoPoint point)
    {
        if (!HasComputerPlayer() || _gtpEngine is null)
        {
            return;
        }

        var color = FormatColor(stone);
        var vertex = GtpCoordinate.FormatVertex(point, _session.BoardSize);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await _gtpEngine.SendCommandExpectSuccessAsync($"play {color} {vertex}", cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void SyncHumanPassIfNeeded(GoStone stone)
    {
        if (!HasComputerPlayer() || _gtpEngine is null)
        {
            return;
        }

        var color = FormatColor(stone);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await _gtpEngine.SendCommandExpectSuccessAsync($"play {color} pass", cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void RequestComputerMoveIfReady()
    {
        if (_pendingEngineCommand is not null ||
            _gtpEngine is null ||
            _session.CurrentMode.Kind != GoAppModeKind.Playing ||
            _session.IsEngineThinking ||
            !string.IsNullOrWhiteSpace(_session.EngineErrorMessage) ||
            _session.GetPlayerKind(_session.CurrentTurn) != GoPlayerKind.Computer)
        {
            return;
        }

        var color = FormatColor(_session.CurrentTurn);
        BeginEngineCommand(async cancellationToken =>
        {
            var response = await _gtpEngine.SendCommandAsync($"genmove {color}", cancellationToken);
            response.ThrowIfError($"genmove {color}");
            return EngineCommandResult.EngineMove(response.Payload);
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
                return new EngineCommandCompletion(EngineCommandResult.Failure(ex.Message), generation);
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
        if (result.ErrorMessage is not null)
        {
            _session.SetEngineError(result.ErrorMessage);
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
            if (_session.Pass())
            {
                _placeStoneSound?.Play(0.45f, 0.25f, 0f);
            }

            StopGtpGameIfGameOver();
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (!GtpCoordinate.TryParseVertex(result.MoveText, _session.BoardSize, out var point))
        {
            _session.SetEngineError($"Invalid GTP vertex: {result.MoveText}");
            return;
        }

        if (!_session.TryPlaceStone(point.X, point.Y))
        {
            _session.SetEngineError($"Illegal GTP move: {result.MoveText}");
            return;
        }

        _placeStoneSound?.Play();
        StopGtpGameIfGameOver();
        StartQueuedEngineCommandIfNeeded();
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

        var engine = _gtpEngine;
        _gtpEngine = null;
        if (engine is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.DisposeAsync();
                }
                catch
                {
                    // Cancellation should return the GUI to setup even if the engine process is already gone.
                }
            });
        }
    }

    private static string FormatColor(GoStone stone) => stone == GoStone.Black ? "black" : "white";

    private static GtpEngineSettings CreateDefaultEngineSettings()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var configuration = new DirectoryInfo(baseDirectory).Parent?.Name ?? "Debug";
        var repositoryRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
        var executableName = OperatingSystem.IsWindows() ? "KifuwarabeGo2026.Engine.exe" : "KifuwarabeGo2026.Engine";
        var engineDirectory = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Engine", "bin", configuration, "net8.0");
        var engineExecutable = Path.Combine(engineDirectory, executableName);
        if (File.Exists(engineExecutable))
        {
            return new GtpEngineSettings("Kifuwarabe Random GTP", engineExecutable, engineDirectory, "", EnableGtpLog: true);
        }

        var engineProject = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Engine", "KifuwarabeGo2026.Engine.csproj");
        return new GtpEngineSettings(
            "Kifuwarabe Random GTP",
            "dotnet",
            repositoryRoot,
            $"run --project \"{engineProject}\"",
            EnableGtpLog: true);
    }

    private static string GetDefaultGtpLogPath() => Path.Combine(AppContext.BaseDirectory, "logs", "gtp.log");

    private sealed record EngineCommandCompletion(EngineCommandResult Result, int Generation);

    private sealed record EngineCommandResult(string? MoveText, string? ErrorMessage, bool MakesEngineReady = false, bool ClosesEngine = false)
    {
        public static EngineCommandResult Success(bool closesEngine = false) => new(null, null, ClosesEngine: closesEngine);

        public static EngineCommandResult EngineReady() => new(null, null, MakesEngineReady: true);

        public static EngineCommandResult EngineMove(string moveText) => new(moveText, null);

        public static EngineCommandResult Failure(string errorMessage) => new(null, errorMessage);
    }

    private static SoundEffect CreatePlaceStoneSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.09f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-42f * t);
            var wave = MathF.Sin(MathF.Tau * 520f * t) * 0.55f + MathF.Sin(MathF.Tau * 210f * t) * 0.45f;
            var sample = (short)(wave * envelope * short.MaxValue * 0.55f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }
}
