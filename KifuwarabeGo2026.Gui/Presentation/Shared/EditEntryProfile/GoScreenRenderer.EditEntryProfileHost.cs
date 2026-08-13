namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

/// <summary>GoScreenRenderer と［EDIT ENTRY PROFILE］コンポーネントを接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public EditEntryProfile EditEntryProfile { get; } = new();

    public int GetPlayerEditPanelCaretIndex(Point point, EntryProfileEditField field, string text) =>
        EditEntryProfile.GetCaretIndex(point, field, text, GetTextBoxCaretIndex);

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint) =>
        EditEntryProfile.Draw(
            session,
            mousePoint,
            _stickyNoteScreen,
            new EditEntryProfileDrawingCallbacks(
                VirtualScreen.Width,
                VirtualScreen.Height,
                FillRect,
                DrawRoundedFill,
                DrawRect,
                DrawText,
                DrawFittedText,
                DrawCommandButton,
                DrawIconStone,
                DrawPlayerRoleFaceIcon,
                DrawTextBoxSelection,
                DrawTextBoxCaret,
                DrawEditableTextEditHint,
                bounds => DrawPlayerEditHint("CHANGE", bounds),
                DrawLine,
                DrawDynamicOptionText));
}
