namespace KifuwarabeGo2026.GameOasis.Gui.Application;

/// <summary>
/// フォルダー選択ダイアログの OS 非依存オプションです。
/// </summary>
public sealed record FolderDialogOptions
{
    public required string Title { get; init; }

    public string? InitialDirectory { get; init; }

    public bool AllowCreateFolder { get; init; } = true;
}
