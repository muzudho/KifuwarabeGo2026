namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>
/// OS の IME が提供する、未確定文字列の更新を通知します。
/// </summary>
public interface ITextCompositionService
{
    event Action<TextCompositionState>? CompositionChanged;
}

public readonly record struct TextCompositionState(string Text, int CaretIndex, bool IsActive)
{
    public static TextCompositionState Empty { get; } = new("", 0, false);
}
