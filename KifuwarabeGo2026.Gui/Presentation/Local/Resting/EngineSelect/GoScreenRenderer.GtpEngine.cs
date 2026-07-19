namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// ［エンジン選択画面］
/// </summary>
public sealed partial class GoScreenRenderer
{

    public int GetGtpEngineEditPanelCaretIndex(Point point, GtpEngineProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, GtpEngineEditPanelFieldTextBounds(field), 0.42f);


    public static bool GetGtpEngineSelectionDialogOkButtonHit(Point point) =>
        GtpEngineSelectionDialogOkButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogCancelButtonHit(Point point) =>
        GtpEngineSelectionDialogCancelButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogAddButtonHit(Point point) =>
        GtpEngineSelectionDialogAddButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogEditButtonHit(Point point) =>
        GtpEngineSelectionDialogEditButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogDuplicateButtonHit(Point point) =>
        GtpEngineSelectionDialogDuplicateButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogDeleteButtonHit(Point point, bool enabled) =>
        enabled && GtpEngineSelectionDialogDeleteButtonBounds.Contains(point);


    public static bool GetGtpEngineDeleteConfirmationConfirmButtonHit(Point point) =>
        GtpEngineDeleteConfirmationConfirmButtonBounds.Contains(point);


    public static bool GetGtpEngineDeleteConfirmationCancelButtonHit(Point point) =>
        GtpEngineDeleteConfirmationCancelButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelCloseButtonHit(Point point) =>
        GtpEngineEditPanelCloseButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelSaveButtonHit(Point point) =>
        GtpEngineEditPanelSaveButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelFileBrowseButtonHit(Point point) =>
        GtpEngineEditPanelFileBrowseButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelWorkingDirectoryBrowseButtonHit(Point point) =>
        GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelLogButtonHit(Point point) =>
        GtpEngineEditPanelLogButtonBounds.Contains(point);


    public static bool GetGtpEngineEditPanelGuiOptionsButtonHit(Point point) =>
        GtpEngineEditPanelGuiOptionsButtonBounds.Contains(point);


    public static bool GetGtpEngineGuiOptionsDialogOkButtonHit(Point point) =>
        GtpEngineGuiOptionsDialogOkButtonBounds.Contains(point);


    public static bool GetGtpEngineGuiOptionsDialogCancelButtonHit(Point point) =>
        GtpEngineGuiOptionsDialogCancelButtonBounds.Contains(point);


    public static int? GetGtpEngineGuiOptionsDialogRandomMoveStepButtonHit(Point point)
    {
        if (GtpEngineGuiOptionsDialogRandomMovePreviousButtonBounds.Contains(point)) return -1;
        return GtpEngineGuiOptionsDialogRandomMoveNextButtonBounds.Contains(point) ? 1 : null;
    }


    public static GtpEngineProfileEditField? GetGtpEngineEditPanelFieldHit(Point point)
    {
        foreach (var field in GtpEngineEditFields)
        {
            if (GtpEngineEditPanelFieldRowBounds(field).Contains(point))
            {
                return field;
            }
        }

        return null;
    }


    public static bool GetGtpEngineSelectionDialogPreviousPageButtonHit(Point point) =>
        GtpEngineSelectionDialogPreviousPageButtonBounds.Contains(point);


    public static bool GetGtpEngineSelectionDialogNextPageButtonHit(Point point) =>
        GtpEngineSelectionDialogNextPageButtonBounds.Contains(point);


    public static int? GetGtpEngineSelectionDialogListItemHit(Point point, GoAppSession session)
    {
        for (var i = 0; i < GoAppSession.GtpEngineSelectionPageSize; i++)
        {
            if (!GtpEngineSelectionDialogListItemBounds(i).Contains(point))
            {
                continue;
            }

            var index = session.GtpEngineSelectionPageIndex * GoAppSession.GtpEngineSelectionPageSize + i;
            return index < session.GtpEngineProfiles.Count ? index : null;
        }

        return null;
    }


    public static bool TryGetGtpEngineSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
    {
        text = "";
        var selectedIndex = session.GtpEngineDialogSelectionIndex;
        if (selectedIndex < 0 || selectedIndex >= session.GtpEngineProfiles.Count)
        {
            return false;
        }

        var profile = session.GtpEngineProfiles[selectedIndex];

        // 実行ファイルのパスのコピー
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath) && PathTooltipCopyButtonBounds(GtpEngineSelectionDialogPropertyRowBounds(1)).Contains(point))
        {
            text = profile.ExecutablePath;
            return true;
        }

        // 作業用ディレクトリのコピー
        if (!profile.WorkingDirectoryModel.IsEmpty && PathTooltipCopyButtonBounds(GtpEngineSelectionDialogPropertyRowBounds(2)).Contains(point))
        {
            text = profile.WorkingDirectoryModel.Value;
            return true;
        }

        return false;
    }


    public static bool GetBlackGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(BlackEngineButtonY).ContainsBrowseButton(point);


    public static bool GetWhiteGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(WhiteEngineButtonY).ContainsBrowseButton(point);


    private void DrawGtpEngineSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineSelectionDialogOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(GtpEngineSelectionDialogBounds.X + 18, GtpEngineSelectionDialogBounds.Y + 20, GtpEngineSelectionDialogBounds.Width, GtpEngineSelectionDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(GtpEngineSelectionDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(GtpEngineSelectionDialogBounds, 2, new Color(116, 145, 146));

        var target = session.IsGtpEngineSelectionForCgos
            ? session.GtpEngineSelectionTargetStone == GoStone.Black ? "CGOS PLAYER 1" : "CGOS PLAYER 2"
            : session.GtpEngineSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        DrawText($"GTP ENGINE SELECT  {target}", new Vector2(GtpEngineSelectionDialogBounds.X + 30, GtpEngineSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(GtpEngineSelectionDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(GtpEngineSelectionDialogOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);

        DrawText("LIST", new Vector2(GtpEngineSelectionDialogListBounds.X, GtpEngineSelectionDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawText("PROPERTIES", new Vector2(GtpEngineSelectionDialogPropertyBounds.X, GtpEngineSelectionDialogPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);

        FillRect(GtpEngineSelectionDialogListBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogListBounds, 1, new Color(67, 84, 92));

        var startIndex = session.GtpEngineSelectionPageIndex * GoAppSession.GtpEngineSelectionPageSize;
        for (var i = 0; i < GoAppSession.GtpEngineSelectionPageSize; i++)
        {
            var index = startIndex + i;
            if (index >= session.GtpEngineProfiles.Count)
            {
                break;
            }

            DrawGtpEngineSelectionListItem(GtpEngineSelectionDialogListItemBounds(i), session, index, mousePoint);
        }

        DrawGtpEngineSelectionProperties(session, mousePoint);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.GtpEngineProfiles.Count / (double)GoAppSession.GtpEngineSelectionPageSize));
        DrawCommandButton(GtpEngineSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.GtpEngineSelectionPageIndex + 1} / {pageCount}", new Vector2(GtpEngineSelectionDialogBounds.X + 350, GtpEngineSelectionDialogBounds.Bottom - 62), new Color(227, 224, 210), 0.48f);
        DrawCommandButton(GtpEngineSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogAddButtonBounds, "ADD", false, mousePoint, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogEditButtonBounds, "EDIT", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.32f);
        DrawCommandButton(GtpEngineSelectionDialogDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedGtpEngine, scale: 0.42f);
        DrawGtpEngineDeleteConfirmation(session, mousePoint);
    }


    private void DrawGtpEngineEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineEditPanelOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(GtpEngineEditPanelBounds.X + 18, GtpEngineEditPanelBounds.Y + 20, GtpEngineEditPanelBounds.Width, GtpEngineEditPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(GtpEngineEditPanelBounds, new Color(19, 24, 31, 248));
        DrawRect(GtpEngineEditPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsGtpEngineAddPanelMode ? "ADD GTP ENGINE" : "EDIT GTP ENGINE", new Vector2(GtpEngineEditPanelBounds.X + 30, GtpEngineEditPanelBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(GtpEngineEditPanelCloseButtonBounds, "BACK", false, mousePoint, scale: 0.42f);

        FillRect(GtpEngineEditPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineEditPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawGtpEngineEditField(session, GtpEngineProfileEditField.DisplayName, "DISPLAY", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.ExecutablePath, "EXE", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.WorkingDirectory, "WORKDIR", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.Arguments, "ARGS", mousePoint);

        DrawCommandButton(GtpEngineEditPanelGuiOptionsButtonBounds, "GUI OPTIONS", false, mousePoint, scale: 0.42f);

        var logBounds = GtpEngineEditPanelLogRowBounds;
        DrawDataRowFrame(logBounds);
        DrawUiLabel(UiLabel.InCompactRow("GTP LOG", logBounds));
        DrawCommandButton(GtpEngineEditPanelLogButtonBounds, session.GtpEngineEditDraft.EnableGtpLog ? "ON" : "OFF", session.GtpEngineEditDraft.EnableGtpLog, mousePoint, scale: 0.42f);

        if (!string.IsNullOrWhiteSpace(session.GtpEngineEditWarning))
        {
            DrawFittedText(session.GtpEngineEditWarning, new Rectangle(GtpEngineEditPanelEditorBounds.X + 48, GtpEngineEditPanelEditorBounds.Bottom - 74, GtpEngineEditPanelEditorBounds.Width - 96, 34), new Color(255, 183, 146), 0.38f);
        }

        DrawCommandButton(GtpEngineEditPanelSaveButtonBounds, SaveGtpEngineLabel(session), false, mousePoint);
        DrawGtpEngineGuiOptionsDialog(session, mousePoint);
    }


    /// <summary>
    /// GTPエンジンが公開するGUIオプションの編集ダイアログを描画します。
    /// </summary>
    private void DrawGtpEngineGuiOptionsDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineGuiOptionsDialogOpen) return;

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 115));
        FillRect(new Rectangle(GtpEngineGuiOptionsDialogBounds.X + 14, GtpEngineGuiOptionsDialogBounds.Y + 16, GtpEngineGuiOptionsDialogBounds.Width, GtpEngineGuiOptionsDialogBounds.Height), new Color(0, 0, 0, 150));
        FillRect(GtpEngineGuiOptionsDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(GtpEngineGuiOptionsDialogBounds, 2, new Color(116, 145, 146));

        DrawText("GUI OPTIONS", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 30, GtpEngineGuiOptionsDialogBounds.Y + 24), new Color(244, 238, 218), 0.72f);
        DrawText("Settings are sent when the engine starts.", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 32, GtpEngineGuiOptionsDialogBounds.Y + 82), new Color(180, 195, 195), 0.4f);

        DrawDataRowFrame(GtpEngineGuiOptionsDialogRandomMoveRowBounds);
        DrawUiLabel(UiLabel.InCompactRow("RandomMove", GtpEngineGuiOptionsDialogRandomMoveRowBounds));
        DrawFittedText(session.GtpEngineRandomMoveDraft, GtpEngineGuiOptionsDialogRandomMoveValueBounds, Color.White, 0.38f);
        DrawCommandButton(GtpEngineGuiOptionsDialogRandomMovePreviousButtonBounds, "PREV", false, mousePoint, scale: 0.27f);
        DrawCommandButton(GtpEngineGuiOptionsDialogRandomMoveNextButtonBounds, "NEXT", false, mousePoint, scale: 0.27f);

        DrawCommandButton(GtpEngineGuiOptionsDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.4f);
        DrawCommandButton(GtpEngineGuiOptionsDialogOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);
    }


    private void DrawGtpEngineEditField(GoAppSession session, GtpEngineProfileEditField field, string label, Point mousePoint)
    {
        var bounds = GtpEngineEditPanelFieldRowBounds(field);
        var active = session.ActiveGtpEngineEditField == field;
        var hovered = bounds.Contains(mousePoint);
        var text = session.GetGtpEngineEditFieldText(field);
        DrawDataRowFrame(bounds, active, hovered);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));

        var textBounds = GtpEngineEditPanelFieldTextBounds(field);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
        {
            DrawTextBoxCaret(text, session.GtpEngineEditCaretIndex, textBounds, 0.42f);
        }

        if (field == GtpEngineProfileEditField.ExecutablePath)
        {
            DrawCommandButton(GtpEngineEditPanelFileBrowseButtonBounds, "REF", false, mousePoint, scale: 0.34f);
        }

        if (field == GtpEngineProfileEditField.WorkingDirectory)
        {
            DrawCommandButton(GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds, "REF", false, mousePoint, scale: 0.34f);
        }
    }


    private void DrawGtpEngineDeleteConfirmation(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineDeleteConfirmationOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(GtpEngineDeleteConfirmationBounds.X + 12, GtpEngineDeleteConfirmationBounds.Y + 14, GtpEngineDeleteConfirmationBounds.Width, GtpEngineDeleteConfirmationBounds.Height), new Color(0, 0, 0, 150));
        FillRect(GtpEngineDeleteConfirmationBounds, new Color(24, 29, 36, 252));
        DrawRect(GtpEngineDeleteConfirmationBounds, 2, new Color(255, 183, 146));

        DrawText("DELETE GTP ENGINE", new Vector2(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 24), new Color(255, 230, 160), 0.62f);
        DrawFittedText($"{session.GtpEngineDeleteConfirmationName} will be removed from the list.", new Rectangle(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 92, GtpEngineDeleteConfirmationBounds.Width - 56, 42), Color.White, 0.5f);
        DrawText("DELETE?", new Vector2(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 150), new Color(180, 195, 195), 0.46f);
        DrawCommandButton(GtpEngineDeleteConfirmationCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.42f);
        DrawCommandButton(GtpEngineDeleteConfirmationConfirmButtonBounds, "DELETE", false, mousePoint, scale: 0.42f);
    }


    private void DrawGtpEngineSelectionListItem(Rectangle bounds, GoAppSession session, int index, Point mousePoint)
    {
        var profile = session.GtpEngineProfiles[index];
        var selectedIndex = session.GtpEngineDialogSelectionIndex;
        var selected = index == selectedIndex;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
        DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 16), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        DrawFittedText(profile.DisplayName, new Rectangle(bounds.X + 62, bounds.Y + 6, bounds.Width - 82, 32), Color.White, 0.5f);
        DrawFittedText(string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "-" : profile.ExecutablePath, new Rectangle(bounds.X + 62, bounds.Y + 40, bounds.Width - 82, 24), new Color(204, 211, 206), 0.34f);
    }


    private void DrawGtpEngineSelectionProperties(GoAppSession session, Point mousePoint)
    {
        FillRect(GtpEngineSelectionDialogPropertyBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogPropertyBounds, 1, new Color(67, 84, 92));

        var selectedIndex = session.GtpEngineDialogSelectionIndex;
        if (selectedIndex < 0 || selectedIndex >= session.GtpEngineProfiles.Count)
        {
            DrawText("NO ENGINE", new Vector2(GtpEngineSelectionDialogPropertyBounds.X + 24, GtpEngineSelectionDialogPropertyBounds.Y + 24), Color.White, 0.5f);
            return;
        }

        var profile = session.GtpEngineProfiles[selectedIndex];
        var y = GtpEngineSelectionDialogPropertyBounds.Y + 22;
        DrawGtpEnginePropertyRow(y, "NAME", profile.DisplayName);
        var executablePath = string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "-" : profile.ExecutablePath;

        // ［作業用ディレクトリー］が無ければハイフン表示
        var displayWorkingDirectory = profile.WorkingDirectoryModel.DisplayValue;

        var executablePathRowBounds = GtpEngineSelectionDialogPropertyRowBounds(1);
        var workingDirectoryRowBounds = GtpEngineSelectionDialogPropertyRowBounds(2);

        DrawPathPropertyRow(executablePathRowBounds, "EXE", executablePath);
        DrawPathPropertyRow(workingDirectoryRowBounds, "WORKDIR", displayWorkingDirectory);
        DrawGtpEnginePropertyRow(y + 210, "ARGS", string.IsNullOrWhiteSpace(profile.Arguments) ? "-" : profile.Arguments);
        DrawGtpEnginePropertyRow(y + 280, "GTP LOG", profile.EnableGtpLog ? "ON" : "OFF");

        DrawPathTooltipIfHovered(executablePathRowBounds, executablePath, mousePoint);
        DrawPathTooltipIfHovered(workingDirectoryRowBounds, displayWorkingDirectory, mousePoint);
    }


    private void DrawGtpEnginePropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(GtpEngineSelectionDialogPropertyBounds.X + 18, y, GtpEngineSelectionDialogPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }


    private static Rectangle GtpEngineSelectionDialogBounds => new(230, 126, 1460, 820);


    private static Rectangle GtpEngineSelectionDialogListBounds => new(270, 242, 650, 560);


    private static Rectangle GtpEngineSelectionDialogPropertyBounds => new(950, 242, 700, 560);


    private static Rectangle GtpEngineSelectionDialogCancelButtonBounds => new(1368, 156, 132, 48);


    private static Rectangle GtpEngineSelectionDialogOkButtonBounds => new(1518, 156, 132, 48);


    private static Rectangle GtpEngineSelectionDialogPreviousPageButtonBounds => new(270, 854, 150, 52);


    private static Rectangle GtpEngineSelectionDialogNextPageButtonBounds => new(770, 854, 150, 52);


    private static Rectangle GtpEngineSelectionDialogAddButtonBounds => new(898, 854, 140, 52);


    private static Rectangle GtpEngineSelectionDialogEditButtonBounds => new(1058, 854, 140, 52);


    private static Rectangle GtpEngineSelectionDialogDuplicateButtonBounds => new(1218, 854, 140, 52);


    private static Rectangle GtpEngineSelectionDialogDeleteButtonBounds => new(1378, 854, 140, 52);


    private static Rectangle GtpEngineDeleteConfirmationBounds => new(654, 358, 612, 260);


    private static Rectangle GtpEngineDeleteConfirmationCancelButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 298, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);


    private static Rectangle GtpEngineDeleteConfirmationConfirmButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 448, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);


    private static Rectangle GtpEngineEditPanelBounds => new(430, 126, 1060, 820);


    private static Rectangle GtpEngineEditPanelEditorBounds => new(520, 228, 880, 590);


    private static Rectangle GtpEngineEditPanelCloseButtonBounds => new(1318, 156, 132, 48);


    private static Rectangle GtpEngineEditPanelSaveButtonBounds => new(1080, 840, 320, 58);


    private static Rectangle GtpEngineEditPanelFileBrowseButtonBounds => new(
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.ExecutablePath).Right - 112,
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.ExecutablePath).Y + 8,
        96,
        40);


    private static Rectangle GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds => new(
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.WorkingDirectory).Right - 112,
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.WorkingDirectory).Y + 8,
        96,
        40);


    private static Rectangle GtpEngineEditPanelGuiOptionsButtonBounds => new(AddPanelControlX, 590, 300, 56);


    private static Rectangle GtpEngineEditPanelLogRowBounds => new(AddPanelControlX, 660, 668, 56);


    private static Rectangle GtpEngineEditPanelLogButtonBounds => new(GtpEngineEditPanelLogRowBounds.X + 152, GtpEngineEditPanelLogRowBounds.Y + 8, 120, 40);


    private static Rectangle GtpEngineGuiOptionsDialogBounds => new(570, 282, 780, 500);


    private static Rectangle GtpEngineGuiOptionsDialogRandomMoveRowBounds => new(GtpEngineGuiOptionsDialogBounds.X + 56, GtpEngineGuiOptionsDialogBounds.Y + 150, GtpEngineGuiOptionsDialogBounds.Width - 112, 60);


    private static Rectangle GtpEngineGuiOptionsDialogRandomMoveValueBounds => new(GtpEngineGuiOptionsDialogRandomMoveRowBounds.X + 166, GtpEngineGuiOptionsDialogRandomMoveRowBounds.Y + 8, 286, 44);


    private static Rectangle GtpEngineGuiOptionsDialogRandomMovePreviousButtonBounds => new(GtpEngineGuiOptionsDialogRandomMoveRowBounds.Right - 190, GtpEngineGuiOptionsDialogRandomMoveRowBounds.Y + 10, 82, 40);


    private static Rectangle GtpEngineGuiOptionsDialogRandomMoveNextButtonBounds => new(GtpEngineGuiOptionsDialogRandomMoveRowBounds.Right - 94, GtpEngineGuiOptionsDialogRandomMoveRowBounds.Y + 10, 82, 40);


    private static Rectangle GtpEngineGuiOptionsDialogCancelButtonBounds => new(GtpEngineGuiOptionsDialogBounds.Right - 330, GtpEngineGuiOptionsDialogBounds.Bottom - 82, 140, 52);


    private static Rectangle GtpEngineGuiOptionsDialogOkButtonBounds => new(GtpEngineGuiOptionsDialogBounds.Right - 170, GtpEngineGuiOptionsDialogBounds.Bottom - 82, 140, 52);


    private static readonly GtpEngineProfileEditField[] GtpEngineEditFields =
    {
        GtpEngineProfileEditField.DisplayName,
        GtpEngineProfileEditField.ExecutablePath,
        GtpEngineProfileEditField.WorkingDirectory,
        GtpEngineProfileEditField.Arguments,
    };


    private static Rectangle GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField field) => field switch
    {
        GtpEngineProfileEditField.DisplayName => new Rectangle(AddPanelControlX, 264, 668, 56),
        GtpEngineProfileEditField.ExecutablePath => new Rectangle(AddPanelControlX, 348, 668, 56),
        GtpEngineProfileEditField.WorkingDirectory => new Rectangle(AddPanelControlX, 432, 668, 56),
        GtpEngineProfileEditField.Arguments => new Rectangle(AddPanelControlX, 516, 668, 56),
        _ => Rectangle.Empty,
    };


    private static Rectangle GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField field)
    {
        var bounds = GtpEngineEditPanelFieldRowBounds(field);
        var rightPadding = field is GtpEngineProfileEditField.ExecutablePath or GtpEngineProfileEditField.WorkingDirectory ? 282 : 168;
        return new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - rightPadding, 42);
    }


    private static Rectangle GtpEngineSelectionDialogListItemBounds(int index) =>
        new(GtpEngineSelectionDialogListBounds.X + 16, GtpEngineSelectionDialogListBounds.Y + 16 + index * 88, GtpEngineSelectionDialogListBounds.Width - 32, 72);


    private static Rectangle GtpEngineSelectionDialogPropertyRowBounds(int index) =>
        new(GtpEngineSelectionDialogPropertyBounds.X + 18, GtpEngineSelectionDialogPropertyBounds.Y + 22 + index * 70, GtpEngineSelectionDialogPropertyBounds.Width - 36, 52);


    private static LabeledBrowseSelector GtpEngineSelectorBounds(int y) => new(new Rectangle(1144, y - 4, 668, 44), "NAME", "", "SELECT");


    private static string SaveGtpEngineLabel(GoAppSession session) =>
        string.IsNullOrWhiteSpace(session.GtpEngineEditSaveMessage)
            ? "SAVE ENGINE"
            : $"SAVE ENGINE {session.GtpEngineEditSaveMessage}";
}
