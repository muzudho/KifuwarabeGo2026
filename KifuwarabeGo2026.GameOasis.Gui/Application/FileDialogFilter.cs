namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System.Collections.Generic;

/// <summary>
/// ファイルダイアログに表示する、OS 非依存のファイル種類です。
/// </summary>
public sealed record FileDialogFilter(
    string Name,
    IReadOnlyList<string> Patterns);
