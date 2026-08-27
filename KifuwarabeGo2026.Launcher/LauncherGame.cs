namespace KifuwarabeGo2026.Launcher;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Effects;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Audio;
using KifuwarabeGo2026.Launcher.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

internal sealed class LauncherGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly ILauncherGuiPlatform _platform;
    private readonly ILauncherEngine _engine;
    private LauncherScreen? _screen;
    private KfwScreenCanvas? _canvas;
    private KfwStationeryDrawingTools? _stationery;
    private KeyboardState _previousKeyboard;
    private bool _screenshotRequested;
    private readonly ScreenshotEffect _screenshotEffect = new();
    private double _screenshotEffectStartedAt = double.NegativeInfinity;
    private const double ScreenshotEffectDurationSeconds = 0.42d;
    private SoundEffect? _screenshotShutterSound;
    private SoundEffectInstance? _screenshotShutterSoundInstance;

    public LauncherGame(ILauncherGuiPlatform platform, ILauncherEngine engine)
    {
        _platform = platform;
        _engine = engine;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = CreateWindowTitle();
    }

    private static string CreateWindowTitle()
    {
        var version = typeof(LauncherGame).Assembly.GetName().Version;
        return version is null
            ? "Kifuwarabe Go 2026 Launcher"
            : $"Kifuwarabe Go 2026 Launcher | v{version.Major}.{version.Minor}.{version.Build}";
    }

    protected override void LoadContent()
    {
        _canvas = new KfwScreenCanvas(GraphicsDevice, Content);
        _stationery = new KfwStationeryDrawingTools(_canvas, new ApproximateTextRasterizer(),
            (center, radius, black) => _canvas.DrawCircle(center, radius, black ? new Color(20, 24, 28) : new Color(235, 235, 228)));
        _screen = new LauncherScreen(_stationery, _platform, _engine, Exit);
        _screenshotShutterSound = ScreenshotShutterSound.Create();
        _screenshotShutterSoundInstance = _screenshotShutterSound.CreateInstance();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var controlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        if (controlDown && keyboard.IsKeyDown(Keys.P) && _previousKeyboard.IsKeyUp(Keys.P))
            _screenshotRequested = true;
        _previousKeyboard = keyboard;
        _screen?.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 18, 23));
        _screen?.Draw();
        if (_screenshotRequested)
        {
            _screenshotRequested = false;
            CaptureScreenshot(gameTime.TotalGameTime.TotalSeconds);
        }
        var effectAge = gameTime.TotalGameTime.TotalSeconds - _screenshotEffectStartedAt;
        if (_stationery is not null && effectAge >= 0d && effectAge < ScreenshotEffectDurationSeconds)
            _screenshotEffect.Draw(_stationery, (float)(effectAge / ScreenshotEffectDurationSeconds));
        base.Draw(gameTime);
    }

    private void CaptureScreenshot(double now)
    {
        try
        {
            var directory = _engine.GetState().ScreenshotSaveDirectory;
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"screenshot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height];
            GraphicsDevice.GetBackBufferData(pixels);
            using var texture = new Texture2D(GraphicsDevice, width, height);
            texture.SetData(pixels);
            using var stream = File.Create(path);
            texture.SaveAsPng(stream, width, height);
            _screenshotEffectStartedAt = now;
            if (_screenshotShutterSoundInstance is not null)
            {
                if (_screenshotShutterSoundInstance.State == SoundState.Playing) _screenshotShutterSoundInstance.Stop();
                _screenshotShutterSoundInstance.Volume = 0.72f;
                _screenshotShutterSoundInstance.Play();
            }
            _screen?.ShowStatus("SCREENSHOT SAVED: " + path);
        }
        catch (Exception exception)
        {
            _screen?.ShowStatus("SCREENSHOT FAILED: " + exception.Message);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _screenshotShutterSoundInstance?.Dispose(); _screenshotShutterSound?.Dispose(); _screen?.Dispose(); _canvas?.Dispose(); }
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
