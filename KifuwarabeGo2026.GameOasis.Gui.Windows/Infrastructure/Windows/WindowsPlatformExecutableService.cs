namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using System;
using System.Collections.Generic;

/// <summary>
/// Windowsの実行ファイル名と選択フィルターを提供します。
/// </summary>
public sealed class WindowsPlatformExecutableService : IPlatformExecutableService
{
    public IReadOnlyList<FileDialogFilter> SelectionFilters { get; } =
    [
        new FileDialogFilter("Executable files", ["*.exe"]),
        new FileDialogFilter("All files", ["*.*"]),
    ];

    public string GetFileName(string baseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        return baseName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : baseName + ".exe";
    }
}
