namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>［SELECT ENTRY］画面の表示と操作判定を担当します。</summary>
public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle PlayerSelectionDialogBounds = new(210, 120, 1500, 840);
    private static readonly Rectangle PlayerSelectionListBounds = new(250, 270, 660, 510);
    private static readonly Rectangle PlayerSelectionClientIdentityListBounds = new(970, 270, 700, 510);
    private static readonly Rectangle PlayerSelectionCancelButtonBounds = new(1116, 180, 156, 50);
    private static readonly Rectangle PlayerSelectionOkButtonBounds = new(1302, 180, 180, 50);
    private static readonly Rectangle PlayerSelectionPageNumberBounds = new(610, 790, 64, 32);
    private static readonly Rectangle PlayerSelectionPreviousButtonBounds = new(686, 782, 104, 48);
    private static readonly Rectangle PlayerSelectionNextButtonBounds = new(802, 782, 116, 48);
    private static readonly Rectangle PlayerSelectionAddHumanButtonBounds = new(270, 880, 110, 48);
    private static readonly Rectangle PlayerSelectionAddComputerButtonBounds = new(392, 880, 150, 48);
    private static readonly Rectangle PlayerSelectionDuplicateButtonBounds = new(554, 880, 128, 48);
    private static readonly Rectangle PlayerSelectionEditButtonBounds = new(694, 880, 120, 48);
    private static readonly Rectangle PlayerSelectionDeleteButtonBounds = new(826, 880, 120, 48);
    private static readonly Rectangle PlayerSelectionOrderButtonBounds = new(958, 880, 140, 48);

    public static bool GetPlayerSelectionDialogCancelButtonHit(Point point) => PlayerSelectionCancelButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogOkButtonHit(Point point) => PlayerSelectionOkButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogPreviousPageButtonHit(Point point) => PlayerSelectionPreviousButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogNextPageButtonHit(Point point) => PlayerSelectionNextButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogAddHumanButtonHit(Point point) => PlayerSelectionAddHumanButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogAddComputerButtonHit(Point point) => PlayerSelectionAddComputerButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogDuplicateButtonHit(Point point) => PlayerSelectionDuplicateButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogDeleteButtonHit(Point point) => PlayerSelectionDeleteButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogEditButtonHit(Point point) => PlayerSelectionEditButtonBounds.Contains(point);
    public static bool GetPlayerSelectionDialogOrderButtonHit(Point point) => PlayerSelectionOrderButtonBounds.Contains(point);

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

    private void DrawPlayerSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerSelectionDialogOpen) return;
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 125));
        FillRect(new Rectangle(PlayerSelectionDialogBounds.X + 16, PlayerSelectionDialogBounds.Y + 18, PlayerSelectionDialogBounds.Width, PlayerSelectionDialogBounds.Height), new Color(0, 0, 0, 150));
        FillRect(PlayerSelectionDialogBounds, new Color(19, 24, 31, 250));
        DrawRect(PlayerSelectionDialogBounds, 2, new Color(116, 145, 146));

        var stone = session.PlayerSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        DrawText($"SELECT ENTRY ({stone})", new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
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
        var addHeaderBounds = new Rectangle(270, 846, 272, 26);
        FillRect(addHeaderBounds, new Color(56, 54, 84));
        DrawRect(addHeaderBounds, 1, new Color(133, 128, 177));
        var addLabelSize = _font.MeasureString("ADD") * 0.34f;
        DrawText("ADD", new Vector2(addHeaderBounds.Center.X - addLabelSize.X / 2f, addHeaderBounds.Center.Y - addLabelSize.Y / 2f), Color.White, 0.34f);
        DrawCommandButton(PlayerSelectionAddHumanButtonBounds, "HUMAN", false, mousePoint, scale: 0.34f);
        DrawCommandButton(PlayerSelectionAddComputerButtonBounds, "COMPUTER", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.PlayerDialogSelectionIndex >= 0, scale: 0.29f);
        DrawCommandButton(PlayerSelectionEditButtonBounds, "EDIT", false, mousePoint, enabled: session.PlayerDialogSelectionIndex >= 0, scale: 0.34f);
        DrawCommandButton(PlayerSelectionDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedEntryProfile, scale: 0.34f);
        DrawCommandButton(PlayerSelectionOrderButtonBounds, "ORDER", false, mousePoint, enabled: session.EntryProfiles.Count > 1, scale: 0.34f);
        DrawCommandButton(PlayerSelectionPreviousButtonBounds, "PREV", false, mousePoint, enabled: session.PlayerSelectionPageIndex > 0, scale: 0.34f);
        DrawFittedText($"{session.PlayerSelectionPageIndex + 1} / {pageCount}", PlayerSelectionPageNumberBounds, new Color(227, 224, 210), 0.44f);
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

    private static Rectangle PlayerSelectionItemBounds(int slot) => new(PlayerSelectionListBounds.X + 16, PlayerSelectionListBounds.Y + 14 + slot * 82, PlayerSelectionListBounds.Width - 32, 72);

    private static Rectangle PlayerSelectionClientIdentityItemBounds(int index) => new(PlayerSelectionClientIdentityListBounds.X + 16, PlayerSelectionClientIdentityListBounds.Y + 14 + index * 82, PlayerSelectionClientIdentityListBounds.Width - 32, 72);
}
