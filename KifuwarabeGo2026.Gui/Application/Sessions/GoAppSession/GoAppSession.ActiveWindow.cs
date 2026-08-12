namespace KifuwarabeGo2026.Gui.Application;

/// <summary>モーダル画面の前後関係を管理します。</summary>
public sealed partial class GoAppSession
{
    private ActiveWindowStack ActiveWindows { get; } = new();

    public ActiveWindowId ActiveWindowId => ActiveWindows.Current;

    private void ActivateWindow(ActiveWindowId windowId) => ActiveWindows.Activate(windowId);

    private void DeactivateWindow(ActiveWindowId windowId) => ActiveWindows.Deactivate(windowId);
}
