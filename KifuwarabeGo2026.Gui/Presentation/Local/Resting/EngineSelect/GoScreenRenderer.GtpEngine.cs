namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// ［エンジン選択画面］
/// </summary>
public sealed partial class GoScreenRenderer
{
    private readonly Dictionary<string, Texture2D> _dynamicOptionTextTextures = [];

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


    public static (int Index, int Action)? GetGtpEngineGuiOptionControlHit(Point point, GoAppSession session)
    {
        var start = session.GtpEngineGuiOptionsPageIndex * GoAppSession.GtpEngineGuiOptionsPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineGuiOptionsPageSize; slot++)
        {
            var index = start + slot;
            if (index >= GtpEngineGuiOptions.Specs.Length) break;
            var option = GtpEngineGuiOptions.Specs[index];
            if (option.Type != "button" && GtpEngineGuiOptionDefaultButtonBounds(slot).Contains(point)) return (index, 3);
            var primaryBounds = option.Type == "spin" ? GtpEngineGuiOptionPrimaryButtonBounds(slot) : GtpEngineGuiOptionWideButtonBounds(slot);
            if (primaryBounds.Contains(point)) return (index, 0);
            if (GtpEngineGuiOptionSecondaryButtonBounds(slot).Contains(point)) return (index, 1);
            if (option.Type == "spin" && GtpEngineGuiOptionValueBounds(slot).Contains(point)) return (index, 2);
        }
        return null;
    }

    public static bool GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(Point point) =>
        GtpEngineRandomMoveSelectionDialogCancelButtonBounds.Contains(point);

    public static bool GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(Point point) =>
        GtpEngineRandomMoveSelectionDialogSelectButtonBounds.Contains(point);

    public static int? GetGtpEngineRandomMoveSelectionDialogItemHit(Point point, GoAppSession session)
    {
        var startIndex = session.GtpEngineRandomMoveSelectionPageIndex * GoAppSession.GtpEngineComboSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineComboSelectionPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= GtpEngineGuiOptions.RandomMoveValues.Length) break;
            if (GtpEngineRandomMoveSelectionDialogItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }

    public static int? GetGtpEngineGuiOptionsDialogPagerStep(Point point) =>
        GetPagerStep(point, GtpEngineGuiOptionsPreviousPageButtonBounds, GtpEngineGuiOptionsNextPageButtonBounds);

    public static int? GetGtpEngineRandomMoveSelectionDialogPagerStep(Point point) =>
        GetPagerStep(point, GtpEngineRandomMoveSelectionPreviousPageButtonBounds, GtpEngineRandomMoveSelectionNextPageButtonBounds);

    private static int? GetPagerStep(Point point, Rectangle previousBounds, Rectangle nextBounds)
    {
        if (previousBounds.Contains(point)) return -1;
        return nextBounds.Contains(point) ? 1 : null;
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
        DrawCommandButton(GtpEngineSelectionDialogOkButtonBounds, "SELECT", false, mousePoint, scale: 0.34f);

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
        DrawText($"PAGE {session.GtpEngineSelectionPageIndex + 1} / {pageCount}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
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
        DrawText("Text values (max 10000 characters)", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 32, GtpEngineGuiOptionsDialogBounds.Y + 116), new Color(118, 139, 143), 0.3f);

        var startIndex = session.GtpEngineGuiOptionsPageIndex * GoAppSession.GtpEngineGuiOptionsPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineGuiOptionsPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= GtpEngineGuiOptions.Specs.Length) break;
            DrawGtpEngineGuiOptionRow(session, GtpEngineGuiOptions.Specs[index], slot, mousePoint);
        }

        DrawPager(
            session.GtpEngineGuiOptionsPageIndex,
            session.GetGtpEngineGuiOptionsPageCount(),
            GtpEngineGuiOptionsPreviousPageButtonBounds,
            GtpEngineGuiOptionsNextPageButtonBounds,
            GtpEngineGuiOptionsPageLabelBounds,
            mousePoint);

        DrawCommandButton(GtpEngineGuiOptionsDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.4f);
        DrawCommandButton(GtpEngineGuiOptionsDialogOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);
        DrawGtpEngineGuiOptionValueTooltip(session, mousePoint);
        DrawGtpEngineRandomMoveSelectionDialog(session, mousePoint);
    }

    private void DrawGtpEngineGuiOptionRow(GoAppSession session, GtpEngineGuiOptionSpec option, int slot, Point mousePoint)
    {
        var row = GtpEngineGuiOptionRowBounds(slot);
        var value = session.GetGtpEngineGuiOptionDraft(option);
        DrawDataRowFrame(row);
        DrawUiLabel(UiLabel.InCompactRow(option.Label, row));
        var valueBounds = option.Type == "spin" ? GtpEngineGuiOptionSpinValueBounds(slot) : GtpEngineGuiOptionValueBounds(slot);
        var rowValue = option.Type switch
        {
            "button" => "Runs before next game",
            "string" or "filename" => AbbreviateOptionValue(value, 28),
            _ => value,
        };
        DrawDynamicOptionText(string.IsNullOrEmpty(rowValue) ? "<empty>" : rowValue, valueBounds, Color.White, 0.34f);
        if (option.Type == "spin" && option.Min is { } min && option.Max is { } max)
            DrawFittedText($"{min} .. {max}", GtpEngineGuiOptionRangeBounds(slot), new Color(118, 139, 143), 0.24f);
        if (option.Type != "button")
            DrawCommandButton(GtpEngineGuiOptionDefaultButtonBounds(slot), "DEFAULT", false, mousePoint, scale: 0.3f);
        switch (option.Type)
        {
            case "check":
                DrawCommandButton(GtpEngineGuiOptionWideButtonBounds(slot), bool.TryParse(value, out var enabled) && enabled ? "ON" : "OFF", enabled, mousePoint, scale: 0.34f);
                break;
            case "spin":
                DrawCommandButton(GtpEngineGuiOptionPrimaryButtonBounds(slot), "-", false, mousePoint, scale: 0.42f);
                DrawCommandButton(GtpEngineGuiOptionSecondaryButtonBounds(slot), "+", false, mousePoint, scale: 0.42f);
                break;
            case "combo":
                DrawCommandButton(GtpEngineGuiOptionWideButtonBounds(slot), "SELECT", false, mousePoint, scale: 0.28f);
                break;
            case "string":
                DrawCommandButton(GtpEngineGuiOptionWideButtonBounds(slot), "EDIT", false, mousePoint, scale: 0.3f);
                break;
            case "filename":
                DrawCommandButton(GtpEngineGuiOptionWideButtonBounds(slot), "REF", false, mousePoint, scale: 0.3f);
                break;
            case "button":
                var queued = bool.TryParse(value, out var isQueued) && isQueued;
                DrawCommandButton(GtpEngineGuiOptionWideButtonBounds(slot), queued ? "QUEUED" : "EXECUTE", queued, mousePoint, scale: queued ? 0.27f : 0.25f);
                break;
        }
    }

    private void DrawGtpEngineGuiOptionValueTooltip(GoAppSession session, Point mousePoint)
    {
        var startIndex = session.GtpEngineGuiOptionsPageIndex * GoAppSession.GtpEngineGuiOptionsPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineGuiOptionsPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= GtpEngineGuiOptions.Specs.Length) break;
            var option = GtpEngineGuiOptions.Specs[index];
            if (option.Type is not ("string" or "filename") || !GtpEngineGuiOptionValueBounds(slot).Contains(mousePoint)) continue;
            var value = session.GetGtpEngineGuiOptionDraft(option);
            if (value.Length <= 28) continue;

            FillRect(new Rectangle(GtpEngineGuiOptionTooltipBounds.X + 8, GtpEngineGuiOptionTooltipBounds.Y + 10, GtpEngineGuiOptionTooltipBounds.Width, GtpEngineGuiOptionTooltipBounds.Height), new Color(0, 0, 0, 150));
            FillRect(GtpEngineGuiOptionTooltipBounds, new Color(30, 36, 43, 252));
            DrawRect(GtpEngineGuiOptionTooltipBounds, 2, new Color(147, 244, 200));
            DrawText(option.Label, new Vector2(GtpEngineGuiOptionTooltipBounds.X + 18, GtpEngineGuiOptionTooltipBounds.Y + 12), new Color(180, 195, 195), 0.32f);
            DrawDynamicOptionText(string.IsNullOrEmpty(value) ? "<empty>" : AbbreviateOptionValue(value, 100), new Rectangle(GtpEngineGuiOptionTooltipBounds.X + 18, GtpEngineGuiOptionTooltipBounds.Y + 42, GtpEngineGuiOptionTooltipBounds.Width - 36, 42), Color.White, 0.38f);
            return;
        }
    }

    private static string AbbreviateOptionValue(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters ? value : value[..Math.Max(0, maximumCharacters - 3)] + "...";

    private void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            DrawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_dynamicOptionTextTextures.TryGetValue(text, out var texture))
        {
            using var font = new System.Drawing.Font("Meiryo", 28, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            var measured = System.Windows.Forms.TextRenderer.MeasureText(text, font, new System.Drawing.Size(int.MaxValue, int.MaxValue), System.Windows.Forms.TextFormatFlags.NoPadding);
            using var bitmap = new System.Drawing.Bitmap(Math.Max(1, measured.Width), Math.Max(1, measured.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                System.Windows.Forms.TextRenderer.DrawText(graphics, text, font, new System.Drawing.Point(0, 0), System.Drawing.Color.White, System.Windows.Forms.TextFormatFlags.NoPadding);
            }
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[text] = texture;
        }

        var targetHeight = MathF.Min(bounds.Height, _font.LineSpacing * scale);
        var fittedScale = MathF.Min(bounds.Width / (float)texture.Width, targetHeight / texture.Height);
        _spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y + (bounds.Height - (int)(texture.Height * fittedScale)) / 2, (int)(texture.Width * fittedScale), (int)(texture.Height * fittedScale)), color);
    }

    private void DrawGtpEngineRandomMoveSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineRandomMoveSelectionDialogOpen) return;

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(GtpEngineRandomMoveSelectionDialogBounds.X + 14, GtpEngineRandomMoveSelectionDialogBounds.Y + 16, GtpEngineRandomMoveSelectionDialogBounds.Width, GtpEngineRandomMoveSelectionDialogBounds.Height), new Color(0, 0, 0, 150));
        FillRect(GtpEngineRandomMoveSelectionDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(GtpEngineRandomMoveSelectionDialogBounds, 2, new Color(116, 145, 146));
        DrawText("SELECT ITEM", new Vector2(GtpEngineRandomMoveSelectionDialogBounds.X + 30, GtpEngineRandomMoveSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.68f);
        DrawText("RandomMove", new Vector2(GtpEngineRandomMoveSelectionDialogBounds.X + 48, GtpEngineRandomMoveSelectionDialogBounds.Y + 80), new Color(180, 195, 195), 0.4f);

        var startIndex = session.GtpEngineRandomMoveSelectionPageIndex * GoAppSession.GtpEngineComboSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineComboSelectionPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= GtpEngineGuiOptions.RandomMoveValues.Length) break;
            var bounds = GtpEngineRandomMoveSelectionDialogItemBounds(slot);
            var selected = index == session.GtpEngineRandomMoveSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(15, 20, 26));
            DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(67, 84, 92));
            DrawFittedText(GtpEngineGuiOptions.RandomMoveValues[index], new Rectangle(bounds.X + 24, bounds.Y + 8, bounds.Width - 48, bounds.Height - 16), Color.White, 0.46f);
        }

        DrawPager(
            session.GtpEngineRandomMoveSelectionPageIndex,
            session.GetGtpEngineRandomMoveSelectionPageCount(),
            GtpEngineRandomMoveSelectionPreviousPageButtonBounds,
            GtpEngineRandomMoveSelectionNextPageButtonBounds,
            GtpEngineRandomMoveSelectionPageLabelBounds,
            mousePoint);

        DrawCommandButton(GtpEngineRandomMoveSelectionDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.38f);
        DrawCommandButton(GtpEngineRandomMoveSelectionDialogSelectButtonBounds, "SELECT", false, mousePoint, scale: 0.36f);
    }

    private void DrawPager(int pageIndex, int pageCount, Rectangle previousBounds, Rectangle nextBounds, Rectangle labelBounds, Point mousePoint)
    {
        DrawCommandButton(previousBounds, "PREV", false, mousePoint, enabled: pageIndex > 0, scale: 0.34f);
        DrawFittedText($"PAGE {pageIndex + 1} / {pageCount}", labelBounds, new Color(227, 224, 210), 0.38f);
        DrawCommandButton(nextBounds, "NEXT", false, mousePoint, enabled: pageIndex < pageCount - 1, scale: 0.34f);
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


    private static Rectangle GtpEngineSelectionDialogPropertyBounds => new(950, 270, 700, 532);


    private static Rectangle GtpEngineSelectionDialogCancelButtonBounds => new(1368, 156, 132, 48);


    private static Rectangle GtpEngineSelectionDialogOkButtonBounds => new(1518, 156, 132, 48);


    private static Rectangle GtpEngineSelectionDialogPreviousPageButtonBounds => new(730, 816, 90, 44);


    private static Rectangle GtpEngineSelectionDialogNextPageButtonBounds => new(830, 816, 90, 44);


    private static Rectangle GtpEngineSelectionDialogAddButtonBounds => new(270, 874, 100, 44);


    private static Rectangle GtpEngineSelectionDialogEditButtonBounds => new(380, 874, 100, 44);


    private static Rectangle GtpEngineSelectionDialogDuplicateButtonBounds => new(490, 874, 120, 44);


    private static Rectangle GtpEngineSelectionDialogDeleteButtonBounds => new(620, 874, 100, 44);


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


    private static Rectangle GtpEngineGuiOptionsDialogBounds => new(570, 180, 780, 700);


    private static Rectangle GtpEngineGuiOptionRowBounds(int slot) => new(GtpEngineGuiOptionsDialogBounds.X + 56, GtpEngineGuiOptionsDialogBounds.Y + 150 + slot * 68, GtpEngineGuiOptionsDialogBounds.Width - 112, 60);
    private static Rectangle GtpEngineGuiOptionValueBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).X + 166, GtpEngineGuiOptionRowBounds(slot).Y + 8, 252, 44);
    private static Rectangle GtpEngineGuiOptionSpinValueBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).X + 166, GtpEngineGuiOptionRowBounds(slot).Y + 8, 112, 44);
    private static Rectangle GtpEngineGuiOptionRangeBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).X + 294, GtpEngineGuiOptionRowBounds(slot).Y + 12, 126, 36);
    private static Rectangle GtpEngineGuiOptionTooltipBounds => new(GtpEngineGuiOptionsDialogBounds.X + 40, GtpEngineGuiOptionsDialogBounds.Y + 520, GtpEngineGuiOptionsDialogBounds.Width - 80, 100);
    private static Rectangle GtpEngineGuiOptionDefaultButtonBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).Right - 94, GtpEngineGuiOptionRowBounds(slot).Y + 10, 82, 40);
    private static Rectangle GtpEngineGuiOptionPrimaryButtonBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).Right - 220, GtpEngineGuiOptionRowBounds(slot).Y + 10, 54, 40);
    private static Rectangle GtpEngineGuiOptionSecondaryButtonBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).Right - 160, GtpEngineGuiOptionRowBounds(slot).Y + 10, 54, 40);
    private static Rectangle GtpEngineGuiOptionWideButtonBounds(int slot) => new(GtpEngineGuiOptionRowBounds(slot).Right - 220, GtpEngineGuiOptionRowBounds(slot).Y + 10, 114, 40);

    private static Rectangle GtpEngineGuiOptionsPreviousPageButtonBounds => new(GtpEngineGuiOptionsDialogBounds.X + 410, GtpEngineGuiOptionsDialogBounds.Y + 450, 100, 44);

    private static Rectangle GtpEngineGuiOptionsPageLabelBounds => new(GtpEngineGuiOptionsDialogBounds.X + 218, GtpEngineGuiOptionsDialogBounds.Y + 456, 180, 32);

    private static Rectangle GtpEngineGuiOptionsNextPageButtonBounds => new(GtpEngineGuiOptionsDialogBounds.X + 520, GtpEngineGuiOptionsDialogBounds.Y + 450, 100, 44);


    private static Rectangle GtpEngineGuiOptionsDialogCancelButtonBounds => new(GtpEngineGuiOptionsDialogBounds.Right - 330, GtpEngineGuiOptionsDialogBounds.Y + 20, 140, 52);


    private static Rectangle GtpEngineGuiOptionsDialogOkButtonBounds => new(GtpEngineGuiOptionsDialogBounds.Right - 170, GtpEngineGuiOptionsDialogBounds.Y + 20, 140, 52);

    private static Rectangle GtpEngineRandomMoveSelectionDialogBounds => new(610, 238, 700, 588);

    private static Rectangle GtpEngineRandomMoveSelectionDialogItemBounds(int index) =>
        new(GtpEngineRandomMoveSelectionDialogBounds.X + 48, GtpEngineRandomMoveSelectionDialogBounds.Y + 112 + index * 76, GtpEngineRandomMoveSelectionDialogBounds.Width - 96, 60);

    private static Rectangle GtpEngineRandomMoveSelectionPreviousPageButtonBounds => new(GtpEngineRandomMoveSelectionDialogBounds.X + 340, GtpEngineRandomMoveSelectionDialogBounds.Y + 430, 100, 44);

    private static Rectangle GtpEngineRandomMoveSelectionPageLabelBounds => new(GtpEngineRandomMoveSelectionDialogBounds.X + 148, GtpEngineRandomMoveSelectionDialogBounds.Y + 436, 180, 32);

    private static Rectangle GtpEngineRandomMoveSelectionNextPageButtonBounds => new(GtpEngineRandomMoveSelectionDialogBounds.X + 450, GtpEngineRandomMoveSelectionDialogBounds.Y + 430, 100, 44);

    private static Rectangle GtpEngineRandomMoveSelectionDialogCancelButtonBounds => new(GtpEngineRandomMoveSelectionDialogBounds.Right - 330, GtpEngineRandomMoveSelectionDialogBounds.Y + 20, 140, 52);

    private static Rectangle GtpEngineRandomMoveSelectionDialogSelectButtonBounds => new(GtpEngineRandomMoveSelectionDialogBounds.Right - 170, GtpEngineRandomMoveSelectionDialogBounds.Y + 20, 140, 52);


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
