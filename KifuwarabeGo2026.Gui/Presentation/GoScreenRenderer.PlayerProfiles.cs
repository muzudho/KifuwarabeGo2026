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
    private static readonly Rectangle TargetProfileEditCloseButtonBounds = new(1320, 182, 150, 48);
    private static readonly Rectangle TargetProfileEditAddCgosButtonBounds = new(466, 820, 150, 48);
    private static readonly Rectangle TargetProfileEditAddLocalButtonBounds = new(628, 820, 160, 48);
    private static readonly Rectangle TargetProfileEditRemoveButtonBounds = new(800, 820, 140, 48);

    public static bool GetBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(BlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(WhitePlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiBlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiWhitePlayerKindButtonY).ContainsBrowseButton(point);
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
    public static bool GetTargetProfileEditCloseButtonHit(Point point) => TargetProfileEditCloseButtonBounds.Contains(point);
    public static bool GetTargetProfileEditAddCgosButtonHit(Point point) => TargetProfileEditAddCgosButtonBounds.Contains(point);
    public static bool GetTargetProfileEditAddLocalButtonHit(Point point) => TargetProfileEditAddLocalButtonBounds.Contains(point);
    public static bool GetTargetProfileEditRemoveButtonHit(Point point) => TargetProfileEditRemoveButtonBounds.Contains(point);
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
        DrawText($"{(cgos ? "CGOS PLAYER" : "PLAYER")} SELECT  {target}", new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
        DrawText(cgos ? "Choose a computer player with a CGOS target for this connection." : "Human and computer players are selected from one list.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
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
        if (session.PlayerEditDraft.Kind == PlayerProfileKind.Computer)
        {
            DrawText("ENGINE", new Vector2(552, 510), new Color(180, 195, 195), 0.36f);
            var engineTextBounds = new Rectangle(760, 503, 600, 42);
            DrawFittedText(session.PlayerEditEngineDisplayName, engineTextBounds, Color.White, 0.42f);
            DrawCommandButton(PlayerEditPanelEngineOptionsButtonBounds, "CHANGE ENGINE", false, mousePoint, scale: 0.28f);
        }
        DrawCommandButton(PlayerEditPanelTargetsButtonBounds, "TARGETS...", false, mousePoint, scale: 0.32f);
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
        DrawCommandButton(TargetProfileEditCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.34f);
        var targets = session.GetPlayerTargetProfiles(session.PlayerEditDraft.Id);
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            DrawDataRowFrame(row, active: index == session.TargetProfileEditIndex);
            DrawFittedText(target.DisplayName, new Rectangle(row.X + 18, row.Y + 10, 240, 28), Color.White, 0.46f);
            DrawFittedText($"LOGIN NAME: {target.LoginName}", new Rectangle(row.X + 280, row.Y + 10, 390, 28), new Color(180, 195, 195), 0.34f);
            DrawFittedText(session.GetTargetProfileConnectionDisplayName(target), new Rectangle(row.X + 280, row.Y + 42, 520, 24), new Color(147, 244, 200), 0.30f);
        }
        DrawCommandButton(TargetProfileEditAddCgosButtonBounds, "ADD CGOS", false, mousePoint, enabled: targets.Count < 5, scale: 0.32f);
        DrawCommandButton(TargetProfileEditAddLocalButtonBounds, "ADD LOCAL", false, mousePoint, enabled: targets.Count < 5, scale: 0.32f);
        DrawCommandButton(TargetProfileEditRemoveButtonBounds, "REMOVE", false, mousePoint, enabled: targets.Count > 1, scale: 0.32f);
    }

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);

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
