namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS のデスクトップ環境でファイルや保存場所を開きます。
/// </summary>
public interface IDesktopLauncher
{
    void OpenTextFile(string path);

    DesktopOpenResult OpenFileWithPreferredApplication(
        string path,
        string preferredApplication);

    void OpenDirectory(string path);

    void RevealFile(string path);

    void TailTextFile(string path, string windowTitle);
}
