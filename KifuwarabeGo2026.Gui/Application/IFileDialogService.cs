namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS のファイル選択ダイアログを表示します。
/// </summary>
public interface IFileDialogService
{
    string? OpenFile(OpenFileDialogOptions options);

    string? SaveFile(SaveFileDialogOptions options);

    string? SelectFolder(FolderDialogOptions options);
}
