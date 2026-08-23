namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;

/// <summary>
/// OS のネイティブウィンドウへアプリケーションアイコンを適用します。
/// </summary>
public interface IWindowIconService
{
    void TryApply(IntPtr windowHandle);
}
