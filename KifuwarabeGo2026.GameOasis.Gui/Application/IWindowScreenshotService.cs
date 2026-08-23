namespace KifuwarabeGo2026.GameOasis.Gui.Application;

/// <summary>
/// アプリケーションのメインウィンドウを、外枠を含めて画像へ保存します。
/// </summary>
public interface IWindowScreenshotService
{
    WindowScreenshotResult SaveActiveWindow(string filePath);
}

public sealed record WindowScreenshotResult(
    int WindowX,
    int WindowY,
    int WindowWidth,
    int WindowHeight,
    int ScreenshotWidth,
    int ScreenshotHeight,
    uint WindowDpi,
    uint SystemDpi,
    string DpiAwareness,
    string Diagnostics);
