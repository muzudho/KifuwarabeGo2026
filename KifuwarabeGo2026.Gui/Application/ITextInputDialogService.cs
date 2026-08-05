namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS の文字列入力ダイアログを表示します。
/// </summary>
public interface ITextInputDialogService
{
    string? PromptText(TextInputDialogOptions options);
}
