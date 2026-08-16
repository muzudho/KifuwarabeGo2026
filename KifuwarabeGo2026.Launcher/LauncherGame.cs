namespace KifuwarabeGo2026.Launcher;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Launcher.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class LauncherGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly IPlatformServices _platform;
    private LauncherScreen? _screen;
    private KfwScreenCanvas? _canvas;

    public LauncherGame(IPlatformServices platform)
    {
        _platform = platform;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "KIFUWARABE GO 2026 - COMMON LAUNCHER";
    }

    protected override void LoadContent()
    {
        _canvas = new KfwScreenCanvas(GraphicsDevice, Content);
        var stationery = new KfwStationeryDrawingTools(_canvas, new ApproximateTextRasterizer(),
            (center, radius, black) => _canvas.DrawCircle(center, radius, black ? new Color(20, 24, 28) : new Color(235, 235, 228)));
        _screen = new LauncherScreen(stationery, _platform, new HttpClient { Timeout = TimeSpan.FromMinutes(15) });
    }

    protected override void Update(GameTime gameTime)
    {
        _screen?.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 18, 23));
        _screen?.Draw();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _screen?.Dispose(); _canvas?.Dispose(); }
        base.Dispose(disposing);
    }
}

internal sealed class ApproximateTextRasterizer : ITextRasterizer
{
    public byte[] RasterizePng(string text, int pixelHeight, bool bold) => throw new NotSupportedException();
    public float MeasureTextWidth(string text, int pixelHeight, bool bold) => text.Length * pixelHeight * 0.6f;
    public int MeasureLineHeight(int pixelHeight, int extraLineSpacing) => pixelHeight + extraLineSpacing;
    public int MeasureBaselineOffset(int pixelHeight) => (int)(pixelHeight * 0.8f);
    public int GetWrappedPageCount(string text, int width, int height, int pixelHeight, int extraLineSpacing) => 1;
    public byte[] RasterizeWrappedPagePng(string text, int width, int height, int pixelHeight, int extraLineSpacing, int requestedPage) => throw new NotSupportedException();
}
