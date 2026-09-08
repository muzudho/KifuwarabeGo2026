namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.HeadUpDisplay;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.ScreenTransition;
using StationeryUI.MonoGame.Effects;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.Breadcrumb;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.PopupFilePathTooltip;
using StationeryUI.MonoGame.Controls.PopupNumberUnderline;
using StationeryUI.MonoGame.Controls.PopupTimeUnderline;
using StationeryUI.MonoGame.Controls.SinglelineTextUnderline;
using StationeryUI.MonoGame.Controls.StickyNote;

/// <summary>
/// ページに依存せず、画面の前面へ表示する共通 UI とその状態を所有します。
/// </summary>
public sealed class HeadUpDisplayComponent
{
    public static HeadUpDisplayComponent Default { get; } = new();

    private HeadUpDisplayComponent()
    {
    }

    public Breadcrumb Breadcrumb { get; } = new();

    public TextInputDialog TextInputDialog { get; } = new();

    public ScreenTransition ScreenTransition { get; } = new();

    public ScreenshotEffect ScreenshotEffect { get; } = new();

    public ReviewUnsavedChangesConfirmation ReviewUnsavedChangesConfirmation { get; } = new();

    public PopupNumberUnderline PopupNumberUnderline { get; } = new();

    public PopupTimeUnderline PopupTimeUnderline { get; } = new();

    public PopupFilePathTooltip PopupFilePathTooltip { get; } = new();

    public StickyNoteScreenId StickyNoteScreen { get; set; } = StickyNoteScreenId.Unknown;
}
