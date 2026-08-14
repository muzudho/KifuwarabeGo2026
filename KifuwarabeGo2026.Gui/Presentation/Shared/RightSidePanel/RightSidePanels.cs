namespace KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Intermission;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared;
using Microsoft.Xna.Framework;
using System;

public static class RightSidePanelLayout
{
    public static readonly Rectangle Bounds = new(1102, 78, 760, 924);
    public const int PrimaryValueX = 1328;
    public const int SecondaryValueX = 1560;
}

public static class RightSidePanelFrame
{
    public static void Draw(GoScreenRenderer renderer)
    {
        var panel = RightSidePanelLayout.Bounds;
        renderer.FillRectangle(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        renderer.FillRectangle(panel, new Color(21, 25, 32, 236));
        renderer.DrawRectangle(panel, 2, new Color(82, 111, 114));
    }
}

/// <summary>現在のアプリケーション状態に対応する右側パネルを選択して描画します。</summary>
public sealed class RightSidePanel
{
    public static RightSidePanel Default { get; } = new();

    private RightSidePanel()
    {
    }

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint,
        LiveBoardPreview? liveBoardPreview, InitialPositionConciergeView? initialPositionConcierge)
    {
        RightSidePanelFrame.Draw(renderer);

        if (initialPositionConcierge is { IsVisible: true })
        {
            LocalMatchScreen.Default.InitialPositionConciergeRightSidePanel.Draw(renderer, initialPositionConcierge, mousePoint);
            return;
        }

        switch (session.CurrentMode.Kind)
        {
            case GoAppModeKind.Playing:
                LocalMatchPlayPage.Default.RightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.GameOver:
                LocalMatchScreen.Default.GameOverRightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.BoardEditing:
                BoardAndReviewScreen.Default.BoardEditingRightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.VariationEditing:
                BoardAndReviewScreen.Default.VariationEditingRightSidePanel.Draw(renderer, session, mousePoint, liveBoardPreview);
                return;
            case GoAppModeKind.Reviewing:
                BoardAndReviewScreen.Default.ReviewingRightSidePanel.Draw(renderer, session, mousePoint);
                return;
        }

        if (session.UseKind == GoAppUseKind.LocalApps)
            LocalMatchIntermissionPage.Default.RightSidePanel.Draw(renderer, session, mousePoint);
        else
            LocalMatchScreen.Default.SetupRightSidePanel.Draw(renderer, session, mousePoint);
    }
}

public sealed class LocalMatchPlayRightSidePanel
{
    public const int PlayersY = 140;

    internal BoardLensButtonStrip BoardLensButtons { get; } = new(1516, 800);

    internal BoardLensButton? GetBoardLensButtonHit(Point point, bool isLensEnabled) =>
        BoardLensButtons.GetHit(point, isLensEnabled);

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) =>
        LocalMatchPlayPage.Default.DrawRightSidePanelContent(renderer, session, mousePoint);
}

public sealed class LocalMatchIntermissionRightSidePanel
{
    internal const int BlackPlayerKindButtonY = 646;
    internal const int BlackEngineButtonY = 704;
    internal const int WhitePlayerKindButtonY = 750;
    internal const int WhiteEngineButtonY = 808;

    internal RightSidePanelPlayerSelector PlayerSelector { get; } = new();

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) =>
        LocalMatchIntermissionPage.Default.DrawRightSidePanelContent(renderer, session, mousePoint);
}

public sealed class SetupRightSidePanel
{
    internal const int BlackPlayerKindButtonY = 710;
    internal const int BlackEngineButtonY = 768;
    internal const int WhitePlayerKindButtonY = 814;
    internal const int WhiteEngineButtonY = 872;

    internal RightSidePanelPlayerSelector PlayerSelector { get; } = new();

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        var screen = LocalMatchScreen.Default;
        var drawingContext = renderer.StationeryDrawingContext;
        screen.BackToTitleButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 184, 668, 176), "TOURNAMENT", new Color(62, 112, 105));
        TournamentRulesScreen.Default.BrowseButton.Draw(mousePoint, drawingContext);
        screen.ImportSgfButton.Label = session.HasReviewGameRecord ? "KIFU CLEAR (SGF)" : "KIFU INPUT (SGF)";
        screen.ImportSgfButton.Draw(mousePoint, drawingContext);
        renderer.DrawResultRow(new Rectangle(1164, 292, 628, 56), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 376, 668, 304), "RULES", new Color(66, 104, 116));
        renderer.DrawInfoStrip(1144, 384, "RULE", session.RuleKind.ToString());
        renderer.DrawInfoStrip(1144, 456, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        renderer.DrawInfoStrip(1144, 528, "KOMI", session.Komi.ToString("0.0"));
        renderer.DrawInfoStrip(1144, 600, "MOVES", session.MoveLimit <= 0 ? "UNLIMITED" : session.MoveLimit.ToString());

        renderer.DrawVerticalResultSection(new Rectangle(1144, 696, 668, 216), "PLAYERS", new Color(76, 91, 126));
        PlayerSelector.DrawPlayerRow(renderer, session, GoStone.Black, mousePoint, BlackPlayerKindButtonY);
        PlayerSelector.DrawPlayerRow(renderer, session, GoStone.White, mousePoint, WhitePlayerKindButtonY);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        var boardAndReviewScreen = BoardAndReviewScreen.Default;
        boardAndReviewScreen.StartReviewingButton.IsEnabled = session.HasReviewGameRecord;
        boardAndReviewScreen.StartReviewingButton.Draw(mousePoint, drawingContext);
        boardAndReviewScreen.StartBoardEditingButton.Draw(mousePoint, drawingContext);
        screen.StartPlayingButton.Label = session.CanStartPlaying ? "START" : "ENGINE REQUIRED";
        screen.StartPlayingButton.LabelScale = session.CanStartPlaying ? 0.48f : 0.28f;
        screen.StartPlayingButton.IsEnabled = session.CanStartPlaying;
        screen.StartPlayingButton.Draw(mousePoint, drawingContext);
    }
}

public sealed class RightSidePanelPlayerSelector
{
    internal LinkUnderline Underline { get; } = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    public void DrawPlayerRow(GoScreenRenderer renderer, GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var player = session.GetSelectedEntryProfile(stone);
        var label = stone == GoStone.Black ? "BLACK PLAYER" : "WHITE PLAYER";
        Draw(renderer,
            PlayerSelectorLayout.CreatePlayerSelector(y) with
            {
                Label = label,
                Value = player?.DisplayName ?? "SELECT PLAYER",
                IsComputer = player is null ? null : player.Kind == EntryProfileKind.Computer,
            },
            mousePoint);

        var isPonnuki = y is LocalMatchIntermissionRightSidePanel.BlackPlayerKindButtonY or LocalMatchIntermissionRightSidePanel.WhitePlayerKindButtonY;
        var handleBounds = LocalMatchScreen.Default.GetHandleBounds(stone, isPonnuki);
        renderer.DrawRightSidePanelFittedText("HANDLE", new Rectangle(1156, handleBounds.Y + 4, 118, 32), UiLabel.TextColor, 0.34f);
        var textBounds = LocalMatchScreen.Default.GetHandleTextBounds(stone, isPonnuki);
        var active = session.ActiveLocalMatchHandleStone == stone;
        var hovered = textBounds.Contains(mousePoint);
        var text = session.GetLocalMatchHandleDraft(stone);
        renderer.DrawRightSidePanelFittedText(text, textBounds, Color.White, 0.32f);
        renderer.DrawRightSidePanelRoundedFill(
            new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5),
            2,
            active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (active)
        {
            renderer.DrawRightSidePanelTextSelection(text, session.LocalMatchHandleSelectionStart,
                session.LocalMatchHandleSelectionLength, textBounds, 0.32f);
            renderer.DrawRightSidePanelTextCaret(text, session.LocalMatchHandleCaretIndex, textBounds, 0.32f);
        }
        if (hovered && !active)
        {
            var hintBounds = new Rectangle(textBounds.Right - 76, textBounds.Bottom - 25, 70, 23);
            renderer.DrawRightSidePanelRoundedFill(hintBounds, 6, new Color(185, 196, 255));
            renderer.DrawRightSidePanelCenteredFittedText("EDIT", hintBounds, new Color(15, 20, 31), 0.34f);
        }
    }

    private void Draw(GoScreenRenderer renderer, PlayerSelector selector, Point mousePoint)
    {
        if (selector.Bounds.X == 1144 && selector.Bounds.Width == 668)
        {
            var isBlack = selector.Label.StartsWith("BLACK", StringComparison.Ordinal);
            renderer.DrawRightSidePanelIconStone(new Vector2(selector.Bounds.X + 34, selector.Bounds.Center.Y), 13, isBlack);
            if (selector.IsComputer is { } isComputer)
                renderer.DrawRightSidePanelPlayerRoleFaceIcon(new Vector2(selector.Bounds.X + 76, selector.Bounds.Center.Y), isComputer);
            var fieldBounds = new Rectangle(RightSidePanelLayout.PrimaryValueX, selector.Bounds.Y + 6,
                selector.Bounds.Right - RightSidePanelLayout.PrimaryValueX - 34, selector.Bounds.Height - 12);
            var hovered = selector.Enabled && selector.Bounds.Contains(mousePoint);
            var valueBounds = hovered
                ? new Rectangle(fieldBounds.X, fieldBounds.Y, fieldBounds.Width - 122, fieldBounds.Height)
                : fieldBounds;
            renderer.DrawRightSidePanelFittedText(selector.Value, valueBounds, Color.White, 0.42f);
            Underline.Bounds = fieldBounds;
            Underline.SetActionBadge(ActionBadgeComponent.Create("CHANGE", fieldBounds));
            Underline.UpdatePointer(mousePoint);
            Underline.Draw(renderer.StationeryDrawingContext);
            return;
        }

        renderer.DrawRightSidePanelDataRowFrame(selector.Bounds);
        renderer.DrawRightSidePanelFittedText(selector.Label, selector.LabelBounds, new Color(158, 178, 178), 0.36f);
        renderer.DrawRightSidePanelFittedText(selector.Value, selector.ValueBounds, Color.White, 0.52f);
        renderer.DrawRightSidePanelCommandButton(selector.BrowseButtonBounds, selector.ButtonLabel, mousePoint,
            selector.Enabled, PlayerSelectorLayout.SelectButtonLabelScale);
    }
}

public sealed class GameOverRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawGameOverRightSidePanelContent(session, mousePoint);
}

public sealed class BoardEditingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawBoardEditingRightSidePanelContent(session, mousePoint);
}

public sealed class VariationEditingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint, LiveBoardPreview? preview) =>
        renderer.DrawVariationEditingRightSidePanelContent(session, mousePoint, preview);
}

public sealed class ReviewingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawReviewingRightSidePanelContent(session, mousePoint);
}

public sealed class InitialPositionConciergeRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, InitialPositionConciergeView view, Point mousePoint) =>
        renderer.DrawInitialPositionConciergeContent(view, mousePoint);
}
