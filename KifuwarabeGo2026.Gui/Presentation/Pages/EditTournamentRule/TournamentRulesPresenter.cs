namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.Shared.PopupFilePathTooltip;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using static KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule.TournamentRulesScreenBounds;

/// <summary>
/// ［大会ルール選択画面］
/// </summary>
public sealed class TournamentRulesPresenter
{
    public static TournamentRulesPresenter Default { get; } = new();

    private KfwStationeryDrawingTools _drawingContext = null!;
    private readonly LinkUnderline _settingsFileLink = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly ActionBadgeComponent _editBadge = ActionBadgeComponent.Create("EDIT", Rectangle.Empty);
    private readonly PopupFilePathTooltip _pathTooltip = new();

    private TournamentRulesPresenter() { }

    public int GetAddPanelDisplayNameCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, string text) =>
        drawingContext.GetTextCaretIndex(point.X, text, TournamentRulesAddPanelDisplayNameTextBounds, 0.46f);


    public static bool TryGetSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
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


    public void Draw(
        KfwStationeryDrawingTools drawingContext,
        GoAppSession session,
        Point mousePoint)
    {
        _drawingContext = drawingContext;
        DrawTournamentRulesSelectionDialog(session, mousePoint);
        DrawTournamentRulesAddPanel(session, mousePoint);
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
        screen.SelectionCancelButton.Draw(mousePoint, _drawingContext);
        screen.SelectionOkButton.Draw(mousePoint, _drawingContext);

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
        screen.PreviousPageButton.Draw(mousePoint, _drawingContext);
        DrawText($"PAGE {session.TournamentRulesSelectionPageIndex + 1} / {pageCount}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
        screen.NextPageButton.Draw(mousePoint, _drawingContext);
        screen.AddButton.Draw(mousePoint, _drawingContext);
        screen.EditButton.Draw(mousePoint, _drawingContext);
        screen.DuplicateButton.Draw(mousePoint, _drawingContext);
        screen.DeleteButton.Draw(mousePoint, _drawingContext);
        screen.OrderButton.Draw(mousePoint, _drawingContext);
        DrawTournamentRulesDeleteConfirmation(session, mousePoint);
        CatalogOrderPresenter.Default.Draw(_drawingContext,
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
        page.DiscardButton.Draw(mousePoint, _drawingContext);
        page.SaveButton.Draw(mousePoint, _drawingContext);

        FillRect(TournamentRulesAddPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesAddPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawDisplayNameTextBox(session, mousePoint);
        DrawFieldLabel("RULE", new Rectangle(626, 319, 668, 50));
        page.JapaneseRuleButton.Draw(mousePoint, _drawingContext);
        page.PureGoRuleButton.Draw(mousePoint, _drawingContext);
        page.ChineseRuleButton.Draw(mousePoint, _drawingContext);
        DrawFieldLabel(
            "BOARD SIZE",
            new Rectangle(626, page.BoardSize9Button.Bounds.Y, 668, 50));
        page.BoardSize9Button.Draw(mousePoint, _drawingContext);
        page.BoardSize13Button.Draw(mousePoint, _drawingContext);
        page.BoardSize19Button.Draw(mousePoint, _drawingContext);
        TournamentRulesScreen.Default.KomiField.Draw(session.Komi, mousePoint,
            new TournamentRuleKomiFieldDrawingCallbacks(DrawFieldLabel, DrawFittedText, _drawingContext));
        TournamentRulesScreen.Default.TimeField.Draw(session.MainTime, mousePoint,
            new TournamentRuleTimeFieldDrawingCallbacks(DrawFieldLabel, DrawFittedText, _drawingContext));
        TournamentRulesScreen.Default.MoveLimitField.Draw(session.MoveLimit, mousePoint,
            new TournamentRuleMoveLimitFieldDrawingCallbacks(DrawFieldLabel, DrawFittedText, _drawingContext));
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
        DrawPathTooltip(fileRowBounds, filePath, mousePoint);
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
        TournamentRulesScreen.Default.DeleteCancelButton.Draw(mousePoint, _drawingContext);
        TournamentRulesScreen.Default.DeleteConfirmButton.Draw(mousePoint, _drawingContext);
    }

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);

    public bool IsSettingsFileHit(Point point) => _settingsFileLink.IsHit(point);

    private void DrawDisplayNameTextBox(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesScreen.Default.AddPanelDisplayNameRowBounds;
        var active = session.IsTournamentRulesDisplayNameEditing;
        var text = active ? session.TournamentRulesDisplayNameDraft : session.TournamentDisplayName;
        var textBounds = TournamentRulesScreen.Default.AddPanelDisplayNameTextBounds;
        var hovered = textBounds.Contains(mousePoint);
        DrawText("DISPLAY", new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        _drawingContext.FillRoundedRectangle(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2,
            active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (active) _drawingContext.DrawTextSelection(text, session.TournamentRulesDisplayNameSelectionStart, session.TournamentRulesDisplayNameSelectionLength, textBounds, 0.46f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.46f);
        if (active) _drawingContext.DrawTextCaret(text, session.TournamentRulesDisplayNameCaretIndex, textBounds, 0.46f);
        if (!active && hovered)
        {
            _editBadge.SetAnchorBounds(textBounds);
            _editBadge.Show();
            _editBadge.Draw(_drawingContext);
        }
        else _editBadge.Hide();

        if (!string.IsNullOrWhiteSpace(session.TournamentRulesDisplayNameWarning))
            DrawFittedText(session.TournamentRulesDisplayNameWarning, new Rectangle(758, 740, 536, 28), new Color(255, 183, 146), 0.34f);
    }

    private void DrawFilePathSelector(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesScreen.Default.AddPanelFileRowBounds;
        var path = string.IsNullOrWhiteSpace(session.CurrentTournamentRules.FilePath) ? "-" : session.CurrentTournamentRules.FilePath;
        DrawFieldLabel("SETTINGS", bounds);
        var textBounds = new Rectangle(bounds.X + 132, bounds.Y + 7, bounds.Width - 152, 42);
        _settingsFileLink.Bounds = textBounds;
        _settingsFileLink.SetActionBadge(ActionBadgeComponent.Create("OPEN", textBounds));
        _settingsFileLink.UpdatePointer(mousePoint);
        DrawFittedText(path, textBounds, Color.White, 0.38f);
        _settingsFileLink.Draw(_drawingContext);
    }

    private void DrawPropertyRow(int y, string label, string value) =>
        DrawPathPropertyRow(new Rectangle(TournamentRulesSelectionDialogPropertyBounds.X + 18, y, TournamentRulesSelectionDialogPropertyBounds.Width - 36, 52), label, value);

    private void DrawPathPropertyRow(Rectangle bounds, string label, string value)
    {
        _drawingContext.DrawDataRowFrame(bounds);
        DrawFittedText(label, new Rectangle(bounds.X + 16, bounds.Y + 7, 120, 38), new Color(180, 195, 195), 0.38f);
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPathTooltip(Rectangle bounds, string path, Point mousePoint) =>
        _pathTooltip.Draw(StickyNoteScreenId.TournamentRulesSelection, StickyNoteKind.TournamentRulesPathHint,
            bounds, path, mousePoint, "FILE", ["Tournament rules settings file."], _drawingContext, _drawingContext.DrawDynamicText);

    private void DrawFieldLabel(string label, Rectangle rowBounds)
    {
        var labelBounds = new Rectangle(626, rowBounds.Y, 112, rowBounds.Height);
        var measured = _drawingContext.MeasureText(label);
        var scale = MathF.Min(0.38f, MathF.Min(labelBounds.Width / Math.Max(1f, measured.X), (labelBounds.Height - 8) / Math.Max(1f, measured.Y)));
        DrawText(label, new Vector2(labelBounds.X, labelBounds.Center.Y - measured.Y * scale / 2), new Color(180, 195, 195), scale);
    }
    private static string FormatKomi(decimal komi) => komi.ToString("0.0");
    private static string FormatMainTime(TimeSpan value) => value == TimeSpan.Zero ? "NO LIMIT" : value.TotalHours >= 1 ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}" : $"{value.Minutes:00}:{value.Seconds:00}";
    private static string FormatMoveLimit(int value) => value <= 0 ? "NO LIMIT" : value.ToString();
}
