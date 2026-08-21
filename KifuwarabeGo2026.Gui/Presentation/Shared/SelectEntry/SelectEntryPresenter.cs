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
            session.PlayerDialogSelectionIndex >= 0,
            session.CanDeleteSelectedEntryProfile,
            session.EntryProfiles.Count > 1,
            session.PlayerSelectionPageIndex > 0,
            session.PlayerSelectionPageIndex < pageCount - 1);
        drawingContext.FillRectangle(new Rectangle(0, 0, drawingContext.ScreenWidth, drawingContext.ScreenHeight), new Color(0, 0, 0, 125));
        drawingContext.FillRectangle(new Rectangle(PlayerSelectionDialogBounds.X + 16, PlayerSelectionDialogBounds.Y + 18, PlayerSelectionDialogBounds.Width, PlayerSelectionDialogBounds.Height), new Color(0, 0, 0, 150));
        drawingContext.FillRectangle(PlayerSelectionDialogBounds, new Color(19, 24, 31, 250));
        drawingContext.DrawRectangle(PlayerSelectionDialogBounds, 2, new Color(116, 145, 146));

        var targetLabel = session.PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
            ? session.PlayerSelectionTargetStone == GoStone.Black ? "PLAYER 1" : "PRACTICE PLAYER"
            : session.PlayerSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        var title = management
            ? "ENTRY PROFILES"
            : session.PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
                ? $"ENTRY ({targetLabel})"
                : $"SELECT ENTRY ({targetLabel})";
        drawingContext.DrawText(title, new Vector2(PlayerSelectionDialogBounds.X + 34, PlayerSelectionDialogBounds.Y + 28), new Color(244, 238, 218), 0.78f);
        drawingContext.DrawText(management ? "Add, edit, remove, and order match entry profiles." : "Select an entry profile for this player. Computer entries use a registered engine profile.", new Vector2(PlayerSelectionDialogBounds.X + 36, PlayerSelectionDialogBounds.Y + 88), new Color(180, 195, 195), 0.38f);
        if (!management) screen.CancelButton.Draw(mousePoint, drawingContext);
        screen.SelectButton.Draw(mousePoint, drawingContext);

        drawingContext.FillRectangle(PlayerSelectionListBounds, new Color(15, 20, 26));
        drawingContext.DrawRectangle(PlayerSelectionListBounds, 1, new Color(67, 84, 92));
        drawingContext.DrawText("ENTRY PROFILES", new Vector2(PlayerSelectionListBounds.X, PlayerSelectionListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        if (session.EntryProfiles.Count == 0)
            drawingContext.DrawFittedText(management ? "NO ENTRY PROFILE - ADD ONE BELOW." : "NO ENTRY PROFILE - Register one from TITLE > ENTRY PROFILES.", new Rectangle(PlayerSelectionListBounds.X + 24, PlayerSelectionListBounds.Center.Y - 20, PlayerSelectionListBounds.Width - 48, 40), new Color(255, 211, 138), 0.34f);
        var start = session.PlayerSelectionPageIndex * GoAppSession.PlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.PlayerSelectionPageSize; slot++)
        {
            var index = start + slot;
            if (index >= session.EntryProfiles.Count) break;
            var bounds = PlayerSelectionItemBounds(slot);
            var selected = index == session.PlayerDialogSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            var player = session.EntryProfiles[index];
            var inspected = management && selected;
            drawingContext.FillRectangle(bounds, selected && !management ? new Color(38, 91, 78) : hovered ? new Color(42, 53, 61) : new Color(27, 35, 42));
            drawingContext.DrawRectangle(bounds, selected ? 2 : 1, inspected ? new Color(125, 225, 255) : selected ? new Color(190, 255, 229) : new Color(73, 91, 98));
            if (inspected)
                drawingContext.DrawSelectionFingerIcon(new Vector2(bounds.X - 42, bounds.Center.Y - 6), 1.15f);
            drawingContext.DrawEntryIcon(new Vector2(bounds.X + 34, bounds.Y + 21));
            drawingContext.DrawFittedText(player.DisplayName, new Rectangle(bounds.X + 58, bounds.Y + 6, bounds.Width - 78, 32), Color.White, 0.50f);
            var detail = session.GetPlayerSelectionDetail(index);
            var detailText = player.Kind == EntryProfileKind.Computer ? $"ENGINE: {detail}" : detail;
            if (player.Kind == EntryProfileKind.Computer)
                drawingContext.DrawEngineIcon(new Vector2(bounds.X + 66, bounds.Y + 55));
            else
                drawingContext.DrawHumanIcon(new Vector2(bounds.X + 66, bounds.Y + 55));
            drawingContext.DrawFittedText(detailText, new Rectangle(bounds.X + 90, bounds.Y + 45, bounds.Width - 110, 24), new Color(180, 195, 195), 0.30f);
        }

        drawingContext.FillRectangle(PlayerSelectionClientIdentityListBounds, new Color(15, 20, 26));
        drawingContext.DrawRectangle(PlayerSelectionClientIdentityListBounds, 1, new Color(67, 84, 92));
        drawingContext.DrawText(management ? "HANDLE / PASSWORD / COMMENT" : "CLIENT IDENTITIES", new Vector2(PlayerSelectionClientIdentityListBounds.X, PlayerSelectionClientIdentityListBounds.Y - 34), new Color(147, 244, 200), 0.34f);
        var identities = management
            ? session.GetManagedPlayerClientIdentities()
            : session.GetPlayerSelectionClientIdentities();
        if (management)
        {
            var headingBounds = new Rectangle(PlayerSelectionClientIdentityListBounds.X + 16, PlayerSelectionClientIdentityListBounds.Y + 12, PlayerSelectionClientIdentityListBounds.Width - 32, 34);
            drawingContext.DrawFittedText("HANDLE", new Rectangle(headingBounds.X + 54, headingBounds.Y, 190, headingBounds.Height), new Color(180, 195, 195), 0.32f);
            drawingContext.DrawFittedText("PASSWORD", new Rectangle(headingBounds.X + 260, headingBounds.Y, 170, headingBounds.Height), new Color(180, 195, 195), 0.32f);
            drawingContext.DrawFittedText("COMMENT", new Rectangle(headingBounds.X + 446, headingBounds.Y, headingBounds.Width - 446, headingBounds.Height), new Color(180, 195, 195), 0.32f);
        }
        for (var index = 0; index < identities.Count; index++)
        {
            var identity = identities[index];
            var bounds = management
                ? PlayerSelectionClientIdentityItemBounds(index) with { Y = PlayerSelectionClientIdentityItemBounds(index).Y + 34, Height = 62 }
                : PlayerSelectionClientIdentityItemBounds(index);
            if (bounds.Bottom > PlayerSelectionClientIdentityListBounds.Bottom - 8) break;
            var selected = index == session.ClientIdentityDialogSelectionIndex;
            drawingContext.DrawDataRowFrame(bounds, active: !management && selected, hovered: !management && bounds.Contains(mousePoint));
            if (management)
            {
                drawingContext.DrawFittedText($"{index + 1}", new Rectangle(bounds.X + 16, bounds.Y + 13, 28, 34), new Color(178, 219, 226), 0.30f);
                drawingContext.DrawFittedText(string.IsNullOrEmpty(identity.LoginName) ? "-" : identity.LoginName, new Rectangle(bounds.X + 54, bounds.Y + 10, 190, 40), Color.White, 0.36f);
                drawingContext.DrawFittedText(string.IsNullOrEmpty(identity.LoginPass) ? "-" : identity.LoginPass, new Rectangle(bounds.X + 260, bounds.Y + 10, 170, 40), Color.White, 0.36f);
                drawingContext.DrawFittedText(string.IsNullOrEmpty(identity.Comment) ? "-" : identity.Comment, new Rectangle(bounds.X + 446, bounds.Y + 10, bounds.Width - 462, 40), Color.White, 0.34f);
            }
            else
            {
                drawingContext.DrawFittedText(identity.LoginName, new Rectangle(bounds.X + 18, bounds.Y + 8, bounds.Width - 36, 28), Color.White, 0.40f);
                drawingContext.DrawFittedText(string.IsNullOrEmpty(identity.Comment) ? "COMMENT: -" : $"COMMENT: {identity.Comment}", new Rectangle(bounds.X + 18, bounds.Y + 39, bounds.Width - 36, 22), new Color(180, 195, 195), 0.27f);
            }
        }

        if (management)
        {
        screen.AddButton.Draw(mousePoint, drawingContext);
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
