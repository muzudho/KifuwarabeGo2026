namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS のメッセージダイアログを表示します。
/// </summary>
public interface IMessageDialogService
{
    void ShowWarning(string title, string message);
}
