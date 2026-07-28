namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS の文字列・整数入力ダイアログを表示します。
/// </summary>
public interface ITextInputDialogService
{
    string? PromptText(TextInputDialogOptions options);

    int? PromptInteger(IntegerInputDialogOptions options);
}
