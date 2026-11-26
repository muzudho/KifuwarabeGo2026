namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

/// <summary>外部から渡された状態だけを描画し、入力を返す画面。</summary>
public sealed class GoStdioGame : Game
{
    private readonly GoStdioSession _session;
    private readonly Action _ready;
    private SpriteBatch? _batch;
    private GoInitialBoardRenderer? _renderer;
    private GoMatchEvaluationPanel? _analysisPanel;
    private bool _showAnalysis = true;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;

    public GoStdioGame(GoStdioSession session, Action ready)
    {
        _session = session;
        _ready = ready;
        _ = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1080, PreferredBackBufferHeight = 900 };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Kifuwarabe Go - Local Match | Click: play, Tab: analysis, P: pass, R: resign, Esc: close";
    }

    protected override void LoadContent()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _renderer = new GoInitialBoardRenderer(GraphicsDevice);
        _analysisPanel = new GoMatchEvaluationPanel(GraphicsDevice);
        _ready();
    }

    protected override void Update(GameTime time)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        if (_session.ShouldExit || keyboard.IsKeyDown(Keys.Escape)) Exit();
        else if (IsActive)
        {
            var viewport = GraphicsDevice.Viewport.Bounds;
            var click = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
            var pointer = new Point(mouse.X, mouse.Y);
            if ((keyboard.IsKeyDown(Keys.Tab) && _previousKeyboard.IsKeyUp(Keys.Tab)) ||
                (click && GoMatchEvaluationPanel.ToggleBounds(viewport).Contains(pointer)))
            {
                _showAnalysis = !_showAnalysis;
            }
            else if (click)
            {
                var layout = GoMatchEvaluationPanel.Layout(viewport, _showAnalysis);
                var geometry = GoInitialBoardRenderer.CreateGeometry(_session.Plan.BoardSize, layout.Board);
                if (layout.Board.Contains(pointer) && geometry.TryGetIntersection(new GoBoardScreenPoint(mouse.X, mouse.Y), out var point))
                    _session.SubmitInput(MatchActionKind.PlayPoint, point.X, point.Y);
            }
            else if (keyboard.IsKeyDown(Keys.P) && _previousKeyboard.IsKeyUp(Keys.P)) _session.SubmitInput(MatchActionKind.Pass);
            else if (keyboard.IsKeyDown(Keys.R) && _previousKeyboard.IsKeyUp(Keys.R)) _session.SubmitInput(MatchActionKind.Resign);
        }
        _previousMouse = mouse;
        _previousKeyboard = keyboard;
        base.Update(time);
    }

    protected override void Draw(GameTime time)
    {
        GraphicsDevice.Clear(new Color(31, 37, 43));
        var viewport = GraphicsDevice.Viewport.Bounds;
        var layout = GoMatchEvaluationPanel.Layout(viewport, _showAnalysis);
        var display = _session.CaptureDisplay();
        var toggle = GoMatchEvaluationPanel.ToggleBounds(viewport);
        _batch!.Begin(samplerState: SamplerState.PointClamp);
        _renderer!.Draw(_batch, display.Board, layout.Board);
        if (_showAnalysis)
            _analysisPanel!.Draw(_batch, layout.Panel, display.Board.PlayedMoveCount, display.Evaluations);
        _analysisPanel!.DrawToggle(_batch, toggle, _showAnalysis, toggle.Contains(Mouse.GetState().Position));
        _batch.End();
        base.Draw(time);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _analysisPanel?.Dispose();
            _renderer?.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
