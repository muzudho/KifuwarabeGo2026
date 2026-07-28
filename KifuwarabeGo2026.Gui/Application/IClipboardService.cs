namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS のクリップボードへテキストを書き込みます。
/// </summary>
public interface IClipboardService
{
    bool TrySetText(string text);
}
