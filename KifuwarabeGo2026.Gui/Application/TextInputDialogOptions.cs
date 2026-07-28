namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// 文字列入力ダイアログの OS 非依存オプションです。
/// </summary>
public sealed record TextInputDialogOptions
{
    public required string Title { get; init; }

    public string InitialValue { get; init; } = "";

    public int MaximumLength { get; init; } = 32767;
}
