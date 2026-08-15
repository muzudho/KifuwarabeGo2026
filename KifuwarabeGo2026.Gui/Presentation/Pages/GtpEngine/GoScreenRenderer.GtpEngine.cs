namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.Shared.PopupFilePathTooltip;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

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

    public static bool GetGtpEngineSelectionDialogOrderButtonHit(Point point) =>
        GtpEngineSelectionDialogOrderButtonBounds.Contains(point);


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

    public static bool GetGtpEngineEditPanelInitialPositionProfileButtonHit(Point point) =>
        GtpEngineEditPanelInitialPositionProfileButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelInitialPositionMethodButtonHit(Point point) =>
        GtpEngineEditPanelInitialPositionMethodButtonBounds.Contains(point);


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
            if (index >= session.ActiveGtpEngineGuiOptionSpecs.Count) break;
            var option = session.ActiveGtpEngineGuiOptionSpecs[index];
            if (option.Type is not ("button" or "string") && GtpEngineGuiOptionDefaultButtonBounds(slot).Contains(point)) return (index, 3);
            if (GtpEngineGuiOptionValueBounds(slot).Contains(point)) return (index, option.Type == "spin" ? 2 : 0);
        }
        return null;
    }

    public static bool GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(Point point) =>
        GtpEngineRandomMoveSelectionDialogCancelButtonBounds.Contains(point);

    public static bool GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(Point point) =>
        GtpEngineRandomMoveSelectionDialogSelectButtonBounds.Contains(point);

    public static int? GetGtpEngineRandomMoveSelectionDialogItemHit(Point point, GoAppSession session)
    {
        var choices = session.GetActiveGtpEngineComboChoices();
        var startIndex = session.GtpEngineRandomMoveSelectionPageIndex * GoAppSession.GtpEngineComboSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineComboSelectionPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= choices.Count) break;
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
        foreach (var field in GtpEngineEditableTextFields)
        {
            if (GtpEngineEditPanelFieldTextBounds(field).Contains(point))
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

        if (PopupFilePathTooltip.TryGetCopyText(
                StickyNoteScreenId.GtpEngineSelection,
                StickyNoteKind.GtpEnginePathHint,
                GtpEngineSelectionDialogPropertyRowBounds(1),
                profile.ExecutablePath,
                point,
                out text)) return true;

        if (PopupFilePathTooltip.TryGetCopyText(
                StickyNoteScreenId.GtpEngineSelection,
                StickyNoteKind.GtpEnginePathHint,
                GtpEngineSelectionDialogPropertyRowBounds(2),
                profile.WorkingDirectoryModel.DisplayValue,
                point,
                out text)) return true;

        return false;
    }


    public static bool GetBlackGtpEngineBrowseButtonHit(Point point) =>
        PlayerSelectorLayout.CreateComputerEngineSelector(SetupRightSidePanel.BlackEngineButtonY).ContainsBrowseButton(point);


    public static bool GetWhiteGtpEngineBrowseButtonHit(Point point) =>
        PlayerSelectorLayout.CreateComputerEngineSelector(SetupRightSidePanel.WhiteEngineButtonY).ContainsBrowseButton(point);

    public static bool GetPonnukiBlackGtpEngineBrowseButtonHit(Point point) =>
        PlayerSelectorLayout.CreateComputerEngineSelector(LocalMatchIntermissionRightSidePanel.BlackEngineButtonY).ContainsBrowseButton(point);

    public static bool GetPonnukiWhiteGtpEngineBrowseButtonHit(Point point) =>
        PlayerSelectorLayout.CreateComputerEngineSelector(LocalMatchIntermissionRightSidePanel.WhiteEngineButtonY).ContainsBrowseButton(point);


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

        DrawText("SELECT ENGINE (GTP)", new Vector2(GtpEngineSelectionDialogBounds.X + 30, GtpEngineSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        var closeLabel = session.IsGtpEngineSelectionForAppProvider ? "CLOSE" : "CANCEL";
        DrawCommandButton(GtpEngineSelectionDialogCancelButtonBounds, closeLabel, false, mousePoint, scale: 0.34f);
        DrawCommandButton(GtpEngineSelectionDialogOkButtonBounds, "USE", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.CanCommitGtpEngineSelection, scale: 0.34f);

        DrawText("LIST", new Vector2(GtpEngineSelectionDialogListBounds.X, GtpEngineSelectionDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawText("PROPERTIES", new Vector2(GtpEngineSelectionDialogPropertyBounds.X, GtpEngineSelectionDialogPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);

        FillRect(GtpEngineSelectionDialogListBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogListBounds, 1, new Color(67, 84, 92));

        if (session.IsGtpEngineCompatibilityLoading)
        {
            DrawGtpEngineSelectionLoadingSkeleton();
        }
        else
        {
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
        }

        if (!session.IsGtpEngineCompatibilityLoading)
            DrawGtpEngineSelectionProperties(session, mousePoint);
        else
            DrawGtpEngineSelectionPropertiesSkeleton();

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.GtpEngineProfiles.Count / (double)GoAppSession.GtpEngineSelectionPageSize));
        DrawCommandButton(GtpEngineSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.GtpEngineSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.GtpEngineSelectionPageIndex + 1} / {pageCount}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.GtpEngineSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogAddButtonBounds, "ADD", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogEditButtonBounds, "EDIT", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.GtpEngineProfiles.Count > 0, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.GtpEngineProfiles.Count > 0, scale: 0.32f);
        DrawCommandButton(GtpEngineSelectionDialogDeleteButtonBounds, "DELETE", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.CanDeleteSelectedGtpEngine, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogOrderButtonBounds, "ORDER", false, mousePoint, enabled: !session.IsGtpEngineCompatibilityLoading && session.GtpEngineProfiles.Count > 1, scale: 0.38f);
        DrawGtpEngineDeleteConfirmation(session, mousePoint);
        DrawCatalogOrderEditor(
            session.GtpEngineOrderEditor,
            "GTP ENGINES",
            mousePoint,
            profile => profile.DisplayName,
            profile => string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "EXECUTABLE NOT SET" : Path.GetFileName(profile.ExecutablePath),
            _ => true);
    }

    private void DrawGtpEngineSelectionLoadingSkeleton()
    {
        var phase = (float)(DateTime.UtcNow.TimeOfDay.TotalSeconds * 2.4d);
        for (var slot = 0; slot < GoAppSession.GtpEngineSelectionPageSize; slot++)
        {
            var bounds = GtpEngineSelectionDialogListItemBounds(slot);
            var highlight = (MathF.Sin(phase - slot * 0.75f) + 1f) * 0.5f;
            FillRect(bounds, new Color(29, 35, 42));
            FillRect(new Rectangle(bounds.X + 18, bounds.Y + 13, 210, 18), Color.Lerp(new Color(48, 56, 64), new Color(86, 96, 106), highlight));
            FillRect(new Rectangle(bounds.X + 18, bounds.Y + 42, 300, 12), Color.Lerp(new Color(39, 46, 53), new Color(72, 81, 90), highlight));
        }

        var spinnerCenter = new Vector2(GtpEngineSelectionDialogListBounds.Center.X, GtpEngineSelectionDialogListBounds.Center.Y);
        for (var index = 0; index < 8; index++)
        {
            var angle = phase * 4f + MathF.Tau * index / 8f;
            var opacity = 0.22f + 0.78f * index / 8f;
            DrawCircle(spinnerCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 28f, 5, new Color(147, 244, 200) * opacity);
        }
        DrawText("CHECKING ENGINES...", spinnerCenter + new Vector2(-130, 56), new Color(180, 195, 195), 0.38f);
    }

    private void DrawGtpEngineSelectionPropertiesSkeleton()
    {
        FillRect(GtpEngineSelectionDialogPropertyBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogPropertyBounds, 1, new Color(67, 84, 92));
        for (var row = 0; row < 5; row++)
        {
            var y = GtpEngineSelectionDialogPropertyBounds.Y + 20 + row * 68;
            FillRect(new Rectangle(GtpEngineSelectionDialogPropertyBounds.X + 22, y, 150, 14), new Color(48, 56, 64));
            FillRect(new Rectangle(GtpEngineSelectionDialogPropertyBounds.X + 190, y, GtpEngineSelectionDialogPropertyBounds.Width - 220, 14), new Color(39, 46, 53));
        }
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

        DrawText(session.IsGtpEngineAddPanelMode ? "ADD ENGINE (GTP)" : "EDIT ENGINE (GTP)", new Vector2(GtpEngineEditPanelBounds.X + 30, GtpEngineEditPanelBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(GtpEngineEditPanelCloseButtonBounds, "DISCARD", false, mousePoint, enabled: session.IsGtpEngineEditDirty, scale: 0.30f);
        DrawCommandButton(GtpEngineEditPanelSaveButtonBounds, session.IsGtpEngineEditDirty ? "SAVE & CLOSE" : "CLOSE", false, mousePoint,
            scale: session.IsGtpEngineEditDirty ? 0.27f : 0.34f);

        DrawGtpEngineEditField(session, GtpEngineProfileEditField.DisplayName, "DISPLAY", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.ExecutablePath, "EXE", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.WorkingDirectory, "WORKDIR", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.Arguments, "ARGS", mousePoint);

        DrawCommandButton(GtpEngineEditPanelGuiOptionsButtonBounds, "ENGINE OPTIONS", false, mousePoint, scale: 0.32f);

        var initialPositionBounds = GtpEngineEditPanelInitialPositionRowBounds;
        DrawDataRowFrame(initialPositionBounds);
        DrawUiLabel(UiLabel.InCompactRow("POSITION", initialPositionBounds));
        DrawCommandButton(
            GtpEngineEditPanelInitialPositionProfileButtonBounds,
            $"PROFILE {FormatInitialPositionProfile(session.GtpEngineEditDraft.InitialPositionProfileId)}",
            false,
            mousePoint,
            scale: 0.28f);
        DrawCommandButton(
            GtpEngineEditPanelInitialPositionMethodButtonBounds,
            $"METHOD {FormatInitialPositionMethod(session.GtpEngineEditDraft.InitialPositionManualPreferredMethod)}",
            false,
            mousePoint,
            scale: 0.27f);

        var logBounds = GtpEngineEditPanelLogRowBounds;
        DrawDataRowFrame(logBounds);
        DrawUiLabel(UiLabel.InCompactRow("GTP LOG", logBounds));
        DrawCommandButton(GtpEngineEditPanelLogButtonBounds, session.GtpEngineEditDraft.EnableGtpLog ? "ON" : "OFF", session.GtpEngineEditDraft.EnableGtpLog, mousePoint, scale: 0.42f);

        if (!string.IsNullOrWhiteSpace(session.GtpEngineEditWarning))
        {
            DrawFittedText(session.GtpEngineEditWarning, new Rectangle(GtpEngineEditPanelBounds.X + 90, GtpEngineEditPanelBounds.Bottom - 76, GtpEngineEditPanelBounds.Width - 180, 28), new Color(255, 183, 146), 0.32f);
        }

        DrawGtpEngineGuiOptionsDialog(session, mousePoint);
    }

    private static string FormatInitialPositionProfile(string? id) =>
        string.IsNullOrWhiteSpace(id) ? "AUTO" : id.Trim().ToUpperInvariant();

    private static string FormatInitialPositionMethod(InitialPositionMethod? method) => method switch
    {
        null => "AUTO",
        InitialPositionMethod.FixedHandicap => "FIXED",
        InitialPositionMethod.SetFreeHandicap => "FREE",
        InitialPositionMethod.LoadSgf => "LOAD SGF",
        InitialPositionMethod.KifuwarabeAtomicSetup => "ATOMIC",
        InitialPositionMethod.SequentialPlay => "PLAY",
        _ => method.ToString()!.ToUpperInvariant(),
    };


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

        DrawText(session.IsAppProviderGameSettingsDialogOpen ? "GAME SETTINGS" : "ENGINE OPTIONS", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 30, GtpEngineGuiOptionsDialogBounds.Y + 24), new Color(244, 238, 218), 0.72f);
        DrawText(session.IsAppProviderGameSettingsDialogOpen ? "PONNUKI / PROVIDER settings are sent when the game starts." : "Settings are sent when the engine starts.", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 32, GtpEngineGuiOptionsDialogBounds.Y + 82), new Color(180, 195, 195), 0.4f);
        DrawText("Text values (max 10000 characters)", new Vector2(GtpEngineGuiOptionsDialogBounds.X + 32, GtpEngineGuiOptionsDialogBounds.Y + 116), new Color(118, 139, 143), 0.3f);

        var startIndex = session.GtpEngineGuiOptionsPageIndex * GoAppSession.GtpEngineGuiOptionsPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineGuiOptionsPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= session.ActiveGtpEngineGuiOptionSpecs.Count) break;
            DrawGtpEngineGuiOptionRow(session, session.ActiveGtpEngineGuiOptionSpecs[index], slot, mousePoint);
        }

        DrawPager(
            session.GtpEngineGuiOptionsPageIndex,
            session.GetGtpEngineGuiOptionsPageCount(),
            GtpEngineGuiOptionsPreviousPageButtonBounds,
            GtpEngineGuiOptionsNextPageButtonBounds,
            GtpEngineGuiOptionsPageLabelBounds,
            mousePoint);

        DrawCommandButton(GtpEngineGuiOptionsDialogCancelButtonBounds, "DISCARD", false, mousePoint, enabled: session.IsGtpEngineGuiOptionsDialogDirty, scale: 0.30f);
        DrawCommandButton(GtpEngineGuiOptionsDialogOkButtonBounds, session.IsGtpEngineGuiOptionsDialogDirty ? "SAVE & CLOSE" : "CLOSE", false, mousePoint,
            scale: session.IsGtpEngineGuiOptionsDialogDirty ? 0.25f : 0.34f);
        DrawGtpEngineGuiOptionValueTooltip(session, mousePoint);
        DrawGtpEngineRandomMoveSelectionDialog(session, mousePoint);
    }

    private void DrawGtpEngineGuiOptionRow(GoAppSession session, GtpEngineGuiOptionSpec option, int slot, Point mousePoint)
    {
        var row = GtpEngineGuiOptionRowBounds(slot);
        var value = session.GetGtpEngineGuiOptionDraft(option);
        var valueBounds = GtpEngineGuiOptionValueBounds(slot);
        var hovered = valueBounds.Contains(mousePoint);
        DrawText(option.Label, new Vector2(row.X + 16, row.Y + 17), new Color(180, 195, 195), 0.36f);
        if (option.Type == "button")
        {
            var queued = bool.TryParse(value, out var isQueued) && isQueued;
            DrawCommandButton(valueBounds, queued ? "QUEUED" : "EXECUTE", queued, mousePoint, scale: queued ? 0.27f : 0.25f);
            return;
        }

        var rowValue = option.Type switch
        {
            "string" or "filename" => AbbreviateOptionValue(value, 28),
            _ => value,
        };
        DrawDynamicOptionText(string.IsNullOrEmpty(rowValue) ? "<empty>" : rowValue, valueBounds, Color.White, 0.34f);
        _gtpEngineOptionLinkUnderline.Bounds = valueBounds;
        _gtpEngineOptionLinkUnderline.SetActionBadge(ActionBadgeComponent.Create(GetGtpEngineOptionActionLabel(option), valueBounds, 0.30f));
        _gtpEngineOptionLinkUnderline.UpdatePointer(mousePoint);
        _gtpEngineOptionLinkUnderline.Draw(_stationeryDrawingContext);
        if (option.Type == "spin" && option.Min is { } min && option.Max is { } max)
            DrawFittedText($"{min} .. {max}", new Rectangle(valueBounds.Right + 12, valueBounds.Y + 10, 126, 28), new Color(118, 139, 143), 0.24f);
        if (option.Type is not ("button" or "string") && row.Contains(mousePoint))
            DrawCommandButton(GtpEngineGuiOptionDefaultButtonBounds(slot), "DEFAULT", false, mousePoint, scale: 0.3f);
    }

    private static string GetGtpEngineOptionActionLabel(GtpEngineGuiOptionSpec option) => option.Type switch
    {
        "check" => "TOGGLE",
        "spin" or "string" => "EDIT",
        "combo" => "SELECT",
        "filename" => "CHANGE",
        "button" => "EXECUTE",
        _ => "EDIT",
    };

    private void DrawGtpEngineGuiOptionValueTooltip(GoAppSession session, Point mousePoint)
    {
        var startIndex = session.GtpEngineGuiOptionsPageIndex * GoAppSession.GtpEngineGuiOptionsPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineGuiOptionsPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= session.ActiveGtpEngineGuiOptionSpecs.Count) break;
            var option = session.ActiveGtpEngineGuiOptionSpecs[index];
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

    internal void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            DrawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_dynamicOptionTextTextures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new MemoryStream(png, writable: false);
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
        DrawText(session.ActiveGtpEngineComboOption?.Label ?? "OPTION", new Vector2(GtpEngineRandomMoveSelectionDialogBounds.X + 48, GtpEngineRandomMoveSelectionDialogBounds.Y + 80), new Color(180, 195, 195), 0.4f);

        var choices = session.GetActiveGtpEngineComboChoices();
        var startIndex = session.GtpEngineRandomMoveSelectionPageIndex * GoAppSession.GtpEngineComboSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.GtpEngineComboSelectionPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= choices.Count) break;
            var choice = choices[index];
            var bounds = GtpEngineRandomMoveSelectionDialogItemBounds(slot);
            var selected = index == session.GtpEngineRandomMoveSelectionIndex;
            var hovered = choice.IsEnabled && bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : choice.IsEnabled ? new Color(15, 20, 26) : new Color(31, 31, 33));
            DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : choice.IsEnabled ? new Color(67, 84, 92) : new Color(75, 63, 65));
            DrawFittedText(choice.Value, new Rectangle(bounds.X + 24, bounds.Y + 7, bounds.Width - 48, 32), choice.IsEnabled ? Color.White : new Color(145, 145, 145), 0.46f);
            if (!choice.IsEnabled)
                DrawFittedText(choice.DisabledReason, new Rectangle(bounds.X + 24, bounds.Y + 42, bounds.Width - 48, 24), new Color(255, 145, 151), 0.25f);
        }

        DrawPager(
            session.GtpEngineRandomMoveSelectionPageIndex,
            session.GetGtpEngineRandomMoveSelectionPageCount(),
            GtpEngineRandomMoveSelectionPreviousPageButtonBounds,
            GtpEngineRandomMoveSelectionNextPageButtonBounds,
            GtpEngineRandomMoveSelectionPageLabelBounds,
            mousePoint);

        DrawCommandButton(GtpEngineRandomMoveSelectionDialogCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.38f);
        var canSelect = session.GtpEngineRandomMoveSelectionIndex >= 0 && session.GtpEngineRandomMoveSelectionIndex < choices.Count && choices[session.GtpEngineRandomMoveSelectionIndex].IsEnabled;
        DrawCommandButton(GtpEngineRandomMoveSelectionDialogSelectButtonBounds, "SELECT", false, mousePoint, enabled: canSelect, scale: 0.36f);
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
        var text = session.GetGtpEngineEditFieldText(field);

        var textBounds = GtpEngineEditPanelFieldTextBounds(field);
        var hovered = textBounds.Contains(mousePoint);
        DrawText(label, new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (GtpEngineEditableTextFields.Contains(field))
            DrawTabNavigationHint(
                bounds,
                Array.IndexOf(GtpEngineEditableTextFields, field),
                session.ActiveGtpEngineEditField is { } activeField ? Array.IndexOf(GtpEngineEditableTextFields, activeField) : -1,
                GtpEngineEditableTextFields.Length);
        if (active)
            DrawTextBoxSelection(text, session.GtpEngineEditSelectionStart, session.GtpEngineEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
        {
            DrawTextBoxCaret(text, session.GtpEngineEditCaretIndex, textBounds, 0.42f);
        }

        if (GtpEngineEditableTextFields.Contains(field))
            DrawEditableTextEditHint(active, hovered, textBounds);
        else if (hovered && !active)
        {
            ChangeActionBadge.SetAnchorBounds(textBounds);
            ChangeActionBadge.Show();
            ChangeActionBadge.Draw(_stationeryDrawingContext);
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
        var inspected = index == session.GtpEngineDialogSelectionIndex;
        var inUse = index == session.SelectedGtpEngineIndex;
        var compatibility = session.GetGtpEngineAppCompatibility(index);
        var enabled = compatibility.CanSelect;
        var rowSelectable = session.IsGtpEngineSelectionForAppProvider || enabled;
        var hovered = rowSelectable && bounds.Contains(mousePoint);
        FillRect(bounds, inUse ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : enabled ? new Color(24, 31, 37) : new Color(27, 28, 31));
        DrawRect(bounds, inspected ? 2 : 1, inspected ? new Color(125, 225, 255) : inUse ? new Color(147, 244, 200) : enabled ? new Color(70, 85, 94) : new Color(75, 63, 65));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 16), inUse ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        if (inspected)
            DrawSelectionFingerMark(new Vector2(bounds.X - 55, bounds.Center.Y - 13), 1.65f);
        _stationeryDrawingContext.DrawPlayerRoleFaceIcon(new Vector2(bounds.X + 72, bounds.Y + 22), isComputer: true);
        var nameWidth = inUse ? bounds.Width - 196 : bounds.Width - 106;
        DrawFittedText(profile.DisplayName, new Rectangle(bounds.X + 86, bounds.Y + 6, nameWidth, 30), enabled ? Color.White : new Color(145, 145, 145), 0.5f);
        if (inUse)
            DrawText("IN USE", new Vector2(bounds.Right - 82, bounds.Y + 12), new Color(177, 255, 215), 0.27f);
        DrawFittedText(compatibility.Message, new Rectangle(bounds.X + 86, bounds.Y + 39, bounds.Width - 106, 24), enabled ? new Color(99, 223, 185) : new Color(255, 145, 151), 0.29f);
    }

    private void DrawSelectionFingerMark(Vector2 origin, float scale)
    {
        var color = new Color(125, 225, 255);
        var thickness = 2f * scale;
        var points = new[]
        {
            origin + new Vector2(0, 2) * scale,
            origin + new Vector2(5, 2) * scale,
            origin + new Vector2(7, -3) * scale,
            origin + new Vector2(9, -3) * scale,
            origin + new Vector2(10, 0) * scale,
            origin + new Vector2(21, 0) * scale,
            origin + new Vector2(24, 3) * scale,
            origin + new Vector2(21, 6) * scale,
            origin + new Vector2(12, 6) * scale,
            origin + new Vector2(15, 9) * scale,
            origin + new Vector2(13, 12) * scale,
            origin + new Vector2(10, 10) * scale,
            origin + new Vector2(11, 14) * scale,
            origin + new Vector2(8, 16) * scale,
            origin + new Vector2(5, 12) * scale,
            origin + new Vector2(0, 10) * scale,
            origin + new Vector2(0, 2) * scale,
        };

        for (var i = 1; i < points.Length; i++)
            DrawLine(points[i - 1], points[i], thickness, color);
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
        DrawGtpEnginePropertyRow(y + 350, "APP", session.GetGtpEngineAppCompatibility(selectedIndex).Message);

        // パス用ポップアップは一度に一つだけ表示する。二つのポップアップが重なると、
        // 後から描いた方が別行のホバー判定に見えてしまう。
        if (PopupFilePathTooltip.IsHovered(HeadUpDisplay.StickyNoteScreen, StickyNoteKind.GtpEnginePathHint, executablePathRowBounds, executablePath, mousePoint))
            HeadUpDisplay.PopupFilePathTooltip.Draw(
                HeadUpDisplay.StickyNoteScreen,
                StickyNoteKind.GtpEnginePathHint,
                executablePathRowBounds,
                executablePath,
                mousePoint,
                "EXE とは？",
                [
                    "コンピュータ碁の実行ファイルです。",
                    "いわゆる思考エンジンです。",
                    "GTP プロトコルに対応している",
                    "必要があります。",
                ],
                _stationeryDrawingContext,
                DrawDynamicOptionText);
        else if (PopupFilePathTooltip.IsHovered(HeadUpDisplay.StickyNoteScreen, StickyNoteKind.GtpEnginePathHint, workingDirectoryRowBounds, displayWorkingDirectory, mousePoint))
            HeadUpDisplay.PopupFilePathTooltip.Draw(
                HeadUpDisplay.StickyNoteScreen,
                StickyNoteKind.GtpEnginePathHint,
                workingDirectoryRowBounds,
                displayWorkingDirectory,
                mousePoint,
                "WORKDIR とは？",
                [
                    "この GUI ではなく、思考エンジンの",
                    "実行ファイルから見たカレント",
                    "ディレクトリーです。詳しくは",
                    "「ワーキングディレクトリー」で",
                    "調べてください。",
                ],
                _stationeryDrawingContext,
                DrawDynamicOptionText);
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

    private static Rectangle GtpEngineSelectionDialogOrderButtonBounds => new(740, 874, 120, 44);


    private static Rectangle GtpEngineDeleteConfirmationBounds => new(654, 358, 612, 260);


    private static Rectangle GtpEngineDeleteConfirmationCancelButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 298, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);


    private static Rectangle GtpEngineDeleteConfirmationConfirmButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 448, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);


    private static Rectangle GtpEngineEditPanelBounds => new(430, 126, 1060, 820);


    private static Rectangle GtpEngineEditPanelCloseButtonBounds => new(1144, 156, 132, 48);


    private static Rectangle GtpEngineEditPanelSaveButtonBounds => new(1288, 156, 162, 48);


    private static Rectangle GtpEngineEditPanelFileBrowseButtonBounds => new(
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.ExecutablePath).X,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.ExecutablePath).Y,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.ExecutablePath).Width,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.ExecutablePath).Height);


    private static Rectangle GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds => new(
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.WorkingDirectory).X,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.WorkingDirectory).Y,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.WorkingDirectory).Width,
        GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField.WorkingDirectory).Height);


    private static Rectangle GtpEngineEditPanelGuiOptionsButtonBounds => new(AddPanelControlX, 590, 300, 56);


    private static Rectangle GtpEngineEditPanelInitialPositionRowBounds => new(AddPanelControlX, 654, 668, 56);


    private static Rectangle GtpEngineEditPanelInitialPositionProfileButtonBounds => new(AddPanelControlX + 152, 662, 220, 40);


    private static Rectangle GtpEngineEditPanelInitialPositionMethodButtonBounds => new(AddPanelControlX + 384, 662, 268, 40);


    private static Rectangle GtpEngineEditPanelLogRowBounds => new(AddPanelControlX, 718, 668, 56);


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


    private static readonly GtpEngineProfileEditField[] GtpEngineEditableTextFields =
    {
        GtpEngineProfileEditField.DisplayName,
        GtpEngineProfileEditField.Arguments,
    };


    private static Rectangle GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField field) => field switch
    {
        GtpEngineProfileEditField.DisplayName => new Rectangle(AddPanelControlX, 272, 668, 48),
        GtpEngineProfileEditField.DefaultCgosLoginName => new Rectangle(AddPanelControlX, 304, 668, 48),
        GtpEngineProfileEditField.DefaultCgosPlainTextPassword => new Rectangle(AddPanelControlX, 360, 668, 48),
        GtpEngineProfileEditField.ExecutablePath => new Rectangle(AddPanelControlX, 352, 668, 48),
        GtpEngineProfileEditField.WorkingDirectory => new Rectangle(AddPanelControlX, 432, 668, 48),
        GtpEngineProfileEditField.Arguments => new Rectangle(AddPanelControlX, 512, 668, 48),
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


}
