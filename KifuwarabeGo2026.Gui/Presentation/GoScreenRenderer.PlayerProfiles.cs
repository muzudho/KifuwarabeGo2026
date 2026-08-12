namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Shared.PlayerSelector;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>Player 選択欄と選択ダイアログの描画・当たり判定。</summary>
public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle PlayerSelectionDialogBounds = new(398, 150, 1124, 760);
    private static readonly Rectangle PlayerSelectionListBounds = new(438, 270, 1044, 510);
    private static readonly Rectangle PlayerSelectionCancelButtonBounds = new(1116, 180, 156, 50);
    private static readonly Rectangle PlayerSelectionOkButtonBounds = new(1302, 180, 180, 50);
    private static readonly Rectangle PlayerSelectionPreviousButtonBounds = new(1212, 816, 104, 48);
    private static readonly Rectangle PlayerSelectionNextButtonBounds = new(1328, 816, 116, 48);
    private static readonly Rectangle PlayerSelectionAddHumanButtonBounds = new(438, 816, 116, 48);
    private static readonly Rectangle PlayerSelectionAddComputerButtonBounds = new(566, 816, 162, 48);
    private static readonly Rectangle PlayerSelectionEditButtonBounds = new(742, 816, 144, 48);
    private static readonly Rectangle PlayerSelectionDeleteButtonBounds = new(900, 816, 144, 48);
    private static readonly Rectangle PlayerSelectionOrderButtonBounds = new(1056, 816, 72, 48);
    private static readonly Rectangle PlayerEditPanelCancelButtonBounds = new(1010, 670, 170, 52);
    private static readonly Rectangle PlayerEditPanelSaveButtonBounds = new(1190, 670, 170, 52);
    private static readonly Rectangle PlayerEditPanelPreviousEngineButtonBounds = new(760, 560, 62, 46);
    private static readonly Rectangle PlayerEditPanelNextEngineButtonBounds = new(834, 560, 62, 46);
    private static readonly Rectangle PlayerEditPanelEngineOptionsButtonBounds = new(908, 560, 220, 46);
    private static readonly Rectangle PlayerEditPanelTargetsButtonBounds = new(1140, 560, 220, 46);
    private static readonly Rectangle PlayerEditPanelSelectTargetButtonBounds = new(1140, 439, 220, 46);
    private static readonly Rectangle TargetProfileEditCloseButtonBounds = new(1320, 182, 150, 48);
    private static readonly Rectangle TargetProfileEditUseButtonBounds = new(1158, 182, 150, 48);
    private static readonly Rectangle TargetProfileEditAddCgosButtonBounds = new(466, 820, 150, 48);
    private static readonly Rectangle TargetProfileEditAddLocalButtonBounds = new(628, 820, 160, 48);
    private static readonly Rectangle TargetProfileEditRemoveButtonBounds = new(800, 820, 140, 48);
    private static readonly Rectangle TargetProfileEditSelectConnectionButtonBounds = new(1110, 820, 260, 48);
    private static readonly Rectangle TargetProfileConnectionSelectionPanelBounds = new(510, 210, 900, 660);
    private static readonly Rectangle TargetProfileConnectionSelectionCancelButtonBounds = new(1050, 236, 140, 48);
    private static readonly Rectangle TargetProfileConnectionSelectionSelectButtonBounds = new(1202, 236, 170, 48);
    private static readonly Rectangle TargetProfileConnectionSelectionPreviousButtonBounds = new(1060, 798, 120, 44);
    private static readonly Rectangle TargetProfileConnectionSelectionNextButtonBounds = new(1192, 798, 120, 44);
    private static readonly Rectangle QuickTargetSelectionPanelBounds = new(560, 245, 800, 560);
    private static readonly Rectangle QuickTargetSelectionCancelButtonBounds = new(1030, 272, 140, 48);
    private static readonly Rectangle QuickTargetSelectionSelectButtonBounds = new(1182, 272, 140, 48);

    public static bool GetBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(BlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(WhitePlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiBlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiWhitePlayerKindButtonY).ContainsBrowseButton(point);
    public static GoStone? GetLocalMatchHandleHit(Point point)
    {
        if (LocalMatchHandleBounds(BlackPlayerKindButtonY).Contains(point)) return GoStone.Black;
        return LocalMatchHandleBounds(WhitePlayerKindButtonY).Contains(point) ? GoStone.White : null;
    }
    public static bool GetPlayerSelectionDialogCancelButtonHit(Point point) => PlayerSelectionCancelButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogOkButtonHit(Point point) => PlayerSelectionOkButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogPreviousPageButtonHit(Point point) => PlayerSelectionPreviousButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogNextPageButtonHit(Point point) => PlayerSelectionNextButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogAddHumanButtonHit(Point point) => PlayerSelectionAddHumanButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogAddComputerButtonHit(Point point) => PlayerSelectionAddComputerButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogDeleteButtonHit(Point point) => PlayerSelectionDeleteButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogEditButtonHit(Point point) => PlayerSelectionEditButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogOrderButtonHit(Point point) => PlayerSelectionOrderButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelCancelButtonHit(Point point) => PlayerEditPanelCancelButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelSaveButtonHit(Point point) => PlayerEditPanelSaveButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelPreviousEngineButtonHit(Point point) => PlayerEditPanelPreviousEngineButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelNextEngineButtonHit(Point point) => PlayerEditPanelNextEngineButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelEngineOptionsButtonHit(Point point) => PlayerEditPanelEngineOptionsButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelTargetsButtonHit(Point point) => PlayerEditPanelTargetsButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelSelectTargetButtonHit(Point point) => PlayerEditPanelSelectTargetButtonBounds.Contains(point);
    public static bool GetTargetProfileEditCloseButtonHit(Point point) => TargetProfileEditCloseButtonBounds.Contains(point);
    public static bool GetTargetProfileEditAddCgosButtonHit(Point point) => TargetProfileEditAddCgosButtonBounds.Contains(point);
    public static bool GetTargetProfileEditAddLocalButtonHit(Point point) => TargetProfileEditAddLocalButtonBounds.Contains(point);
    public static bool GetTargetProfileEditRemoveButtonHit(Point point) => TargetProfileEditRemoveButtonBounds.Contains(point);
    public static bool GetTargetProfileEditUseButtonHit(Point point) => TargetProfileEditUseButtonBounds.Contains(point);
    public static bool GetTargetProfileEditSelectConnectionButtonHit(Point point) => TargetProfileEditSelectConnectionButtonBounds.Contains(point);
    public static bool GetTargetProfileConnectionSelectionCancelButtonHit(Point point) => TargetProfileConnectionSelectionCancelButtonBounds.Contains(point);
    public static bool GetTargetProfileConnectionSelectionSelectButtonHit(Point point) => TargetProfileConnectionSelectionSelectButtonBounds.Contains(point);
    public static bool GetTargetProfileConnectionSelectionPreviousButtonHit(Point point) => TargetProfileConnectionSelectionPreviousButtonBounds.Contains(point);
    public static bool GetTargetProfileConnectionSelectionNextButtonHit(Point point) => TargetProfileConnectionSelectionNextButtonBounds.Contains(point);
    public static int? GetTargetProfileConnectionSelectionItemHit(Point point, GoAppSession session)
    {
        var start = session.TargetProfileConnectionSelectionPageIndex * GoAppSession.TargetProfileConnectionSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.TargetProfileConnectionSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.CgosConnectionProfiles.Count) break;
            if (TargetProfileConnectionSelectionItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }
    public static bool GetQuickTargetSelectionCancelButtonHit(Point point) => QuickTargetSelectionCancelButtonBounds.Contains(point);
    public static bool GetQuickTargetSelectionSelectButtonHit(Point point) => QuickTargetSelectionSelectButtonBounds.Contains(point);
    public static int? GetQuickTargetSelectionItemHit(Point point, GoAppSession session)
    {
        var targets = session.GetQuickTargetSelectionTargets(session.QuickTargetSelectionStone, session.QuickTargetSelectionIsCgos);
        for (var index = 0; index < targets.Count; index++)
            if (QuickTargetSelectionItemBounds(index).Contains(point)) return index;
        return null;
    }
    public static TargetProfileEditField? GetTargetProfileEditFieldHit(Point point, GoAppSession session)
    {
        var index = session.TargetProfileEditIndex;
        var isLocalMatch = string.IsNullOrEmpty(session.TargetProfileEditDraft.ConnectionProfileId);
        return TargetProfileEditFieldTextBounds(index, TargetProfileEditField.DisplayName, isLocalMatch).Contains(point) ? TargetProfileEditField.DisplayName :
            TargetProfileEditFieldTextBounds(index, TargetProfileEditField.LoginName, isLocalMatch).Contains(point) ? TargetProfileEditField.LoginName :
            !isLocalMatch && TargetProfileEditFieldTextBounds(index, TargetProfileEditField.LoginPass, false).Contains(point) ? TargetProfileEditField.LoginPass : null;
    }
    public int GetTargetProfileEditCaretIndex(Point point, int index, TargetProfileEditField field, string text, bool isLocalMatch) =>
        GetTextBoxCaretIndex(point.X, text, TargetProfileEditFieldTextBounds(index, field, isLocalMatch), 0.34f);
    public static int? GetTargetProfileEditItemHit(Point point, GoAppSession session)
    {
        var count = session.GetPlayerTargetProfiles(session.PlayerEditDraft.Id).Count;
        for (var index = 0; index < count; index++)
            if (new Rectangle(466, 290 + index * 92, 1008, 78).Contains(point)) return index;
        return null;
    }

    public static PlayerProfileEditField? GetPlayerEditPanelFieldHit(Point point) =>
        PlayerEditPanelFieldTextBounds(PlayerProfileEditField.DisplayName).Contains(point) ? PlayerProfileEditField.DisplayName :
        null;

    public int GetPlayerEditPanelCaretIndex(Point point, PlayerProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, PlayerEditPanelFieldTextBounds(field), 0.42f);

    public static int? GetPlayerSelectionDialogItemHit(Point point, GoAppSession session)
    {
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.PlayerProfiles.Count) break;
            if (PlayerSelectionItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }

    private void DrawSetupPlayerRow(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var player = session.GetSelectedPlayerProfile(stone);
        var label = stone == GoStone.Black ? "BLACK PLAYER" : "WHITE PLAYER";
        DrawPlayerSelector(PlayerSelectorLayout.CreatePlayerSelector(y) with { Label = label, Value = player?.DisplayName ?? "SELECT PLAYER" }, mousePoint);
        var handleBounds = LocalMatchHandleBounds(y);
        DrawDataRowFrame(handleBounds, hovered: handleBounds.Contains(mousePoint));
        DrawUiLabel(UiLabel.InCompactRow("HANDLE", handleBounds));
        DrawFittedText(session.GetLocalMatchPresentedName(stone), new Rectangle(handleBounds.X + 152, handleBounds.Y + 7, handleBounds.Width - 168, 30), Color.White, 0.32f);
    }

    private void DrawPlayerSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerSelectionDialogOpen) return;
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 125));
        FillRect(new Rectangle(PlayerSelectionDialogBounds.X + 16, PlayerSelectionDialogBounds.Y + 18, PlayerSelectionDialogBounds.Width, PlayerSelectionDialogBounds.Height), new Color(0, 0, 0, 150));
        FillRect(PlayerSelectionDialogBounds, new Color(19, 24, 31, 250));
        DrawRect(PlayerSelectionDialogBounds, 2, new Color(116, 145, 146));

        var target = session.PlayerSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        var cgos = session.PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos;
        DrawText($"{(cgos ? "ONLINE MATCH PLAYER" : "PLAYER")} SELECT  {target}", new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
        DrawText(cgos ? "Choose a computer player with an OnlineMatch (CGOS) Target for this connection." : "Human and computer players are selected from one list.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
        DrawCommandButton(PlayerSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOkButtonBounds, "SELECT", false, mousePoint, enabled: session.CanCommitPlayerSelection, scale: 0.34f);

        FillRect(PlayerSelectionListBounds, new Color(15, 20, 26));
        DrawRect(PlayerSelectionListBounds, 1, new Color(67, 84, 92));
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.PlayerProfiles.Count) break;
            var bounds = PlayerSelectionItemBounds(slot);
            var selected = index == session.PlayerDialogSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(38, 91, 78) : hovered ? new Color(42, 53, 61) : new Color(27, 35, 42));
            DrawRect(bounds, selected ? 2 : 1, selected ? new Color(190, 255, 229) : new Color(73, 91, 98));
            DrawFittedText(session.PlayerProfiles[index].DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 12, bounds.Width - 40, 30), Color.White, 0.48f);
            DrawFittedText(session.GetPlayerSelectionDetail(index), new Rectangle(bounds.X + 20, bounds.Y + 52, bounds.Width - 40, 24), new Color(180, 195, 195), 0.30f);
        }

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.PlayerProfiles.Count / (double)GoAppSession.PlayerSelectionPageSize));
        var addHeaderBounds = new Rectangle(438, 782, 290, 26);
        FillRect(addHeaderBounds, new Color(56, 54, 84));
        DrawRect(addHeaderBounds, 1, new Color(133, 128, 177));
        var addLabelSize = _font.MeasureString("ADD") * 0.34f;
        DrawText("ADD", new Vector2(addHeaderBounds.Center.X - addLabelSize.X / 2f, addHeaderBounds.Center.Y - addLabelSize.Y / 2f), Color.White, 0.34f);
        DrawCommandButton(PlayerSelectionAddHumanButtonBounds, "HUMAN", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionAddComputerButtonBounds, "COMPUTER", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionEditButtonBounds, "EDIT", false, mousePoint, enabled: session.PlayerDialogSelectionIndex >= 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedPlayerProfile, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOrderButtonBounds, "ORDER", false, mousePoint, enabled: session.PlayerProfiles.Count > 1, scale: 0.26f);
        DrawCommandButton(PlayerSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.PlayerSelectionPageIndex > 0, scale: 0.34f);
        DrawFittedText($"{session.PlayerSelectionPageIndex + 1} / {pageCount}", new Rectangle(1140, 824, 64, 32), new Color(227, 224, 210), 0.44f);
        DrawCommandButton(PlayerSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.PlayerSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCatalogOrderEditor(session.PlayerOrderEditor, "PLAYERS", mousePoint, player => player.DisplayName, player => player.Kind == PlayerProfileKind.Human ? "HUMAN" : "COMPUTER");
    }

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerEditPanelOpen) return;
        var bounds = new Rectangle(510, 270, 900, 480);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 140));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT PLAYER", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawPlayerEditField(session, PlayerProfileEditField.DisplayName, "DISPLAY NAME", mousePoint);
        DrawText("TARGET", new Vector2(552, 446), new Color(180, 195, 195), 0.36f);
        DrawFittedText(session.PlayerEditTargetDisplayName, new Rectangle(760, 439, 360, 42), Color.White, 0.42f);
        DrawCommandButton(PlayerEditPanelSelectTargetButtonBounds, "SELECT TARGET", false, mousePoint, scale: 0.27f);
        if (session.PlayerEditDraft.Kind == PlayerProfileKind.Computer)
        {
            DrawText("ENGINE", new Vector2(552, 510), new Color(180, 195, 195), 0.36f);
            var engineTextBounds = new Rectangle(760, 503, 600, 42);
            DrawFittedText(session.PlayerEditEngineDisplayName, engineTextBounds, Color.White, 0.42f);
            DrawCommandButton(PlayerEditPanelEngineOptionsButtonBounds, "CHANGE ENGINE", false, mousePoint, scale: 0.28f);
        }
        DrawCommandButton(PlayerEditPanelTargetsButtonBounds, "EDIT TARGETS", false, mousePoint, scale: 0.28f);
        DrawText("Click a field to edit.  Enter: finish  Escape: cancel  Tab: next field", new Vector2(bounds.X + 42, bounds.Y + 360), new Color(180, 195, 195), 0.28f);
        DrawCommandButton(PlayerEditPanelCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerEditPanelSaveButtonBounds, "SAVE", false, mousePoint, scale: 0.40f);
    }

    private void DrawTargetProfileEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTargetProfileEditPanelOpen) return;
        var bounds = new Rectangle(430, 150, 1080, 760);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT TARGETS", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText("この Player 専用の接続先です（最大 5 件）。", new Rectangle(bounds.X + 36, bounds.Y + 82, 700, 32), new Color(180, 195, 195), 0.34f);
        var targets = session.GetPlayerTargetProfiles(session.PlayerEditDraft.Id);
        DrawCommandButton(TargetProfileEditUseButtonBounds, "USE", false, mousePoint, enabled: targets.Count > 0 && !session.IsTargetProfileInUse(session.TargetProfileEditIndex), scale: 0.34f);
        DrawCommandButton(TargetProfileEditCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.34f);
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var isSelectedTarget = index == session.TargetProfileEditIndex;
            DrawDataRowFrame(row, active: isSelectedTarget, hovered: row.Contains(mousePoint));
            if (isSelectedTarget)
            {
                DrawText("▶", new Vector2(row.X + 4, row.Y + 25), new Color(147, 244, 200), 0.34f);
                DrawRect(row, 2, new Color(147, 244, 200));
            }
            if (isSelectedTarget)
            {
                var isLocalMatch = string.IsNullOrEmpty(target.ConnectionProfileId);
                DrawTargetProfileEditField(session, index, TargetProfileEditField.DisplayName, "DISPLAY", mousePoint, isLocalMatch);
                DrawTargetProfileEditField(session, index, TargetProfileEditField.LoginName, "HANDLE", mousePoint, isLocalMatch);
                if (!isLocalMatch)
                    DrawTargetProfileEditField(session, index, TargetProfileEditField.LoginPass, "LOGIN PASS", mousePoint, false);
                DrawFittedText($"CONNECTION: {session.TargetProfileEditConnectionDisplayName}", new Rectangle(row.X + 18, row.Y + 47, 920, 22), new Color(147, 244, 200), 0.28f);
                if (session.IsTargetProfileInUse(index))
                    DrawFittedText("IN USE", new Rectangle(row.Right - 130, row.Y + 45, 110, 22), new Color(147, 244, 200), 0.28f);
            }
            else
            {
                DrawFittedText(target.DisplayName, new Rectangle(row.X + 18, row.Y + 10, 240, 28), Color.White, 0.46f);
                DrawFittedText($"HANDLE: {target.LoginName}", new Rectangle(row.X + 280, row.Y + 10, 390, 28), new Color(180, 195, 195), 0.34f);
                DrawFittedText(session.GetTargetProfileConnectionDisplayName(target), new Rectangle(row.X + 280, row.Y + 42, 520, 24), new Color(147, 244, 200), 0.30f);
            }
        }
        DrawCommandButton(TargetProfileEditAddCgosButtonBounds, "ADD ONLINE MATCH", false, mousePoint, enabled: targets.Count < 5, scale: 0.23f);
        DrawCommandButton(TargetProfileEditAddLocalButtonBounds, "ADD LOCAL", false, mousePoint, enabled: targets.Count < 5, scale: 0.32f);
        DrawCommandButton(TargetProfileEditRemoveButtonBounds, "REMOVE", false, mousePoint, enabled: targets.Count > 1, scale: 0.32f);
        var selected = session.TargetProfileEditDraft;
        DrawFittedText($"CONNECTION: {session.TargetProfileEditConnectionDisplayName}", new Rectangle(960, 766, 470, 32), new Color(147, 244, 200), 0.34f);
        var canSelectConnection = !string.IsNullOrEmpty(selected.ConnectionProfileId) && session.CgosConnectionProfiles.Count > 0;
        DrawCommandButton(TargetProfileEditSelectConnectionButtonBounds, "SELECT ONLINE MATCH SERVER", false, mousePoint, enabled: canSelectConnection, scale: 0.20f);
        DrawTargetProfileConnectionSelectionPanel(session, mousePoint);
    }

    private void DrawTargetProfileConnectionSelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTargetProfileConnectionSelectionPanelOpen) return;

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(TargetProfileConnectionSelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(TargetProfileConnectionSelectionPanelBounds, 2, new Color(116, 145, 146));
        DrawText("SELECT ONLINE MATCH SERVER", new Vector2(542, 240), new Color(244, 238, 218), 0.50f);
        DrawText("Choose the OnlineMatch (CGOS) server for this Target.", new Vector2(544, 294), new Color(180, 195, 195), 0.28f);
        DrawCommandButton(TargetProfileConnectionSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(TargetProfileConnectionSelectionSelectButtonBounds, "SELECT", false, mousePoint, scale: 0.34f);

        for (var slot = 0; slot < GoAppSession.TargetProfileConnectionSelectionPageSize; slot++)
        {
            var index = session.TargetProfileConnectionSelectionPageIndex * GoAppSession.TargetProfileConnectionSelectionPageSize + slot;
            if (index >= session.CgosConnectionProfiles.Count) break;
            var connection = session.CgosConnectionProfiles[index];
            var bounds = TargetProfileConnectionSelectionItemBounds(slot);
            var selected = index == session.TargetProfileConnectionSelectionIndex;
            DrawDataRowFrame(bounds, active: selected, hovered: bounds.Contains(mousePoint));
            DrawFittedText(connection.DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 10, bounds.Width - 40, 28), Color.White, 0.40f);
            DrawFittedText($"{connection.Host}:{connection.Port}  /  {connection.Event}  /  {connection.Round}".Trim(), new Rectangle(bounds.X + 20, bounds.Y + 43, bounds.Width - 40, 22), new Color(180, 195, 195), 0.26f);
        }

        DrawFittedText($"PAGE {session.TargetProfileConnectionSelectionPageIndex + 1} / {session.TargetProfileConnectionSelectionPageCount}", new Rectangle(872, 805, 168, 30), new Color(227, 224, 210), 0.34f);
        DrawCommandButton(TargetProfileConnectionSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.TargetProfileConnectionSelectionPageIndex > 0, scale: 0.32f);
        DrawCommandButton(TargetProfileConnectionSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.TargetProfileConnectionSelectionPageIndex < session.TargetProfileConnectionSelectionPageCount - 1, scale: 0.32f);
    }

    private void DrawQuickTargetSelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsQuickTargetSelectionPanelOpen) return;
        var targets = session.GetQuickTargetSelectionTargets(session.QuickTargetSelectionStone, session.QuickTargetSelectionIsCgos);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 125));
        FillRect(QuickTargetSelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(QuickTargetSelectionPanelBounds, 2, new Color(116, 145, 146));
        var service = session.QuickTargetSelectionIsCgos ? "ONLINE MATCH (CGOS)" : "LOCAL MATCH";
        DrawText("SELECT TEMPORARY HANDLE", new Vector2(590, 277), new Color(244, 238, 218), 0.56f);
        DrawStickyNote(
            new Rectangle(560, 824, 800, 130),
            new Vector2(960, 805),
            new Vector2(960, 824),
            new Color(147, 244, 200),
            new Color(116, 145, 146),
            "HANDLE とは？",
            ["機械に入力できる書式に従った、Player の Entry 名です。", $"この一覧は {service} 用です。選択は今回だけに適用されます。"],
            bodyLineSpacing: 30);
        DrawCommandButton(QuickTargetSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(QuickTargetSelectionSelectButtonBounds, "SELECT", false, mousePoint, scale: 0.30f);
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var bounds = QuickTargetSelectionItemBounds(index);
            DrawDataRowFrame(bounds, active: index == session.QuickTargetSelectionIndex, hovered: bounds.Contains(mousePoint));
            DrawFittedText(target.LoginName, new Rectangle(bounds.X + 18, bounds.Y + 9, 420, 28), Color.White, 0.42f);
            DrawFittedText(target.DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 43, 420, 20), new Color(180, 195, 195), 0.27f);
        }
    }

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);

    private static Rectangle LocalMatchHandleBounds(int y) => new(1144, y + 48, 668, 40);

    private static Rectangle TargetProfileConnectionSelectionItemBounds(int slot) => new(544, 332 + slot * 82, 832, 70);

    private static Rectangle QuickTargetSelectionItemBounds(int index) => new(592, 392 + index * 72, 736, 62);

    private static Rectangle TargetProfileEditFieldTextBounds(int index, TargetProfileEditField field, bool isLocalMatch)
    {
        var rowY = 290 + index * 92;
        return field switch
        {
            TargetProfileEditField.DisplayName => new Rectangle(560, rowY + 7, 190, 30),
            TargetProfileEditField.LoginName => new Rectangle(920, rowY + 7, isLocalMatch ? 520 : 230, 30),
            TargetProfileEditField.LoginPass => new Rectangle(1290, rowY + 7, 150, 30),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown target edit field."),
        };
    }

    private void DrawTargetProfileEditField(GoAppSession session, int index, TargetProfileEditField field, string label, Point mousePoint, bool isLocalMatch)
    {
        var textBounds = TargetProfileEditFieldTextBounds(index, field, isLocalMatch);
        var active = session.ActiveTargetProfileEditField == field;
        DrawText(label, new Vector2(textBounds.X - (field == TargetProfileEditField.LoginPass ? 116 : 100), textBounds.Y + 4), new Color(180, 195, 195), 0.28f);
        DrawTournamentRulesTextInputSurface(textBounds, active, textBounds.Contains(mousePoint));
        var text = session.GetTargetProfileEditField(field);
        var displayText = field == TargetProfileEditField.LoginPass && !active ? new string('*', text.Length) : text;
        if (active)
            DrawTextBoxSelection(text, session.TargetProfileEditSelectionStart, session.TargetProfileEditSelectionLength, textBounds, 0.34f);
        DrawFittedText(string.IsNullOrEmpty(displayText) ? "-" : displayText, textBounds, Color.White, 0.34f);
        if (active)
            DrawTextBoxCaret(text, session.TargetProfileEditCaretIndex, textBounds, 0.34f);
    }

    private static Rectangle PlayerEditPanelFieldTextBounds(PlayerProfileEditField field) => field switch
    {
        PlayerProfileEditField.DisplayName => new(760, 375, 600, 42),
        PlayerProfileEditField.Identifier => new(760, 439, 600, 42),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    private void DrawPlayerEditField(GoAppSession session, PlayerProfileEditField field, string label, Point mousePoint)
    {
        var textBounds = PlayerEditPanelFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        DrawText(label, new Vector2(552, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawTournamentRulesTextInputSurface(textBounds, active, textBounds.Contains(mousePoint));
        var text = session.GetPlayerEditFieldText(field);
        if (active)
            DrawTextBoxSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
            DrawTextBoxCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);
    }

    private void DrawPlayerEngineCycleButton(Rectangle bounds, bool pointsRight, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, hovered ? new Color(53, 66, 75) : new Color(31, 40, 47));
        DrawRect(bounds, 2, new Color(105, 127, 134));

        var centerX = bounds.Center.X;
        var centerY = bounds.Center.Y;
        var size = 14;
        for (var offset = -size; offset <= size; offset++)
        {
            var distanceFromTip = pointsRight ? size - offset : size + offset;
            var halfHeight = distanceFromTip * size / (size * 2);
            var x = centerX + offset;
            FillRect(
                new Rectangle(x, centerY - halfHeight, 1, halfHeight * 2 + 1),
                new Color(220, 234, 230));
        }
    }
}
