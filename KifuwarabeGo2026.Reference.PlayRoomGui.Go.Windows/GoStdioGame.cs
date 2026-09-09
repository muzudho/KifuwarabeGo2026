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
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;

    public GoStdioGame(GoStdioSession session, Action ready)
    {
        _session = session;
        _ready = ready;
        _ = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1080, PreferredBackBufferHeight = 1080 };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Kifuwarabe Go - External Match | Click: play, P: pass, R: resign, Esc: close";
    }

    protected override void LoadContent()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _renderer = new GoInitialBoardRenderer(GraphicsDevice);
        _ready();
    }

    protected override void Update(GameTime time)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        if (_session.ShouldExit || keyboard.IsKeyDown(Keys.Escape)) Exit();
        else if (IsActive)
        {
            if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            {
                var geometry = GoInitialBoardRenderer.CreateGeometry(_session.Plan.BoardSize, GraphicsDevice.Viewport.Bounds);
                if (geometry.TryGetIntersection(new GoBoardScreenPoint(mouse.X, mouse.Y), out var point))
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
        _batch!.Begin(samplerState: SamplerState.LinearClamp);
        _renderer!.Draw(_batch, _session.View, GraphicsDevice.Viewport.Bounds);
        _batch.End();
        base.Draw(time);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _renderer?.Dispose(); _batch?.Dispose(); }
        base.Dispose(disposing);
    }
}
