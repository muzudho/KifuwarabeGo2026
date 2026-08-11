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
    private static readonly Rectangle PlayerSelectionPreviousButtonBounds = new(438, 806, 138, 48);
    private static readonly Rectangle PlayerSelectionNextButtonBounds = new(1344, 806, 138, 48);

    public static bool GetBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(BlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(WhitePlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiBlackPlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiBlackPlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPonnukiWhitePlayerSelectButtonHit(Point point) => PlayerSelectorLayout.CreatePlayerSelector(PonnukiWhitePlayerKindButtonY).ContainsBrowseButton(point);
    public static bool GetPlayerSelectionDialogCancelButtonHit(Point point) => PlayerSelectionCancelButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogOkButtonHit(Point point) => PlayerSelectionOkButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogPreviousPageButtonHit(Point point) => PlayerSelectionPreviousButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogNextPageButtonHit(Point point) => PlayerSelectionNextButtonBounds.Contains(point);

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
        DrawCommandButton(PlayerSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.PlayerSelectionPageIndex > 0, scale: 0.42f);
        DrawFittedText($"PAGE {session.PlayerSelectionPageIndex + 1} / {pageCount}", new Rectangle(748, 814, 424, 32), new Color(227, 224, 210), 0.42f);
        DrawCommandButton(PlayerSelectionNextButtonBounds, "NEXT", false, mousePoint, enabled: session.PlayerSelectionPageIndex < pageCount - 1, scale: 0.42f);
    }

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);
}
