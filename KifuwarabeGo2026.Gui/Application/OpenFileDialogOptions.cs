namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;

/// <summary>
/// ファイルを開くダイアログの OS 非依存オプションです。
/// </summary>
public sealed record OpenFileDialogOptions
{
    public required string Title { get; init; }

    public string? InitialDirectory { get; init; }

    public string? InitialFileName { get; init; }

    public string? DefaultExtension { get; init; }

    public IReadOnlyList<FileDialogFilter> Filters { get; init; } = [];

    public bool CheckFileExists { get; init; } = true;
}
