namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using System;
using System.Linq;

/// <summary>
/// WinForms を使って Windows のファイル選択ダイアログを表示します。
/// </summary>
public sealed class WindowsFileDialogService : IFileDialogService
{
    public string? OpenFile(OpenFileDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            CheckFileExists = options.CheckFileExists,
            DefaultExt = NormalizeExtension(options.DefaultExtension),
            FileName = options.InitialFileName ?? "",
            Filter = BuildFilter(options.Filters),
            InitialDirectory = options.InitialDirectory ?? "",
            Title = options.Title,
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.FileName
            : null;
    }

    public string? SaveFile(SaveFileDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            AddExtension = options.AddExtension,
            CheckPathExists = options.CheckPathExists,
            DefaultExt = NormalizeExtension(options.DefaultExtension),
            FileName = options.InitialFileName ?? "",
            Filter = BuildFilter(options.Filters),
            InitialDirectory = options.InitialDirectory ?? "",
            OverwritePrompt = options.OverwritePrompt,
            Title = options.Title,
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.FileName
            : null;
    }

    public string? SelectFolder(FolderDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = options.Title,
            SelectedPath = options.InitialDirectory ?? "",
            ShowNewFolderButton = options.AllowCreateFolder,
            UseDescriptionForTitle = true,
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    private static string BuildFilter(System.Collections.Generic.IReadOnlyList<FileDialogFilter> filters) =>
        string.Join(
            "|",
            filters.Select(filter =>
            {
                var patterns = string.Join(";", filter.Patterns);
                return $"{filter.Name} ({patterns})|{patterns}";
            }));

    private static string NormalizeExtension(string? extension) =>
        extension?.TrimStart('.') ?? "";
}
