namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using System;

/// <summary>
/// WinForms を使って Windows の文字列入力ダイアログを表示します。
/// </summary>
public sealed class WindowsTextInputDialogService : ITextInputDialogService
{
    public string? PromptText(TextInputDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var dialog = CreateDialog(options.Title);
        using var textBox = new System.Windows.Forms.TextBox
        {
            Left = 20,
            Top = 20,
            Width = 580,
            Text = options.InitialValue,
            MaxLength = options.MaximumLength,
        };
        using var cancelButton = CreateCancelButton();
        using var okButton = CreateOkButton();

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.AddRange([textBox, cancelButton, okButton]);
        textBox.SelectAll();

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? textBox.Text
            : null;
    }

    private static System.Windows.Forms.Form CreateDialog(string title) =>
        new()
        {
            ClientSize = new System.Drawing.Size(620, 150),
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
            Text = title,
        };

    private static System.Windows.Forms.Button CreateCancelButton() =>
        new()
        {
            Left = 20,
            Top = 78,
            Width = 110,
            Height = 42,
            Text = "CANCEL",
            DialogResult = System.Windows.Forms.DialogResult.Cancel,
        };

    private static System.Windows.Forms.Button CreateOkButton() =>
        new()
        {
            Left = 150,
            Top = 78,
            Width = 110,
            Height = 42,
            Text = "OK",
            DialogResult = System.Windows.Forms.DialogResult.OK,
        };
}
