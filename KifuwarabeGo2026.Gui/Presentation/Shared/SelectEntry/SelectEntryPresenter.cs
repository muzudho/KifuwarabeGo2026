namespace KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using static KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry.SelectEntryScreenBounds;

/// <summary>［SELECT ENTRY］画面の表示と操作判定を担当します。</summary>
public sealed class SelectEntryPresenter
{
    public static SelectEntryPresenter Default { get; } = new();

    private SelectEntryPresenter()
    {
    }

    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerSelectionDialogOpen) return;
        var screen = SelectEntryScreen.Default;
        var management = session.PlayerSelectionPurpose == PlayerSelectionPurpose.Management;
        screen.SelectButton.Label = management ? "CLOSE" : "SELECT";
        screen.CancelButton.IsEnabled = !management;
        var pageCount = Math.Max(1, (int)Math.Ceiling(session.EntryProfiles.Count / (double)GoAppSession.PlayerSelectionPageSize));
        screen.UpdateState(
            management || session.CanCommitPlayerSelection,
            session.GtpEngineProfiles.Count > 0,
            session.PlayerDialogSelectionIndex >= 0,
            session.CanDeleteSelectedEntryProfile,
            session.EntryProfiles.Count > 1,
            session.PlayerSelectionPageIndex > 0,
            session.PlayerSelectionPageIndex < pageCount - 1);
        drawingContext.FillRectangle(new Rectangle(0, 0, drawingContext.ScreenWidth, drawingContext.ScreenHeight), new Color(0, 0, 0, 125));
        drawingContext.FillRectangle(new Rectangle(PlayerSelectionDialogBounds.X + 16, PlayerSelectionDialogBounds.Y + 18, PlayerSelectionDialogBounds.Width, PlayerSelectionDialogBounds.Height), new Color(0, 0, 0, 150));
        drawingContext.FillRectangle(PlayerSelectionDialogBounds, new Color(19, 24, 31, 250));
        drawingContext.DrawRectangle(PlayerSelectionDialogBounds, 2, new Color(116, 145, 146));

        var stone = session.PlayerSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        drawingContext.DrawText(management ? "ENTRY PROFILES" : $"SELECT ENTRY ({stone})", new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
        drawingContext.DrawText(management ? "Add, edit, remove, and order match entry profiles." : "Select an entry profile for this player. Computer entries use a registered engine profile.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
        if (!management) screen.CancelButton.Draw(mousePoint, drawingContext);
        screen.SelectButton.Draw(mousePoint, drawingContext);

        drawingContext.FillRectangle(PlayerSelectionListBounds, new Color(15, 20, 26));
        drawingContext.DrawRectangle(PlayerSelectionListBounds, 1, new Color(67, 84, 92));
        drawingContext.DrawText("ENTRY PROFILES", new Vector2(PlayerSelectionListBounds.X, PlayerSelectionListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        if (session.EntryProfiles.Count == 0)
            drawingContext.DrawFittedText(management ? "NO ENTRY PROFILE - ADD HUMAN OR COMPUTER BELOW." : "NO ENTRY PROFILE - Register one from TITLE > ENTRY PROFILES.", new Rectangle(PlayerSelectionListBounds.X + 24, PlayerSelectionListBounds.Center.Y - 20, PlayerSelectionListBounds.Width - 48, 40), new Color(255, 211, 138), 0.34f);
        drawingContext.DrawFittedText("PLAYER NAME", new Rectangle(PlayerSelectionListBounds.X + 210, PlayerSelectionListBounds.Y - 30, 180, 22), new Color(180, 210, 215), 0.30f);
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.EntryProfiles.Count) break;
            var bounds = PlayerSelectionItemBounds(slot);
            var selected = index == session.PlayerDialogSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            var player = session.EntryProfiles[index];
            drawingContext.FillRectangle(bounds, selected ? new Color(38, 91, 78) : hovered ? new Color(42, 53, 61) : new Color(27, 35, 42));
            drawingContext.DrawRectangle(bounds, selected ? 2 : 1, selected ? new Color(190, 255, 229) : new Color(73, 91, 98));
            drawingContext.DrawFittedText(player.DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 6, bounds.Width - 40, 32), Color.White, 0.50f);
            drawingContext.DrawEntryIcon(new Vector2(bounds.X + 34, bounds.Y + 55));
            var detail = session.GetPlayerSelectionDetail(index);
            var detailText = player.Kind == EntryProfileKind.Computer ? $"ENGINE: {detail}" : detail;
            drawingContext.DrawFittedText(detailText, new Rectangle(bounds.X + 58, bounds.Y + 45, bounds.Width - 78, 24), new Color(180, 195, 195), 0.30f);
        }

        drawingContext.FillRectangle(PlayerSelectionClientIdentityListBounds, new Color(15, 20, 26));
        drawingContext.DrawRectangle(PlayerSelectionClientIdentityListBounds, 1, new Color(67, 84, 92));
        drawingContext.DrawText("CLIENT IDENTITIES", new Vector2(PlayerSelectionClientIdentityListBounds.X, PlayerSelectionClientIdentityListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        var identities = session.GetPlayerSelectionClientIdentities();
        for (var index = 0; index < identities.Count; index++)
        {
            var identity = identities[index];
            var bounds = PlayerSelectionClientIdentityItemBounds(index);
            var selected = index == session.ClientIdentityDialogSelectionIndex;
            drawingContext.DrawDataRowFrame(bounds, active: selected, hovered: bounds.Contains(mousePoint));
            drawingContext.DrawFittedText(identity.DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 8, bounds.Width - 36, 28), Color.White, 0.40f);
            drawingContext.DrawFittedText($"HANDLE: {identity.LoginName}", new Rectangle(bounds.X + 18, bounds.Y + 39, bounds.Width - 36, 22), new Color(180, 195, 195), 0.27f);
        }

        var addHeaderBounds = new Rectangle(270, 846, 272, 26);
        if (management)
        {
        drawingContext.FillRectangle(addHeaderBounds, new Color(56, 54, 84));
        drawingContext.DrawRectangle(addHeaderBounds, 1, new Color(133, 128, 177));
        var addLabelSize = drawingContext.MeasureText("ADD") * 0.34f;
        drawingContext.DrawText("ADD", new Vector2(addHeaderBounds.Center.X - addLabelSize.X / 2f, addHeaderBounds.Center.Y - addLabelSize.Y / 2f), Color.White, 0.34f);
        screen.AddHumanButton.Draw(mousePoint, drawingContext);
        screen.AddComputerButton.Draw(mousePoint, drawingContext);
        screen.DuplicateButton.Draw(mousePoint, drawingContext);
        screen.EditButton.Draw(mousePoint, drawingContext);
        screen.DeleteButton.Draw(mousePoint, drawingContext);
        screen.OrderButton.Draw(mousePoint, drawingContext);
        }
        screen.PreviousButton.Draw(mousePoint, drawingContext);
        drawingContext.DrawFittedText($"{session.PlayerSelectionPageIndex + 1} / {pageCount}", PlayerSelectionPageNumberBounds, new Color(227, 224, 210), 0.44f);
        screen.NextButton.Draw(mousePoint, drawingContext);
        CatalogOrderPresenter.Default.Draw(drawingContext,
            session.PlayerOrderEditor,
            "PLAYERS",
            mousePoint,
            player => player.DisplayName,
            player => player.Kind == EntryProfileKind.Computer
                ? $"ENGINE: {session.GetEntryProfileSummary(player)}"
                : session.GetEntryProfileSummary(player),
            player => player.Kind == EntryProfileKind.Computer);
    }

}
