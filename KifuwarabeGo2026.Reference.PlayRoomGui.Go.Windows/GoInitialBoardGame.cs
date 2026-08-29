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
    private SpriteBatch? _spriteBatch;
    private GoInitialBoardRenderer? _renderer;

    public GoInitialBoardGame(GoPlayRoomLaunchPlan plan)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _ = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1080,
            PreferredBackBufferHeight = 1080,
            SynchronizeWithVerticalRetrace = true,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = $"Kifuwarabe Go - {_plan.BoardSize}x{_plan.BoardSize}";
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new GoInitialBoardRenderer(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(31, 37, 43));
        _spriteBatch!.Begin(samplerState: SamplerState.LinearClamp);
        _renderer!.Draw(_spriteBatch, _plan, GraphicsDevice.Viewport.Bounds);
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
