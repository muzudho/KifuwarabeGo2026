namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Shared.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.Shared.PlayerSelector;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>Player 選択欄と選択ダイアログの描画・当たり判定。</summary>
public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle PlayerSelectionDialogBounds = new(210, 120, 1500, 840);
    private static readonly Rectangle PlayerSelectionListBounds = new(250, 270, 660, 510);
    private static readonly Rectangle PlayerSelectionClientIdentityListBounds = new(970, 270, 700, 510);
    private static readonly Rectangle PlayerSelectionCancelButtonBounds = new(1116, 180, 156, 50);
    private static readonly Rectangle PlayerSelectionOkButtonBounds = new(1302, 180, 180, 50);
    private static readonly Rectangle PlayerSelectionPreviousButtonBounds = new(1212, 816, 104, 48);
    private static readonly Rectangle PlayerSelectionNextButtonBounds = new(1328, 816, 116, 48);
    private static readonly Rectangle PlayerSelectionAddHumanButtonBounds = new(438, 816, 116, 48);
    private static readonly Rectangle PlayerSelectionAddComputerButtonBounds = new(566, 816, 162, 48);
    private static readonly Rectangle PlayerSelectionEditButtonBounds = new(742, 816, 144, 48);
    private static readonly Rectangle PlayerSelectionDeleteButtonBounds = new(900, 816, 144, 48);
    private static readonly Rectangle PlayerSelectionOrderButtonBounds = new(1056, 816, 72, 48);
    private static readonly Rectangle PlayerEditPanelCancelButtonBounds = new(1080, 288, 132, 42);
    private static readonly Rectangle PlayerEditPanelSaveButtonBounds = new(1224, 288, 148, 42);
    private static readonly Rectangle ClientIdentityProfileSelectionCloseButtonBounds = new(1320, 182, 150, 48);
    private static readonly Rectangle ClientIdentityProfileSelectionUseButtonBounds = new(1158, 182, 150, 48);
    private static readonly Rectangle ClientIdentityProfileSelectionEditButtonBounds = new(800, 820, 150, 48);
    private static readonly Rectangle ClientIdentityProfileEditCancelButtonBounds = new(1158, 182, 150, 48);
    private static readonly Rectangle ClientIdentityProfileEditSaveButtonBounds = new(1320, 182, 150, 48);
    private static readonly Rectangle ClientIdentityProfileEditUseButtonBounds = new(1158, 182, 150, 48);
    private static readonly Rectangle ClientIdentityProfileEditAddCgosButtonBounds = new(628, 820, 160, 48);
    private static readonly Rectangle ClientIdentityProfileEditAddLocalButtonBounds = new(466, 820, 150, 48);
    private static readonly Rectangle ClientIdentityProfileEditRemoveButtonBounds = new(962, 820, 150, 48);
    private static readonly Rectangle ClientIdentityProfileConnectionSelectionPanelBounds = new(510, 210, 900, 660);
    private static readonly Rectangle ClientIdentityProfileConnectionSelectionCancelButtonBounds = new(1050, 236, 140, 48);
    private static readonly Rectangle ClientIdentityProfileConnectionSelectionSelectButtonBounds = new(1202, 236, 170, 48);
    private static readonly Rectangle ClientIdentityProfileConnectionSelectionPreviousButtonBounds = new(1060, 798, 120, 44);
    private static readonly Rectangle ClientIdentityProfileConnectionSelectionNextButtonBounds = new(1192, 798, 120, 44);
    private static readonly Rectangle QuickClientIdentitySelectionPanelBounds = new(560, 245, 800, 560);
    private static readonly Rectangle QuickClientIdentitySelectionCancelButtonBounds = new(1030, 272, 140, 48);
    private static readonly Rectangle QuickClientIdentitySelectionSelectButtonBounds = new(1182, 272, 140, 48);

    public static bool GetBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(BlackPlayerKindButtonY).Bounds.Contains(point);
    public static bool GetWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(WhitePlayerKindButtonY).Bounds.Contains(point);
    public static bool GetPonnukiBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiBlackPlayerKindButtonY).Bounds.Contains(point);
    public static bool GetPonnukiWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiWhitePlayerKindButtonY).Bounds.Contains(point);
    public static GoStone? GetLocalMatchHandleHit(Point point)
    {
        if (LocalMatchHandleBounds(BlackPlayerKindButtonY).Contains(point)) return GoStone.Black;
        return LocalMatchHandleBounds(WhitePlayerKindButtonY).Contains(point) ? GoStone.White : null;
    }
    public int GetLocalMatchHandleCaretIndex(Point point, GoStone stone, string text) =>
        GetTextBoxCaretIndex(point.X, text, LocalMatchHandleTextBounds(stone), 0.32f);
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
    public static bool GetPlayerEditPanelClientIdentityChangeHit(Point point) => PlayerEditPanelClientIdentityTextBounds.Contains(point);
    public static bool GetPlayerEditPanelEngineChangeHit(Point point) => PlayerEditPanelEngineTextBounds.Contains(point);
    public static bool GetClientIdentityProfileSelectionCloseButtonHit(Point point) => ClientIdentityProfileSelectionCloseButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileSelectionUseButtonHit(Point point) => ClientIdentityProfileSelectionUseButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileSelectionEditButtonHit(Point point) => ClientIdentityProfileSelectionEditButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditCancelButtonHit(Point point) => ClientIdentityProfileEditCancelButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditSaveButtonHit(Point point) => ClientIdentityProfileEditSaveButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditAddCgosButtonHit(Point point) => ClientIdentityProfileEditAddCgosButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditAddLocalButtonHit(Point point) => ClientIdentityProfileEditAddLocalButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditRemoveButtonHit(Point point) => ClientIdentityProfileEditRemoveButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileEditUseButtonHit(Point point) => ClientIdentityProfileEditUseButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileConnectionSelectionCancelButtonHit(Point point) => ClientIdentityProfileConnectionSelectionCancelButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileConnectionSelectionSelectButtonHit(Point point) => ClientIdentityProfileConnectionSelectionSelectButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileConnectionSelectionPreviousButtonHit(Point point) => ClientIdentityProfileConnectionSelectionPreviousButtonBounds.Contains(point);
    public static bool GetClientIdentityProfileConnectionSelectionNextButtonHit(Point point) => ClientIdentityProfileConnectionSelectionNextButtonBounds.Contains(point);
    public static int? GetClientIdentityProfileConnectionSelectionItemHit(Point point, GoAppSession session)
    {
        var start = session.ClientIdentityProfileConnectionSelectionPageIndex * GoAppSession.ClientIdentityProfileConnectionSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.ClientIdentityProfileConnectionSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.CgosConnectionProfiles.Count) break;
            if (ClientIdentityProfileConnectionSelectionItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }
    public static bool GetQuickClientIdentitySelectionCancelButtonHit(Point point) => QuickClientIdentitySelectionCancelButtonBounds.Contains(point);
    public static bool GetQuickClientIdentitySelectionSelectButtonHit(Point point) => QuickClientIdentitySelectionSelectButtonBounds.Contains(point);
    public static int? GetQuickClientIdentitySelectionItemHit(Point point, GoAppSession session)
    {
        var targets = session.GetQuickClientIdentitySelectionTargets(session.QuickClientIdentitySelectionStone, session.QuickClientIdentitySelectionIsCgos);
        for (var index = 0; index < targets.Count; index++)
            if (QuickClientIdentitySelectionItemBounds(index).Contains(point)) return index;
        return null;
    }
    public static ClientIdentityProfileEditField? GetClientIdentityProfileEditFieldHit(Point point, GoAppSession session)
    {
        var index = session.ClientIdentityProfileEditIndex;
        var isLocalMatch = string.IsNullOrEmpty(session.ClientIdentityProfileEditDraft.ConnectionProfileId);
        return ClientIdentityProfileEditFieldTextBounds(index, ClientIdentityProfileEditField.LoginName, isLocalMatch).Contains(point) ? ClientIdentityProfileEditField.LoginName :
            !isLocalMatch && ClientIdentityProfileEditFieldTextBounds(index, ClientIdentityProfileEditField.LoginPass, false).Contains(point) ? ClientIdentityProfileEditField.LoginPass : null;
    }
    public int GetClientIdentityProfileEditCaretIndex(Point point, int index, ClientIdentityProfileEditField field, string text, bool isLocalMatch) =>
        GetTextBoxCaretIndex(point.X, text, ClientIdentityProfileEditFieldTextBounds(index, field, isLocalMatch), 0.34f);
    public static int? GetClientIdentityProfileEditItemHit(Point point, GoAppSession session)
    {
        var count = session.GetPlayerClientIdentityProfiles(session.PlayerEditDraft.Id).Count;
        for (var index = 0; index < count; index++)
            if (new Rectangle(466, 290 + index * 92, 1008, 78).Contains(point)) return index;
        return null;
    }

    public static int? GetClientIdentityProfileSelectionItemHit(Point point, GoAppSession session) =>
        GetClientIdentityProfileEditItemHit(point, session);

    public static EntryProfileEditField? GetPlayerEditPanelFieldHit(Point point) =>
        PlayerEditPanelFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point) ? EntryProfileEditField.DisplayName :
        null;

    public int GetPlayerEditPanelCaretIndex(Point point, EntryProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, PlayerEditPanelFieldTextBounds(field), 0.42f);

    public static int? GetPlayerSelectionDialogItemHit(Point point, GoAppSession session)
    {
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.EntryProfiles.Count) break;
            if (PlayerSelectionItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }
    public static int? GetPlayerSelectionClientIdentityItemHit(Point point, GoAppSession session)
    {
        var identities = session.GetPlayerSelectionClientIdentities();
        for (var index = 0; index < identities.Count; index++)
            if (PlayerSelectionClientIdentityItemBounds(index).Contains(point)) return index;
        return null;
    }

    private void DrawSetupPlayerRow(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var player = session.GetSelectedEntryProfile(stone);
        var label = stone == GoStone.Black ? "BLACK PLAYER" : "WHITE PLAYER";
        DrawPlayerSelector(
            PlayerSelectorLayout.CreatePlayerSelector(y) with
            {
                Label = label,
                Value = player?.DisplayName ?? "SELECT PLAYER",
                IsComputer = player is null ? null : player.Kind == EntryProfileKind.Computer,
            },
            mousePoint);
        var handleBounds = LocalMatchHandleBounds(y);
        DrawFittedText("HANDLE", new Rectangle(1156, handleBounds.Y + 4, 118, 32), UiLabel.TextColor, 0.34f);
        var textBounds = LocalMatchHandleTextBounds(stone);
        var active = session.ActiveLocalMatchHandleStone == stone;
        var hovered = textBounds.Contains(mousePoint);
        var text = session.GetLocalMatchHandleDraft(stone);
        DrawFittedText(text, textBounds, Color.White, 0.32f);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (active)
        {
            DrawTextBoxSelection(text, session.LocalMatchHandleSelectionStart, session.LocalMatchHandleSelectionLength, textBounds, 0.32f);
            DrawTextBoxCaret(text, session.LocalMatchHandleCaretIndex, textBounds, 0.32f);
        }
        if (hovered && !active)
        {
            // 入力欄のアンダーライン終端の近くに、読みやすい反転プレートで表示する。
            var hintBounds = new Rectangle(textBounds.Right - 76, textBounds.Bottom - 25, 70, 23);
            DrawRoundedFill(hintBounds, 6, new Color(185, 196, 255));
            DrawSharpCenteredFittedText("EDIT", hintBounds, new Color(15, 20, 31), 0.34f);
        }
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
        DrawText("Select an Entry Profile on the left, then a Client Identity on the right.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
        DrawCommandButton(PlayerSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOkButtonBounds, "SELECT", false, mousePoint, enabled: session.CanCommitPlayerSelection, scale: 0.34f);

        FillRect(PlayerSelectionListBounds, new Color(15, 20, 26));
        DrawRect(PlayerSelectionListBounds, 1, new Color(67, 84, 92));
        DrawText("ENTRY PROFILES", new Vector2(PlayerSelectionListBounds.X, PlayerSelectionListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        DrawFittedText("PLAYER NAME", new Rectangle(PlayerSelectionListBounds.X + 210, PlayerSelectionListBounds.Y - 30, 180, 22), new Color(180, 210, 215), 0.30f);
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.EntryProfiles.Count) break;
            var bounds = PlayerSelectionItemBounds(slot);
            var selected = index == session.PlayerDialogSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            var player = session.EntryProfiles[index];
            FillRect(bounds, selected ? new Color(38, 91, 78) : hovered ? new Color(42, 53, 61) : new Color(27, 35, 42));
            DrawRect(bounds, selected ? 2 : 1, selected ? new Color(190, 255, 229) : new Color(73, 91, 98));
            DrawFittedText(player.DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 6, bounds.Width - 40, 32), Color.White, 0.50f);
            DrawPlayerRoleFaceIcon(new Vector2(bounds.X + 34, bounds.Y + 55), player.Kind == EntryProfileKind.Computer);
            var detail = session.GetPlayerSelectionDetail(index);
            var detailText = player.Kind == EntryProfileKind.Computer ? $"ENGINE: {detail}" : detail;
            DrawFittedText(detailText, new Rectangle(bounds.X + 58, bounds.Y + 45, bounds.Width - 78, 24), new Color(180, 195, 195), 0.30f);
        }

        FillRect(PlayerSelectionClientIdentityListBounds, new Color(15, 20, 26));
        DrawRect(PlayerSelectionClientIdentityListBounds, 1, new Color(67, 84, 92));
        DrawText("CLIENT IDENTITIES", new Vector2(PlayerSelectionClientIdentityListBounds.X, PlayerSelectionClientIdentityListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        var identities = session.GetPlayerSelectionClientIdentities();
        for (var index = 0; index < identities.Count; index++)
        {
            var identity = identities[index];
            var bounds = PlayerSelectionClientIdentityItemBounds(index);
            var selected = index == session.ClientIdentityDialogSelectionIndex;
            DrawDataRowFrame(bounds, active: selected, hovered: bounds.Contains(mousePoint));
            DrawFittedText(identity.DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 8, bounds.Width - 36, 28), Color.White, 0.40f);
            DrawFittedText($"HANDLE: {identity.LoginName}", new Rectangle(bounds.X + 18, bounds.Y + 39, bounds.Width - 36, 22), new Color(180, 195, 195), 0.27f);
        }

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.EntryProfiles.Count / (double)GoAppSession.PlayerSelectionPageSize));
        var addHeaderBounds = new Rectangle(438, 782, 290, 26);
        FillRect(addHeaderBounds, new Color(56, 54, 84));
        DrawRect(addHeaderBounds, 1, new Color(133, 128, 177));
        var addLabelSize = _font.MeasureString("ADD") * 0.34f;
        DrawText("ADD", new Vector2(addHeaderBounds.Center.X - addLabelSize.X / 2f, addHeaderBounds.Center.Y - addLabelSize.Y / 2f), Color.White, 0.34f);
        DrawCommandButton(PlayerSelectionAddHumanButtonBounds, "HUMAN", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionAddComputerButtonBounds, "COMPUTER", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionEditButtonBounds, "EDIT", false, mousePoint, enabled: session.PlayerDialogSelectionIndex >= 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedEntryProfile, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOrderButtonBounds, "ORDER", false, mousePoint, enabled: session.EntryProfiles.Count > 1, scale: 0.26f);
        DrawCommandButton(PlayerSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.PlayerSelectionPageIndex > 0, scale: 0.34f);
        DrawFittedText($"{session.PlayerSelectionPageIndex + 1} / {pageCount}", new Rectangle(1140, 824, 64, 32), new Color(227, 224, 210), 0.44f);
        DrawCommandButton(PlayerSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.PlayerSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCatalogOrderEditor(
            session.PlayerOrderEditor,
            "PLAYERS",
            mousePoint,
            player => player.DisplayName,
            player => player.Kind == EntryProfileKind.Computer
                ? $"ENGINE: {session.GetEntryProfileSummary(player)}"
                : session.GetEntryProfileSummary(player),
            player => player.Kind == EntryProfileKind.Computer);
    }

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerEditPanelOpen) return;
        var bounds = new Rectangle(510, 270, 900, 480);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 140));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT ENTRY PROFILE", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawCommandButton(PlayerEditPanelCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(PlayerEditPanelSaveButtonBounds, "SAVE", false, mousePoint, scale: 0.36f);
        DrawPlayerEditField(session, EntryProfileEditField.DisplayName, "DISPLAY NAME", mousePoint);
        DrawPlayerEditPopupField("HANDLE", session.PlayerEditClientIdentityHandle, PlayerEditPanelClientIdentityTextBounds, mousePoint);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPlayerEditPopupField("ENGINE", session.PlayerEditEngineDisplayName, PlayerEditPanelEngineTextBounds, mousePoint);
        DrawPlayerEditStickyNote(session, mousePoint);
    }

    private void DrawClientIdentityProfileEditPanel(GoAppSession session, Point mousePoint)
    {
        if (session.IsClientIdentityProfileSelectionPanelOpen)
        {
            DrawClientIdentityProfileSelectionPanel(session, mousePoint);
            return;
        }
        if (!session.IsClientIdentityProfileEditPanelOpen) return;
        var bounds = new Rectangle(430, 150, 1080, 760);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT CLIENT IDENTITY", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText("機械で扱えるフォーマットのプレイヤー情報を設定できます。", new Rectangle(bounds.X + 36, bounds.Y + 82, 860, 32), new Color(180, 195, 195), 0.34f);
        var targets = session.GetPlayerClientIdentityProfiles(session.PlayerEditDraft.Id);
        DrawCommandButton(ClientIdentityProfileEditCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(ClientIdentityProfileEditSaveButtonBounds, "SAVE", false, mousePoint, scale: 0.36f);
        var edited = session.ClientIdentityProfileEditDraft;
        var isLocalMatch = string.IsNullOrEmpty(edited.ConnectionProfileId);
        DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.LoginName, "HANDLE", mousePoint, isLocalMatch);
        if (!isLocalMatch)
            DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.LoginPass, "PASSWORD", mousePoint, false);
        if (!session.IsClientIdentityProfileConnectionSelectionPanelOpen) return;
        for (var index = 0; index < targets.Count; index++)
        {
            // 編集画面には、操作対象の一件だけを表示する。選択リストは別画面へ分離する。
            if (index != session.ClientIdentityProfileEditIndex) continue;
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var isSelectedClientIdentity = index == session.ClientIdentityProfileEditIndex;
            if (isSelectedClientIdentity)
            {
                DrawText("▶", new Vector2(row.X + 4, row.Y + 25), new Color(147, 244, 200), 0.34f);
                DrawRect(row, 2, new Color(147, 244, 200));
            }
            if (isSelectedClientIdentity)
            {
                var isLocalMatchForLegacy = string.IsNullOrEmpty(target.ConnectionProfileId);
                DrawClientIdentityProfileEditField(session, index, ClientIdentityProfileEditField.LoginName, "HANDLE", mousePoint, isLocalMatchForLegacy);
                if (!isLocalMatchForLegacy)
                    DrawClientIdentityProfileEditField(session, index, ClientIdentityProfileEditField.LoginPass, "PASSWORD", mousePoint, false);
                DrawFittedText($"CONNECTION: {session.ClientIdentityProfileEditConnectionDisplayName}", new Rectangle(row.X + 18, row.Y + 47, 920, 22), new Color(147, 244, 200), 0.28f);
                if (session.IsClientIdentityProfileInUse(index))
                    DrawFittedText("IN USE", new Rectangle(row.Right - 130, row.Y + 45, 110, 22), new Color(147, 244, 200), 0.28f);
            }
            else
            {
                DrawFittedText(target.DisplayName, new Rectangle(row.X + 18, row.Y + 10, 240, 28), Color.White, 0.46f);
                DrawFittedText($"HANDLE: {target.LoginName}", new Rectangle(row.X + 280, row.Y + 10, 390, 28), new Color(180, 195, 195), 0.34f);
                DrawFittedText(session.GetClientIdentityProfileConnectionDisplayName(target), new Rectangle(row.X + 280, row.Y + 42, 520, 24), new Color(147, 244, 200), 0.30f);
            }
        }
    }

    private void DrawClientIdentityProfileSelectionPanel(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(430, 150, 1080, 760);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("USE CLIENT IDENTITY", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawFittedText("GREEN: selected     BLUE: current operation", new Rectangle(bounds.X + 36, bounds.Y + 82, 500, 26), new Color(180, 210, 215), 0.31f);
        var targets = session.GetPlayerClientIdentityProfiles(session.PlayerEditDraft.Id);
        DrawCommandButton(ClientIdentityProfileSelectionUseButtonBounds, "USE", false, mousePoint, enabled: targets.Count > 0 && !session.IsClientIdentityProfileInUse(session.ClientIdentityProfileSelectionIndex), scale: 0.34f);
        DrawCommandButton(ClientIdentityProfileSelectionCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.34f);
        var addBounds = new Rectangle(ClientIdentityProfileEditAddLocalButtonBounds.X, 786, ClientIdentityProfileEditAddCgosButtonBounds.Right - ClientIdentityProfileEditAddLocalButtonBounds.X, 24);
        FillRect(addBounds, new Color(56, 54, 84));
        DrawRect(addBounds, 1, new Color(133, 128, 177));
        DrawCenteredText("ADD", new Vector2(addBounds.Center.X, addBounds.Center.Y), Color.White, 0.34f);
        DrawCommandButton(ClientIdentityProfileEditAddLocalButtonBounds, "LOCAL MATCH", false, mousePoint, enabled: targets.Count < 5, scale: 0.25f);
        DrawCommandButton(ClientIdentityProfileEditAddCgosButtonBounds, "ONLINE MATCH", false, mousePoint, enabled: targets.Count < 5, scale: 0.22f);
        DrawCommandButton(ClientIdentityProfileSelectionEditButtonBounds, "EDIT", false, mousePoint, enabled: targets.Count > 0, scale: 0.34f);
        DrawCommandButton(ClientIdentityProfileEditRemoveButtonBounds, "REMOVE", false, mousePoint, enabled: targets.Count > 1, scale: 0.30f);

        var firstRow = new Rectangle(bounds.X + 36, bounds.Y + 140, bounds.Width - 72, 78);
        DrawFittedText("HANDLE", new Rectangle(firstRow.X + 18, firstRow.Y - 27, 410, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("PASSWORD", new Rectangle(firstRow.X + 460, firstRow.Y - 27, 180, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("SERVICE", new Rectangle(firstRow.X + 680, firstRow.Y - 27, 260, 24), new Color(180, 210, 215), 0.30f);

        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var selected = session.IsClientIdentityProfileInUse(index);
            var operated = index == session.ClientIdentityProfileSelectionIndex;
            FillRect(row, selected ? new Color(38, 103, 86) : row.Contains(mousePoint) ? new Color(43, 52, 62) : new Color(24, 31, 37));
            DrawRect(row, operated ? 3 : selected ? 2 : 1, operated ? new Color(125, 225, 255) : selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
            if (operated) DrawSelectionFingerMark(new Vector2(row.X - 55, row.Center.Y - 13), 1.65f);
            DrawFittedText(target.LoginName, new Rectangle(row.X + 18, row.Y + 25, 410, 30), Color.White, 0.42f);
            DrawFittedText(string.IsNullOrEmpty(target.LoginPass) ? "NONE" : "SET", new Rectangle(row.X + 460, row.Y + 25, 180, 30), Color.White, 0.34f);
            DrawFittedText(string.IsNullOrEmpty(target.ConnectionProfileId) ? "LOCAL MATCH" : "ONLINE MATCH", new Rectangle(row.X + 680, row.Y + 25, 260, 30), Color.White, 0.34f);
        }
    }

    private void DrawClientIdentityProfileConnectionSelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsClientIdentityProfileConnectionSelectionPanelOpen) return;

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(ClientIdentityProfileConnectionSelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(ClientIdentityProfileConnectionSelectionPanelBounds, 2, new Color(116, 145, 146));
        DrawText("SELECT ONLINE MATCH SERVER", new Vector2(542, 240), new Color(244, 238, 218), 0.50f);
        DrawText("Choose the OnlineMatch (CGOS) server for this Client Identity.", new Vector2(544, 294), new Color(180, 195, 195), 0.28f);
        DrawCommandButton(ClientIdentityProfileConnectionSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(ClientIdentityProfileConnectionSelectionSelectButtonBounds, "SELECT", false, mousePoint, scale: 0.34f);

        for (var slot = 0; slot < GoAppSession.ClientIdentityProfileConnectionSelectionPageSize; slot++)
        {
            var index = session.ClientIdentityProfileConnectionSelectionPageIndex * GoAppSession.ClientIdentityProfileConnectionSelectionPageSize + slot;
            if (index >= session.CgosConnectionProfiles.Count) break;
            var connection = session.CgosConnectionProfiles[index];
            var bounds = ClientIdentityProfileConnectionSelectionItemBounds(slot);
            var selected = index == session.ClientIdentityProfileConnectionSelectionIndex;
            DrawDataRowFrame(bounds, active: selected, hovered: bounds.Contains(mousePoint));
            DrawFittedText(connection.DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 10, bounds.Width - 40, 28), Color.White, 0.40f);
            DrawFittedText($"{connection.Host}:{connection.Port}  /  {connection.Event}  /  {connection.Round}".Trim(), new Rectangle(bounds.X + 20, bounds.Y + 43, bounds.Width - 40, 22), new Color(180, 195, 195), 0.26f);
        }

        DrawFittedText($"PAGE {session.ClientIdentityProfileConnectionSelectionPageIndex + 1} / {session.ClientIdentityProfileConnectionSelectionPageCount}", new Rectangle(872, 805, 168, 30), new Color(227, 224, 210), 0.34f);
        DrawCommandButton(ClientIdentityProfileConnectionSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.ClientIdentityProfileConnectionSelectionPageIndex > 0, scale: 0.32f);
        DrawCommandButton(ClientIdentityProfileConnectionSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.ClientIdentityProfileConnectionSelectionPageIndex < session.ClientIdentityProfileConnectionSelectionPageCount - 1, scale: 0.32f);
    }

    private void DrawQuickClientIdentitySelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsQuickClientIdentitySelectionPanelOpen) return;
        var targets = session.GetQuickClientIdentitySelectionTargets(session.QuickClientIdentitySelectionStone, session.QuickClientIdentitySelectionIsCgos);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 125));
        FillRect(QuickClientIdentitySelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(QuickClientIdentitySelectionPanelBounds, 2, new Color(116, 145, 146));
        var service = session.QuickClientIdentitySelectionIsCgos ? "ONLINE MATCH (CGOS)" : "LOCAL MATCH";
        DrawText("SELECT TEMPORARY HANDLE", new Vector2(590, 277), new Color(244, 238, 218), 0.56f);
        DrawStickyNote(
            StickyNoteKind.QuickClientIdentityHandleHint,
            new Vector2(960, 805),
            new Color(147, 244, 200),
            new Color(116, 145, 146),
            "HANDLE とは？",
            ["機械に入力できる書式に従った、Player の Entry 名です。", $"この一覧は {service} 用です。選択は今回だけに適用されます。"],
            bodyLineSpacing: 30);
        DrawCommandButton(QuickClientIdentitySelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.30f);
        DrawCommandButton(QuickClientIdentitySelectionSelectButtonBounds, "SELECT", false, mousePoint, scale: 0.30f);
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var bounds = QuickClientIdentitySelectionItemBounds(index);
            DrawDataRowFrame(bounds, active: index == session.QuickClientIdentitySelectionIndex, hovered: bounds.Contains(mousePoint));
            DrawFittedText(target.LoginName, new Rectangle(bounds.X + 18, bounds.Y + 9, 420, 28), Color.White, 0.42f);
            DrawFittedText(target.DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 43, 420, 20), new Color(180, 195, 195), 0.27f);
        }
    }

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);

    private static Rectangle PlayerSelectionClientIdentityItemBounds(int index) => new(PlayerSelectionClientIdentityListBounds.X + 16, PlayerSelectionClientIdentityListBounds.Y + 14 + index * 82, PlayerSelectionClientIdentityListBounds.Width - 32, 72);

    private static Rectangle LocalMatchHandleBounds(int y) => new(1144, y + 48, 668, 40);

    private static Rectangle LocalMatchHandleTextBounds(GoStone stone)
    {
        var bounds = LocalMatchHandleBounds(stone == GoStone.Black ? BlackPlayerKindButtonY : WhitePlayerKindButtonY);
        return new Rectangle(GameOverValueX, bounds.Y + 4, 410, 30);
    }

    private static Rectangle ClientIdentityProfileConnectionSelectionItemBounds(int slot) => new(544, 332 + slot * 82, 832, 70);

    private static Rectangle QuickClientIdentitySelectionItemBounds(int index) => new(592, 392 + index * 72, 736, 62);

    private static Rectangle ClientIdentityProfileEditFieldTextBounds(int index, ClientIdentityProfileEditField field, bool isLocalMatch)
    {
        var rowY = 358;
        return field switch
        {
            ClientIdentityProfileEditField.DisplayName => new Rectangle(760, rowY + 7, 600, 42),
            ClientIdentityProfileEditField.LoginName => new Rectangle(760, rowY + 7, 600, 42),
            ClientIdentityProfileEditField.LoginPass => new Rectangle(760, rowY + 71, 600, 42),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown target edit field."),
        };
    }

    private void DrawClientIdentityProfileEditField(GoAppSession session, int index, ClientIdentityProfileEditField field, string label, Point mousePoint, bool isLocalMatch)
    {
        var textBounds = ClientIdentityProfileEditFieldTextBounds(index, field, isLocalMatch);
        var active = session.ActiveClientIdentityProfileEditField == field;
        DrawText(label, new Vector2(552, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        var hovered = textBounds.Contains(mousePoint);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        var text = session.GetClientIdentityProfileEditField(field);
        var displayText = field == ClientIdentityProfileEditField.LoginPass && !active ? new string('*', text.Length) : text;
        if (active)
            DrawTextBoxSelection(text, session.ClientIdentityProfileEditSelectionStart, session.ClientIdentityProfileEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(displayText) ? "-" : displayText, textBounds, Color.White, 0.42f);
        if (active)
            DrawTextBoxCaret(text, session.ClientIdentityProfileEditCaretIndex, textBounds, 0.42f);
        if (hovered && !active) DrawPlayerEditHint("EDIT", textBounds);
        if (field == ClientIdentityProfileEditField.LoginName && PlayerEditFieldHoverBounds(textBounds).Contains(mousePoint))
            DrawClientIdentityHandleStickyNote(textBounds);
    }

    private void DrawClientIdentityHandleStickyNote(Rectangle textBounds)
    {
        DrawStickyNote(StickyNoteKind.ClientIdentityHandleHint, PlayerEditUnderlineConnectorStart(textBounds), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局サービスにログインするときの、プレイヤー固有の名前。", "接続する相手の機械に入力できるフォーマットに合わせます。"], bodyLineSpacing: 26);
    }

    private static Rectangle PlayerEditPanelFieldTextBounds(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => new(760, 375, 600, 42),
        EntryProfileEditField.Identifier => new(760, 439, 600, 42),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    private static readonly Rectangle PlayerEditPanelClientIdentityTextBounds = new(760, 439, 600, 42);
    private static readonly Rectangle PlayerEditPanelEngineTextBounds = new(760, 503, 600, 42);

    private static Rectangle PlayerEditFieldHoverBounds(Rectangle textBounds) =>
        new(552, textBounds.Y, textBounds.Right - 552, textBounds.Height);

    private void DrawPlayerEditField(GoAppSession session, EntryProfileEditField field, string label, Point mousePoint)
    {
        var textBounds = PlayerEditPanelFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        DrawText(label, new Vector2(552, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        var hovered = textBounds.Contains(mousePoint);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        var text = session.GetPlayerEditFieldText(field);
        if (active)
            DrawTextBoxSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
            DrawTextBoxCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);
        if (hovered && !active)
            DrawPlayerEditHint("EDIT", textBounds);
    }

    private void DrawPlayerEditPopupField(string label, string value, Rectangle textBounds, Point mousePoint)
    {
        var hovered = textBounds.Contains(mousePoint);
        DrawText(label, new Vector2(552, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawFittedText(value, textBounds, Color.White, 0.42f);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (hovered)
            DrawPlayerEditHint("CHANGE", textBounds);
    }

    private void DrawPlayerEditHint(string text, Rectangle textBounds)
    {
        var hintBounds = text == "EDIT"
            ? new Rectangle(textBounds.Right - 76, textBounds.Bottom - 25, 70, 23)
            : new Rectangle(textBounds.Right - 108, textBounds.Bottom - 28, 100, 26);
        DrawRoundedFill(hintBounds, 6, new Color(185, 196, 255));
        DrawSharpCenteredFittedText(text, hintBounds, new Color(15, 20, 31), 0.34f);
    }

    private void DrawPlayerEditStickyNote(GoAppSession session, Point mousePoint)
    {
        var displayNameBounds = PlayerEditPanelFieldTextBounds(EntryProfileEditField.DisplayName);
        var handleBounds = PlayerEditPanelClientIdentityTextBounds;
        var engineBounds = PlayerEditPanelEngineTextBounds;
        string? heading = null;
        string[]? bodyLines = null;
        Vector2 connectorStart = default;

        if (PlayerEditFieldHoverBounds(displayNameBounds).Contains(mousePoint))
        {
            heading = "DISPLAY NAME とは？";
            bodyLines = ["画面に表示され、棋譜に書き込まれる、プレイヤーの呼び名。"];
            connectorStart = PlayerEditUnderlineConnectorStart(displayNameBounds);
        }
        else if (PlayerEditFieldHoverBounds(handleBounds).Contains(mousePoint))
        {
            heading = "HANDLE とは？";
            bodyLines = ["対局サービスにログインするときの、プレイヤー固有の名前。", "接続する相手の機械に入力できるフォーマットに合わせます。"];
            connectorStart = PlayerEditUnderlineConnectorStart(handleBounds);
        }
        else if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer && PlayerEditFieldHoverBounds(engineBounds).Contains(mousePoint))
        {
            heading = "ENGINE とは？";
            bodyLines = ["コンピューターとして着手するための GTP エンジン。"];
            connectorStart = PlayerEditUnderlineConnectorStart(engineBounds);
        }

        if (heading is null || bodyLines is null) return;
        DrawStickyNote(
            StickyNoteKind.EntryProfileFieldHint,
            connectorStart,
            new Color(185, 196, 255),
            new Color(116, 145, 178),
            heading,
            bodyLines,
            bodyLineSpacing: 26);
    }

    private static Vector2 PlayerEditUnderlineConnectorStart(Rectangle textBounds) =>
        // アンダーラインの中心線へ、右端から少し内側で接続する。
        new(textBounds.Right - 24, textBounds.Bottom + 4);

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
