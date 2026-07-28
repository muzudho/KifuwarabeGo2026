namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;

/// <summary>
/// WinForms を使って Windows の警告ダイアログを表示します。
/// </summary>
public sealed class WindowsMessageDialogService : IMessageDialogService
{
    public void ShowWarning(string title, string message)
    {
        System.Windows.Forms.MessageBox.Show(
            message,
            title,
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning);
    }
}
