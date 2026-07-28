namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;

/// <summary>
/// ファイルを保存するダイアログの OS 非依存オプションです。
/// </summary>
public sealed record SaveFileDialogOptions
{
    public required string Title { get; init; }

    public string? InitialDirectory { get; init; }

    public string? InitialFileName { get; init; }

    public string? DefaultExtension { get; init; }

    public IReadOnlyList<FileDialogFilter> Filters { get; init; } = [];

    public bool AddExtension { get; init; } = true;

    public bool CheckPathExists { get; init; } = true;

    public bool OverwritePrompt { get; init; } = true;
}
