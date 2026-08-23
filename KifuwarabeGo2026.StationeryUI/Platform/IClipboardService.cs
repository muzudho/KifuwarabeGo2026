namespace KifuwarabeGo2026.GameOasis.Gui.Application;

/// <summary>
/// OS のクリップボードへテキストを書き込みます。
/// </summary>
public interface IClipboardService
{
    bool TrySetText(string text);

    bool TryGetText(out string text);
}
