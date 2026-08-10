namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>
/// OS の IME が提供する、未確定文字列の更新を通知します。
/// </summary>
public interface ITextCompositionService
{
    event Action<TextCompositionState>? CompositionChanged;

    event Action<TextCompositionDiagnostics>? DiagnosticsChanged;

    /// <summary>
    /// プラットフォーム側から届いた変換中テキストを、GUI スレッドで通知する。
    /// </summary>
    void Update();
}

public readonly record struct TextCompositionState(string Text, int CaretIndex, bool IsActive)
{
    public static TextCompositionState Empty { get; } = new("", 0, false);
}

public readonly record struct TextCompositionDiagnostics(bool IsSdlWindowResolved, bool IsWindowProcedureAttached)
{
    public static TextCompositionDiagnostics Empty { get; } = new(false, false);
}
