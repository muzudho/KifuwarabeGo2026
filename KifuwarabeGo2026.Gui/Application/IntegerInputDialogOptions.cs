namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// 整数入力ダイアログの OS 非依存オプションです。
/// </summary>
public sealed record IntegerInputDialogOptions
{
    public required string Title { get; init; }

    public int InitialValue { get; init; }

    public int Minimum { get; init; } = int.MinValue;

    public int Maximum { get; init; } = int.MaxValue;
}
