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
    private static readonly Rectangle PlayerEditPanelCloseButtonBounds = new(1190, 624, 170, 52);

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
    public static bool GetPlayerEditPanelCloseButtonHit(Point point) => PlayerEditPanelCloseButtonBounds.Contains(point);

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
        DrawText($"PLAYER SELECT  {target}", new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
        DrawText("Human and computer players are selected from one list.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
        DrawCommandButton(PlayerSelectionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOkButtonBounds, "SELECT", false, mousePoint, enabled: session.PlayerDialogSelectionIndex >= 0, scale: 0.34f);

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
        DrawCommandButton(PlayerSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.PlayerSelectionPageIndex > 0, scale: 0.34f);
        DrawFittedText($"{session.PlayerSelectionPageIndex + 1} / {pageCount}", new Rectangle(1140, 824, 64, 32), new Color(227, 224, 210), 0.44f);
        DrawCommandButton(PlayerSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.PlayerSelectionPageIndex < pageCount - 1, scale: 0.42f);
    }

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerEditPanelOpen) return;
        var bounds = new Rectangle(510, 290, 900, 430);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 140));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT PLAYER", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawFittedText($"DISPLAY NAME  {session.PlayerEditDraft.DisplayName}", new Rectangle(bounds.X + 42, bounds.Y + 110, 816, 42), Color.White, 0.42f);
        DrawFittedText($"IDENTIFIER  {session.PlayerEditDraft.Identifier}", new Rectangle(bounds.X + 42, bounds.Y + 174, 816, 42), Color.White, 0.42f);
        DrawFittedText(session.PlayerEditDraft.Kind == PlayerProfileKind.Computer ? $"ENGINE  {session.PlayerEditEngineDisplayName}" : "HUMAN PLAYER", new Rectangle(bounds.X + 42, bounds.Y + 238, 816, 42), new Color(180, 195, 195), 0.38f);
        DrawText("DISPLAY NAME / IDENTIFIER editing is being connected to PopupTextBox.", new Vector2(bounds.X + 42, bounds.Y + 320), new Color(255, 225, 128), 0.28f);
        DrawCommandButton(PlayerEditPanelCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.40f);
    }

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);
}
