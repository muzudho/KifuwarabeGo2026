namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using static KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule.TournamentRulesScreenBounds;

/// <summary>
/// ［大会ルール選択画面］
/// </summary>
public sealed partial class GoScreenRenderer
{
    private readonly TournamentRuleKomiField _tournamentRuleKomiField = new();
    private readonly TournamentRuleMoveLimitField _tournamentRuleMoveLimitField = new();
    private readonly TournamentRuleTimeField _tournamentRuleTimeField = new();

    public static bool GetTournamentRulesBrowseButtonHit(Point point) =>
        TournamentRulesSelectButtonBounds.Contains(point);


    public static bool GetTournamentRulesAddPanelCloseButtonHit(Point point) =>
        TournamentRulesAddPanelCloseButtonBounds.Contains(point);


    public static bool GetTournamentRulesAddPanelDisplayNameBoxHit(Point point) =>
        TournamentRulesAddPanelDisplayNameTextBounds.Contains(point);


    public int GetTournamentRulesAddPanelDisplayNameCaretIndex(Point point, string text) =>
        GetTextBoxCaretIndex(point.X, text, TournamentRulesAddPanelDisplayNameTextBounds, 0.46f);


    public static bool TryGetTournamentRulesSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
    {
        text = "";
        if (session.TournamentRulesDialogSelectionIndex < 0 || session.TournamentRulesDialogSelectionIndex >= session.TournamentRulesList.Count)
        {
            return false;
        }

        var path = session.TournamentRulesList[session.TournamentRulesDialogSelectionIndex].FilePath;
        if (string.IsNullOrWhiteSpace(path) || !PathTooltipCopyButtonBounds(TournamentRulesSelectionDialogPropertyRowBounds(6)).Contains(point))
        {
            return false;
        }

        text = path;
        return true;
    }


    public static bool GetSaveTournamentRulesButtonHit(Point point) => SaveTournamentRulesButtonBounds.Contains(point);


    private void DrawTournamentRulesSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesSelectionDialogOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(TournamentRulesSelectionDialogBounds.X + 18, TournamentRulesSelectionDialogBounds.Y + 20, TournamentRulesSelectionDialogBounds.Width, TournamentRulesSelectionDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(TournamentRulesSelectionDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(TournamentRulesSelectionDialogBounds, 2, new Color(116, 145, 146));

        DrawText("TOURNAMENT RULES", new Vector2(TournamentRulesSelectionDialogBounds.X + 30, TournamentRulesSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(TournamentRulesSelectionDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(TournamentRulesSelectionDialogOkButtonBounds, "SELECT", false, mousePoint, scale: 0.34f);

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
        DrawCommandButton(TournamentRulesSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.TournamentRulesSelectionPageIndex + 1} / {pageCount}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogAddButtonBounds, "ADD", false, mousePoint, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogEditButtonBounds, "EDIT", false, mousePoint, enabled: session.TournamentRulesList.Count > 0, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.TournamentRulesList.Count > 0, scale: 0.34f);
        DrawCommandButton(TournamentRulesSelectionDialogDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedTournamentRules, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogOrderButtonBounds, "ORDER", false, mousePoint, enabled: session.TournamentRulesList.Count > 1, scale: 0.38f);
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
        DrawCommandButton(TournamentRulesAddPanelCloseButtonBounds, "DISCARD", false, mousePoint, enabled: session.IsTournamentRulesDirty, scale: 0.30f);
        DrawCommandButton(SaveTournamentRulesButtonBounds, session.IsTournamentRulesDirty ? "SAVE & CLOSE" : "CLOSE", false, mousePoint,
            scale: session.IsTournamentRulesDirty ? 0.27f : 0.34f);

        FillRect(TournamentRulesAddPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesAddPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawDisplayNameTextBox(session, mousePoint);
        DrawTournamentRulesFieldLabel("RULE", new Rectangle(AddPanelControlX, 319, 668, 50));
        DrawRuleKindButtons(session.RuleKind, mousePoint);
        DrawTournamentRulesFieldLabel(
            "BOARD SIZE",
            new Rectangle(AddPanelControlX, AddPanelBoardSizeButtonY, 668, 50));
        DrawBoardSizeButtons(session.BoardSize, mousePoint, AddPanelBoardSizeButtonY);
        _tournamentRuleKomiField.Draw(session.Komi, mousePoint,
            new TournamentRuleKomiFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, this,
                new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText)));
        _tournamentRuleTimeField.Draw(session.MainTime, mousePoint,
            new TournamentRuleTimeFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, this,
                new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText)));
        _tournamentRuleMoveLimitField.Draw(session.MoveLimit, mousePoint,
            new TournamentRuleMoveLimitFieldDrawingCallbacks(DrawTournamentRulesFieldLabel, DrawFittedText, this,
                new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText)));
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
        DrawCommandButton(TournamentRulesDeleteConfirmationCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.42f);
        DrawCommandButton(TournamentRulesDeleteConfirmationConfirmButtonBounds, "DELETE", false, mousePoint, scale: 0.42f);
    }


    private static Rectangle TournamentRulesAddPanelBounds => new(430, 126, 1060, 820);


    private static Rectangle TournamentRulesAddPanelEditorBounds => new(520, 228, 880, 590);


    private static Rectangle TournamentRulesAddPanelCloseButtonBounds => new(1144, 156, 132, 48);


    private static Rectangle TournamentRulesAddPanelDisplayNameRowBounds => new(AddPanelControlX, 244, 668, 56);


    private static Rectangle TournamentRulesAddPanelDisplayNameTextBounds =>
        new(TournamentRulesAddPanelDisplayNameRowBounds.X + 132, TournamentRulesAddPanelDisplayNameRowBounds.Y + 7, TournamentRulesAddPanelDisplayNameRowBounds.Width - 148, 42);


    private static Rectangle TournamentRulesAddPanelFileRowBounds => new(AddPanelControlX, 676, 668, 56);



    private static Rectangle SaveTournamentRulesButtonBounds => new(1288, 156, 162, 48);
}
