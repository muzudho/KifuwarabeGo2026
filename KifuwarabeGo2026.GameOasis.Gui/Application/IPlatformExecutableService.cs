namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System.Collections.Generic;

/// <summary>
/// 対象OSにおける実行ファイル名と選択フィルターを提供します。
/// </summary>
public interface IPlatformExecutableService
{
    IReadOnlyList<FileDialogFilter> SelectionFilters { get; }

    string GetFileName(string baseName);
}
