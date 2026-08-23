namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.LocalMatch.Play;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RightSidePanel;

/// <summary>ローカル対局の対局中ページを描画します。</summary>
public sealed class LocalMatchPlayPage
{
    public static LocalMatchPlayPage Default { get; } = new();

    private LocalMatchPlayPage()
    {
    }

    public LocalMatchPlayRightSidePanel RightSidePanel { get; } = new();
}
