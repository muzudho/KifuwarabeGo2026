namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

/// <summary>保存済み起動Planの初期盤面を表示する、囲碁Play Room専用の最小画面ループです。</summary>
public sealed class GoInitialBoardGame : Game
{
    private readonly GoPlayRoomLaunchPlan _plan;
    private readonly GoLocalMatchSession _session;
    private readonly GoLocalMatchGtpController _gtp;
    private readonly CancellationTokenSource _cancellation = new();
    private SpriteBatch? _spriteBatch;
    private GoInitialBoardRenderer? _renderer;
    private Task? _initializationTask;
    private bool _initialized;
    private bool _engineFailed;
    private Task<GoLocalMatchAction>? _actionTask;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private string _status = "STARTING";

    public GoInitialBoardGame(GoPlayRoomLaunchPlan plan)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _session = new GoLocalMatchSession(plan);
        _gtp = new GoLocalMatchGtpController(plan);
        _ = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1080,
            PreferredBackBufferHeight = 1080,
            SynchronizeWithVerticalRetrace = true,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        UpdateWindowTitle();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new GoInitialBoardRenderer(GraphicsDevice);
        _initializationTask = _gtp.InitializeAsync(_cancellation.Token);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        CompleteInitialization();
        CompleteAction();
        if (_initialized && !_engineFailed &&
            _actionTask is null && !_session.IsGameOver)
        {
            if (_session.IsComputerTurn)
            {
                _status = $"{_session.CurrentTurn} ENGINE THINKING";
                _actionTask = _gtp.GenerateMoveAsync(_session.CurrentTurn, _cancellation.Token);
            }
            else if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            {
                var geometry = GoInitialBoardRenderer.CreateGeometry(_session.Plan.BoardSize, GraphicsDevice.Viewport.Bounds);
                if (geometry.TryGetIntersection(new GoBoardScreenPoint(mouse.X, mouse.Y), out var point) && _session.CanPlay(point))
                {
                    _status = $"PLAYING {GoLocalMatchGtpController.Vertex(point, _session.Plan.BoardSize)}";
                    _actionTask = _gtp.PlayHumanAsync(_session.CurrentTurn, point, _cancellation.Token);
                }
            }
            else if (keyboard.IsKeyDown(Keys.P) && _previousKeyboard.IsKeyUp(Keys.P))
            {
                _status = $"{_session.CurrentTurn} PASS";
                _actionTask = _gtp.PassHumanAsync(_session.CurrentTurn, _cancellation.Token);
            }
        }
        _previousMouse = mouse;
        _previousKeyboard = keyboard;
        UpdateWindowTitle();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(31, 37, 43));
        _spriteBatch!.Begin(samplerState: SamplerState.LinearClamp);
        _renderer!.Draw(_spriteBatch, _session.CaptureViewState(), GraphicsDevice.Viewport.Bounds);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Cancel();
            try
            {
                var pending = new[] { _initializationTask, _actionTask }.Where(task => task is not null).Cast<Task>().ToArray();
                if (pending.Length > 0) Task.WhenAll(pending).GetAwaiter().GetResult();
            }
            catch { }
            try { _gtp.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            catch { }
            _cancellation.Dispose();
            _renderer?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void CompleteInitialization()
    {
        var task = _initializationTask;
        if (task is null || !task.IsCompleted) return;
        _initializationTask = null;
        if (task.IsCompletedSuccessfully)
        {
            _initialized = true;
            _status = _session.IsComputerTurn ? $"{_session.CurrentTurn} ENGINE READY" : $"{_session.CurrentTurn} TO PLAY";
        }
        else
        {
            _engineFailed = true;
            _status = "ENGINE ERROR: " + (task.Exception?.GetBaseException().Message ?? "Initialization was cancelled.");
        }
    }

    private void CompleteAction()
    {
        var task = _actionTask;
        if (task is null || !task.IsCompleted) return;
        _actionTask = null;
        if (!task.IsCompletedSuccessfully)
        {
            _engineFailed = true;
            _status = "ENGINE ERROR: " + (task.Exception?.GetBaseException().Message ?? "The move was cancelled.");
            return;
        }

        var action = task.Result;
        var applied = action.Stone == _session.CurrentTurn && action.Kind switch
        {
            GoLocalMatchActionKind.Play when action.Point is { } point => _session.TryPlay(point),
            GoLocalMatchActionKind.Pass => _session.Pass(),
            GoLocalMatchActionKind.Resign => _session.Resign(action.Stone),
            _ => false,
        };
        _status = applied
            ? _session.IsGameOver ? _session.GameOverReason : $"{_session.CurrentTurn} TO PLAY"
            : "MOVE ERROR: The action was not legal in the Play Room state.";
        if (!applied) _engineFailed = true;
    }

    private void UpdateWindowTitle() =>
        Window.Title = $"Kifuwarabe Go - {_plan.BoardSize}x{_plan.BoardSize} | {_status} | Click: play, P: pass, Esc: close";
}
