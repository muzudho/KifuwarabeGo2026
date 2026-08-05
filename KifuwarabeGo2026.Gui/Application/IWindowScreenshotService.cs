namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS が管理するアクティブウィンドウを、外枠を含めて画像へ保存します。
/// </summary>
public interface IWindowScreenshotService
{
    void SaveActiveWindow(string filePath);
}
