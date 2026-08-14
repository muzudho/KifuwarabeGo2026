namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.Shared.PopupFilePathTooltip;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using static KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule.TournamentRulesScreenBounds;

/// <summary>
/// ［大会ルール選択画面］
/// </summary>
public sealed partial class GoScreenRenderer
{
    public int GetTournamentRulesAddPanelDisplayNameCaretIndex(Point point, string text) =>
        GetTextBoxCaretIndex(point.X, text, TournamentRulesAddPanelDisplayNameTextBounds, 0.46f);


    public static bool TryGetTournamentRulesSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
    {
        if (session.TournamentRulesDialogSelectionIndex < 0 || session.TournamentRulesDialogSelectionIndex >= session.TournamentRulesList.Count)
        {
            text = string.Empty;
            return false;
        }

        var path = session.TournamentRulesList[session.TournamentRulesDialogSelectionIndex].FilePath;
        return PopupFilePathTooltip.TryGetCopyText(
            StickyNoteScreenId.TournamentRulesSelection,
            StickyNoteKind.TournamentRulesPathHint,
            TournamentRulesSelectionDialogPropertyRowBounds(6),
            path,
            point,
            out text);
    }


    private void DrawTournamentRulesSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesSelectionDialogOpen)
        {
            return;
        }

        var screen = TournamentRulesScreen.Default;

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(TournamentRulesSelectionDialogBounds.X + 18, TournamentRulesSelectionDialogBounds.Y + 20, TournamentRulesSelectionDialogBounds.Width, TournamentRulesSelectionDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(TournamentRulesSelectionDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(TournamentRulesSelectionDialogBounds, 2, new Color(116, 145, 146));

        DrawText("TOURNAMENT RULES", new Vector2(TournamentRulesSelectionDialogBounds.X + 30, TournamentRulesSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        screen.SelectionCancelButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.SelectionOkButton.Draw(mousePoint, _stationeryDrawingContext);

        DrawText("LIST", new Vector2(TournamentRulesSelectionDialogListBounds.X, TournamentRulesSelectionDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawText("PROPERTIES", new Vector2(TournamentRulesSelectionDialogPropertyBounds.X, TournamentRulesSelectionDialogPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);

        FillRect(TournamentRulesSelectionDialogListBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesSelectionDialogListBounds, 1, new Color(67, 84, 92));

        var startIndex = session.TournamentRulesSelectionPageIndex * GoAppSession.TournamentRulesSelectionPageSize;
        for (var i = 0; i < GoAppSession.TournamentRulesSelectionPageSize; i++)
        {
            var index = startIndex + i;
            if (index >= session.TournamentRulesList.Count)
            {
                break;
            }

            DrawTournamentRulesSelectionListItem(TournamentRulesSelectionDialogListItemBounds(i), session, index, mousePoint);
        }

        DrawTournamentRulesSelectionProperties(session, mousePoint);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.TournamentRulesList.Count / (double)GoAppSession.TournamentRulesSelectionPageSize));
        screen.UpdateSelectionState(session.TournamentRulesList.Count, session.CanDeleteSelectedTournamentRules, session.TournamentRulesSelectionPageIndex, pageCount);
        screen.PreviousPageButton.Draw(mousePoint, _stationeryDrawingContext);
        DrawText($"PAGE {session.TournamentRulesSelectionPageIndex + 1} / {pageCount}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
        screen.NextPageButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.AddButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.EditButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.DuplicateButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.DeleteButton.Draw(mousePoint, _stationeryDrawingContext);
        screen.OrderButton.Draw(mousePoint, _stationeryDrawingContext);
        DrawTournamentRulesDeleteConfirmation(session, mousePoint);
        DrawCatalogOrderEditor(
            session.TournamentRulesOrderEditor,
            "TOURNAMENT RULES",
            mousePoint,
            rules => rules.DisplayName,
            rules => $"{rules.Rule}  {rules.BoardSize}x{rules.BoardSize}  KOMI {FormatKomi(rules.Komi)}");
    }


    private void DrawTournamentRulesAddPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesAddPanelOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(TournamentRulesAddPanelBounds.X + 18, TournamentRulesAddPanelBounds.Y + 20, TournamentRulesAddPanelBounds.Width, TournamentRulesAddPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(TournamentRulesAddPanelBounds, new Color(19, 24, 31, 248));
        DrawRect(TournamentRulesAddPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsTournamentRulesEditPanelMode ? "EDIT TOURNAMENT RULES" : "ADD TOURNAMENT RULES", new Vector2(TournamentRulesAddPanelBounds.X + 30, TournamentRulesAddPanelBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        var rulesScreen = TournamentRulesScreen.Default;
        var page = EditTournamentRulePage.Default;
        page.UpdateState(session.IsTournamentRulesDirty, session.RuleKind, session.BoardSize, session.CurrentMode.Kind);
        page.DiscardButton.Draw(mousePoint, _stationeryDrawingContext);
        page.SaveButton.Draw(mousePoint, _stationeryDrawingContext);

        FillRect(TournamentRulesAddPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesAddPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawDisplayNameTextBox(session, mousePoint);
        DrawTournamentRulesFieldLabel("RULE", new Rectangle(AddPanelControlX, 319, 668, 50));
        page.JapaneseRuleButton.Draw(mousePoint, _stationeryDrawingContext);
        page.PureGoRuleButton.Draw(mousePoint, _stationeryDrawingContext);
        page.ChineseRuleButton.Draw(mousePoint, _stationeryDrawingContext);
        DrawTournamentRulesFieldLabel(
            "BOARD SIZE",
            new Rectangle(AddPanelControlX, page.BoardSize9Button.Bounds.Y, 668, 50));
        page.BoardSize9Button.Draw(mousePoint, _stationeryDrawingContext);
        page.BoardSize13Button.Draw(mousePoint, _stationeryDrawingContext);
        page.BoardSize19Button.Draw(mousePoint, _stationeryDrawingContext);
        TournamentRulesScreen.Default.KomiField.Draw(session.Komi, mousePoint,
            new TournamentRuleKomiFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, _stationeryDrawingContext));
        TournamentRulesScreen.Default.TimeField.Draw(session.MainTime, mousePoint,
            new TournamentRuleTimeFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, _stationeryDrawingContext));
        TournamentRulesScreen.Default.MoveLimitField.Draw(session.MoveLimit, mousePoint,
            new TournamentRuleMoveLimitFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, _stationeryDrawingContext));
        DrawFilePathSelector(session, mousePoint);
    }


    private void DrawTournamentRulesSelectionListItem(Rectangle bounds, GoAppSession session, int index, Point mousePoint)
    {
        var rules = session.TournamentRulesList[index];
        var selected = index == session.TournamentRulesDialogSelectionIndex;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
        DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 16), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        DrawFittedText(rules.DisplayName, new Rectangle(bounds.X + 62, bounds.Y + 6, bounds.Width - 82, 32), Color.White, 0.5f);
        DrawText($"{rules.Rule}  {rules.BoardSize}x{rules.BoardSize}  KOMI {FormatKomi(rules.Komi)}", new Vector2(bounds.X + 62, bounds.Y + 42), new Color(204, 211, 206), 0.34f);
    }


    private void DrawTournamentRulesSelectionProperties(GoAppSession session, Point mousePoint)
    {
        FillRect(TournamentRulesSelectionDialogPropertyBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesSelectionDialogPropertyBounds, 1, new Color(67, 84, 92));

        if (session.TournamentRulesDialogSelectionIndex < 0 || session.TournamentRulesDialogSelectionIndex >= session.TournamentRulesList.Count)
        {
            DrawText("NO RULES", new Vector2(TournamentRulesSelectionDialogPropertyBounds.X + 24, TournamentRulesSelectionDialogPropertyBounds.Y + 24), Color.White, 0.5f);
            return;
        }

        var rules = session.TournamentRulesList[session.TournamentRulesDialogSelectionIndex];
        var y = TournamentRulesSelectionDialogPropertyBounds.Y + 22;
        DrawPropertyRow(y, "NAME", rules.DisplayName);
        DrawPropertyRow(y + 70, "RULE", rules.Rule.ToString());
        DrawPropertyRow(y + 140, "BOARD", $"{rules.BoardSize} x {rules.BoardSize}");
        DrawPropertyRow(y + 210, "KOMI", FormatKomi(rules.Komi));
        DrawPropertyRow(y + 280, "TIME", FormatMainTime(rules.MainTime));
        DrawPropertyRow(y + 350, "MOVES", FormatMoveLimit(rules.MoveLimit));
        var filePath = string.IsNullOrWhiteSpace(rules.FilePath) ? "-" : rules.FilePath;
        var fileRowBounds = TournamentRulesSelectionDialogPropertyRowBounds(6);
        DrawPathPropertyRow(fileRowBounds, "FILE", string.IsNullOrWhiteSpace(rules.FilePath) ? "-" : Path.GetFileName(rules.FilePath));
        DrawPathTooltipIfHovered(fileRowBounds, filePath, mousePoint);
    }


    private void DrawTournamentRulesDeleteConfirmation(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesDeleteConfirmationOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(TournamentRulesDeleteConfirmationBounds.X + 12, TournamentRulesDeleteConfirmationBounds.Y + 14, TournamentRulesDeleteConfirmationBounds.Width, TournamentRulesDeleteConfirmationBounds.Height), new Color(0, 0, 0, 150));
        FillRect(TournamentRulesDeleteConfirmationBounds, new Color(24, 29, 36, 252));
        DrawRect(TournamentRulesDeleteConfirmationBounds, 2, new Color(255, 183, 146));

        DrawText("DELETE TOURNAMENT RULES", new Vector2(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 24), new Color(255, 230, 160), 0.62f);
        DrawFittedText($"{session.TournamentRulesDeleteConfirmationFileName} will be deleted.", new Rectangle(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 92, TournamentRulesDeleteConfirmationBounds.Width - 56, 42), Color.White, 0.5f);
        DrawText("DELETE?", new Vector2(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 150), new Color(180, 195, 195), 0.46f);
        TournamentRulesScreen.Default.DeleteCancelButton.Draw(mousePoint, _stationeryDrawingContext);
        TournamentRulesScreen.Default.DeleteConfirmButton.Draw(mousePoint, _stationeryDrawingContext);
    }


}
