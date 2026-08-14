namespace KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;

using KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenshotEffect;
using KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;
using KifuwarabeGo2026.Gui.Presentation.Shared.PopupFilePathTooltip;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupTimeUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;

/// <summary>
/// ページに依存せず、画面の前面へ表示する共通 UI とその状態を所有します。
/// </summary>
public sealed class HeadUpDisplay
{
    public static HeadUpDisplay Default { get; } = new();

    private HeadUpDisplay()
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
