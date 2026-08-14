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
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using static KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.PopupTrendChartScreenBounds;
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
    public SgfAutoSaveCheckBox SgfAutoSaveCheckBox { get; } = new();

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        var drawingContext = renderer.StationeryDrawingContext;
        new Headline("GAME OVER", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f).Draw(drawingContext);
        drawingContext.DrawText($"{session.PlayedMoveCount}手で終局", new Vector2(1144, 196), new Color(99, 223, 185), 0.58f);
        var screen = LocalMatchScreen.Default;
        screen.ReturnToSetupButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 236, 668, 128), "RESULT", new Color(80, 48, 38));
        renderer.DrawResultRow(new Rectangle(1164, 242, 628, 52), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);
        DrawCalculationResultRow(renderer, new Rectangle(1164, 300, 628, 52), session);

        renderer.DrawRightSidePanelGameOverTrendChart(session, mousePoint);

        if (session.UseKind == GoAppUseKind.LocalApps)
        {
            renderer.DrawVerticalResultSection(new Rectangle(1144, 668, 668, 174), "AGEHAMA", new Color(112, 76, 48));
            renderer.DrawRightSidePanelAgehamaSummary(new Rectangle(1164, 692, 628, 132), session.BlackAgehama, session.WhiteAgehama);
        }

        renderer.DrawVerticalResultSection(new Rectangle(1144, 854, 668, 126), "ACTION", new Color(91, 82, 105));
        screen.GameOverReviewButton.Draw(mousePoint, drawingContext);
        if (session.IsSgfAutoSaveAvailable)
            SgfAutoSaveCheckBox.Draw(screen.ExportSgfButton.Bounds, session, mousePoint, drawingContext);
        else
            screen.ExportSgfButton.Draw(mousePoint, drawingContext);
    }

    private static void DrawCalculationResultRow(GoScreenRenderer renderer, Rectangle bounds, GoAppSession session)
    {
        renderer.DrawRightSidePanelResultLabel(bounds, "RESULT", new Color(80, 48, 38));
        const string pureGoPrefix = "PURE GO ";
        var result = string.IsNullOrWhiteSpace(session.GameOverReason) ? "GAME OVER" : session.GameOverReason;
        if (result.StartsWith(pureGoPrefix, StringComparison.Ordinal))
            result = result[pureGoPrefix.Length..];
        var black = result.StartsWith("BLACK ", StringComparison.Ordinal);
        var white = result.StartsWith("WHITE ", StringComparison.Ordinal);
        if (black || white)
        {
            renderer.DrawRightSidePanelStoneValue(RightSidePanelLayout.PrimaryValueX, bounds.Center.Y,
                result[6..], black, new Color(99, 223, 185));
            return;
        }

        renderer.StationeryDrawingContext.DrawFittedText(result,
            new Rectangle(RightSidePanelLayout.PrimaryValueX, bounds.Y + 6,
                bounds.Right - RightSidePanelLayout.PrimaryValueX - 18, bounds.Height - 12),
            new Color(99, 223, 185), 0.58f);
    }
}

public sealed class SgfAutoSaveCheckBox
{
    public void Draw(Rectangle bounds, GoAppSession session, Point mousePoint, StationeryDrawingContext drawingContext)
    {
        var hovered = bounds.Contains(mousePoint);
        drawingContext.FillRectangle(bounds, hovered ? new Color(47, 65, 91, 230) : new Color(31, 45, 70, 220));
        drawingContext.DrawRectangle(bounds, 2, new Color(137, 160, 205));

        var checkBounds = new Rectangle(bounds.X + 12, bounds.Y + (bounds.Height - 28) / 2, 28, 28);
        drawingContext.FillRectangle(checkBounds, new Color(17, 24, 48, 245));
        drawingContext.DrawRectangle(checkBounds, 2, new Color(176, 194, 242));
        if (session.IsSgfAutoSaveEnabled)
        {
            drawingContext.DrawLine(new Vector2(checkBounds.X + 6, checkBounds.Y + 15), new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7), 4, new Color(91, 218, 211));
            drawingContext.DrawLine(new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7), new Vector2(checkBounds.Right - 5, checkBounds.Y + 6), 4, new Color(91, 218, 211));
        }

        var statusWidth = string.IsNullOrEmpty(session.SgfAutoSaveStatus) ? 0 : 116;
        drawingContext.DrawFittedText("AUTO SAVE",
            new Rectangle(checkBounds.Right + 10, bounds.Y + 6, bounds.Width - 60 - statusWidth, bounds.Height - 12),
            Color.White, 0.34f);
        if (statusWidth <= 0) return;
        var statusColor = session.SgfAutoSaveStatus == "AUTO SAVED"
            ? new Color(99, 223, 185)
            : new Color(255, 145, 151);
        drawingContext.DrawFittedText(session.SgfAutoSaveStatus,
            new Rectangle(bounds.Right - statusWidth - 8, bounds.Y + 6, statusWidth, bounds.Height - 12),
            statusColor, 0.28f);
    }
}

public sealed class BoardEditingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        var controls = BoardAndReviewScreen.Default.BoardEditing;
        var drawingContext = renderer.StationeryDrawingContext;
        controls.UpdateState(session.BoardEditingStone, session.CanUndoBoardEditing, session.CanRedoBoardEditing);
        new Headline("BOARD EDIT", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f).Draw(drawingContext);
        controls.CancelButton.Draw(mousePoint, drawingContext);
        controls.AdoptButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 204, 668, 76), "BOARD", new Color(66, 104, 116));
        renderer.DrawResultRow(new Rectangle(1164, 208, 628, 60), "SIZE", $"{session.BoardSize} x {session.BoardSize}", new Color(62, 112, 105), Color.White);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 292, 668, 260), "EDIT", new Color(76, 91, 126));
        renderer.DrawRightSidePanelResultLabel(new Rectangle(1164, 296, 628, 40), "STONE", new Color(76, 91, 126));
        controls.BlackButton.Draw(mousePoint, drawingContext);
        controls.WhiteButton.Draw(mousePoint, drawingContext);
        controls.EraseButton.Draw(mousePoint, drawingContext);

        renderer.DrawRightSidePanelResultLabel(new Rectangle(1164, 414, 628, 40), "HISTORY", new Color(76, 91, 126));
        controls.UndoButton.Draw(mousePoint, drawingContext);
        controls.RedoButton.Draw(mousePoint, drawingContext);
        controls.ClearButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 564, 668, 220), "POSITION", new Color(62, 112, 105));
        renderer.DrawRightSidePanelStoneCountStrip(session, 584, showLeader: false, minimal: true);
        DrawCurrentStoneResultRow(renderer, new Rectangle(1164, 690, 628, 64), session);
    }

    private static void DrawCurrentStoneResultRow(GoScreenRenderer renderer, Rectangle bounds, GoAppSession session)
    {
        renderer.DrawRightSidePanelResultLabel(bounds, "RESULT", new Color(80, 48, 38));
        var difference = session.BlackStoneCount - session.WhiteStoneCount;
        if (difference == 0)
        {
            renderer.StationeryDrawingContext.DrawText("EVEN",
                new Vector2(RightSidePanelLayout.PrimaryValueX, bounds.Center.Y - 14),
                new Color(99, 223, 185), 0.5f);
            return;
        }

        renderer.DrawRightSidePanelStoneValue(RightSidePanelLayout.PrimaryValueX, bounds.Center.Y,
            $"+{Math.Abs(difference)}", difference > 0, new Color(99, 223, 185));
    }
}

public sealed class VariationEditingRightSidePanel
{
    // ========================================
    // 機能
    // ========================================

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint, LiveBoardPreview? preview)
    {
        var controls = BoardAndReviewScreen.Default.VariationEditing;
        var drawingContext = renderer.StationeryDrawingContext;
        controls.UpdateState(session.VariationEditingStone, session.CanAdoptVariationPosition, session.CanUndoVariation);
        new Headline("ANALYSIS BOARD", new Vector2(1144, 136), new Color(42, 62, 68), 0.68f).Draw(drawingContext);
        controls.DiscardButton.Draw(mousePoint, drawingContext);
        if (session.CanAdoptVariationPosition)
            controls.AdoptButton.Draw(mousePoint, drawingContext);

        var informationWidth = preview is null ? 668 : 372;
        var informationRowWidth = preview is null ? 628 : 332;
        renderer.DrawVerticalResultSection(new Rectangle(1144, 204, informationWidth, 112), "EDITING", new Color(67, 112, 118));
        renderer.DrawResultRow(new Rectangle(1164, 210, informationRowWidth, 44), "SOURCE",
            session.HasVariationCustomPosition ? "CUSTOM POSITION" : $"MOVE {session.VariationSourceMoveIndex}",
            new Color(67, 112, 118), Color.White);
        renderer.DrawResultRow(new Rectangle(1164, 260, informationRowWidth, 44), "VARIATION",
            $"+{session.VariationMoveCount} MOVES", new Color(67, 112, 118), new Color(99, 223, 185));

        renderer.DrawVerticalResultSection(new Rectangle(1144, 332, informationWidth, 200), "POSITION", new Color(76, 91, 126));
        renderer.DrawBothPlayersComponent(1144, 340, informationWidth,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.BlackPlayerName) ? "BLACK" : session.CurrentGameRecord.BlackPlayerName,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.WhitePlayerName) ? "WHITE" : session.CurrentGameRecord.WhitePlayerName,
            null, null, null, session.BlackAgehama, session.WhiteAgehama, session.CurrentTurn, minimal: true);

        if (preview is not null)
            DrawLiveBoardWipe(renderer, preview);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 548, 668, 112), "TOOL", new Color(67, 112, 118));
        controls.PlayButton.Draw(mousePoint, drawingContext);
        controls.BlackButton.Draw(mousePoint, drawingContext);
        controls.WhiteButton.Draw(mousePoint, drawingContext);
        controls.EraseButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 676, 668, 110), "HOW TO USE", new Color(86, 99, 104));
        drawingContext.DrawFittedText(
            "PLAY: LEGAL MOVES.  BLACK / WHITE / ERASE: EDIT THE POSITION DIRECTLY. THE ORIGINAL GAME IS NEVER CHANGED.",
            new Rectangle(1166, 690, 624, 78), new Color(218, 228, 226), 0.3f);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 802, 668, 74), "BOARD", new Color(76, 91, 126));
        controls.ClearButton.Draw(mousePoint, drawingContext);
        renderer.DrawRightSidePanelSelectableCommandButton(controls.BoardLensToggleBounds, "L",
            session.IsRenParseDisplayEnabled, mousePoint, true, 0.40f);
        renderer.DrawRightSidePanelCommandButton(controls.BoardLensPreviousBounds, "<J", mousePoint, session.IsRenParseDisplayEnabled, 0.25f);
        renderer.DrawRightSidePanelCommandButton(controls.BoardLensNextBounds, "K>", mousePoint, session.IsRenParseDisplayEnabled, 0.25f);
        renderer.DrawRightSidePanelCommandButton(controls.BoardLensExitBounds, "OFF/1", mousePoint, session.IsRenParseDisplayEnabled, 0.22f);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        controls.ExportSgfButton.Draw(mousePoint, drawingContext);
        controls.CommentButton.Draw(mousePoint, drawingContext);
        controls.UndoButton.Draw(mousePoint, drawingContext);
        controls.PassButton.Draw(mousePoint, drawingContext);
    }

    private static void DrawLiveBoardWipe(GoScreenRenderer renderer, LiveBoardPreview preview)
    {
        var drawingContext = renderer.StationeryDrawingContext;
        var bounds = BoardAndReviewScreen.Default.VariationEditing.LiveBoardBounds;
        drawingContext.FillRectangle(new Rectangle(bounds.X + 7, bounds.Y + 8, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        drawingContext.FillRectangle(bounds, new Color(31, 43, 45));
        drawingContext.DrawRectangle(bounds, 3, new Color(91, 218, 211));
        drawingContext.DrawText($"CURRENT  MOVE {preview.MoveCount}", new Vector2(bounds.X + 12, bounds.Y + 8), new Color(91, 218, 211), 0.28f);

        var board = new Rectangle(bounds.X + 15, bounds.Y + 38, bounds.Width - 30, bounds.Height - 53);
        drawingContext.FillRectangle(board, new Color(221, 166, 82));
        drawingContext.DrawRectangle(board, 2, new Color(83, 55, 32));
        const float margin = 12f;
        var start = new Vector2(board.X + margin, board.Y + margin);
        var usable = board.Width - margin * 2f;
        var cell = preview.BoardSize <= 1 ? usable : usable / (preview.BoardSize - 1);
        var end = start + new Vector2(cell * (preview.BoardSize - 1), cell * (preview.BoardSize - 1));

        for (var index = 0; index < preview.BoardSize; index++)
        {
            var offset = cell * index;
            drawingContext.DrawLine(new Vector2(start.X + offset, start.Y), new Vector2(start.X + offset, end.Y), 1, new Color(55, 38, 25));
            drawingContext.DrawLine(new Vector2(start.X, start.Y + offset), new Vector2(end.X, start.Y + offset), 1, new Color(55, 38, 25));
        }

        var stoneRadius = Math.Max(3f, cell * 0.38f);
        for (var y = 0; y < preview.BoardSize; y++)
        for (var x = 0; x < preview.BoardSize; x++)
        {
            var stone = preview.GetStone(x, y);
            if (stone == GoStone.Empty) continue;
            renderer.DrawRightSidePanelCircle(new Vector2(start.X + cell * x, start.Y + cell * y), stoneRadius,
                stone == GoStone.Black ? new Color(27, 31, 34) : new Color(247, 245, 237));
        }

        if (preview.LatestMove?.Point is not { } point) return;
        var center = new Vector2(start.X + cell * point.X, start.Y + cell * point.Y);
        var radius = stoneRadius + 2f;
        const int segments = 16;
        for (var index = 0; index < segments; index++)
        {
            var angleA = MathHelper.TwoPi * index / segments;
            var angleB = MathHelper.TwoPi * (index + 1) / segments;
            drawingContext.DrawLine(
                center + new Vector2(MathF.Cos(angleA), MathF.Sin(angleA)) * radius,
                center + new Vector2(MathF.Cos(angleB), MathF.Sin(angleB)) * radius,
                2, new Color(91, 218, 211));
        }
    }
}

public sealed class ReviewingRightSidePanel
{
    // ========================================
    // 機能
    // ========================================

    public int? GetStepButtonHit(Point point) =>
        ReviewMoveNavigation.GetButtonHit(point, ReviewChartPopupStepButtonBounds);

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        var controls = BoardAndReviewScreen.Default.Review;
        var drawingContext = renderer.StationeryDrawingContext;
        controls.UpdateBoardLensState(session.IsRenParseDisplayEnabled, session.IsMeasureBoardLens);
        new Headline("KIFU REVIEW", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f).Draw(drawingContext);
        if (session.HasUnsavedReviewCommentChanges)
        {
            drawingContext.DrawFittedText("COMMENTS NOT SAVED TO FILE", controls.UnsavedCommentsNoticeBounds,
                new Color(255, 205, 112), 0.26f);
        }
        controls.BackToHomeButton.Draw(mousePoint, drawingContext);
        if (session.UseKind == GoAppUseKind.LocalPlay)
            controls.UsePositionButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 204, 668, 120), "RULES", new Color(66, 104, 116));
        renderer.DrawResultRow(new Rectangle(1164, 208, 628, 52), "BOARD", $"{session.BoardSize} x {session.BoardSize}", new Color(62, 112, 105), Color.White);
        renderer.DrawResultRow(new Rectangle(1164, 264, 628, 52), "KOMI", session.Komi.ToString("0.0"), new Color(62, 112, 105), Color.White);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 336, 668, 200), "PLAYERS", new Color(76, 91, 126));
        renderer.DrawBothPlayersComponent(1144, 344, 668,
            session.ReviewBlackPlayerName, session.ReviewWhitePlayerName,
            session.ReviewBlackUsedTime, session.ReviewWhiteUsedTime, session.ReviewTimeLimit,
            session.BlackAgehama, session.WhiteAgehama, session.CurrentTurn, minimal: true,
            blackLiveElapsed: session.ReviewBlackUsedTime, whiteLiveElapsed: session.ReviewWhiteUsedTime);

        renderer.DrawRightSidePanelReviewTrendChart(session, mousePoint);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 850, 668, 142), "REVIEW", new Color(76, 91, 126));
        renderer.DrawRightSidePanelResultLabel(new Rectangle(1164, 858, 468, 36),
            $"STEP {session.ReviewMoveIndex} / {session.ReviewMoveCount}", new Color(76, 91, 126));
        controls.BoardLensNextButton.Draw(mousePoint, drawingContext);
        controls.BoardLensPreviousButton.Draw(mousePoint, drawingContext);
        controls.BoardLensExitButton.Draw(mousePoint, drawingContext);
        controls.BoardLensButton.Draw(mousePoint, drawingContext);
        ReviewMoveNavigation.Draw(renderer, session.ReviewMoveIndex, session.ReviewMoveCount,
            mousePoint, ReviewChartPopupStepButtonBounds);
        drawingContext.DrawFittedText(
            "[L] BOARD LENS    HOME/END    ARROWS: -/+1,10    PGDN/PGUP: -/+50",
            new Rectangle(1168, 950, 624, 24), new Color(147, 201, 190), 0.23f);
    }
}

public static class ReviewMoveNavigation
{
    // ========================================
    // データメンバー
    // ========================================

    internal static readonly int[] StepButtonValues =
        [int.MinValue, -50, -10, -1, 1, 10, 50, int.MaxValue];

    // ========================================
    // 機能
    // ========================================

    public static int? GetButtonHit(Point point, Func<int, Rectangle> getButtonBounds)
    {
        for (var index = 0; index < StepButtonValues.Length; index++)
            if (getButtonBounds(index).Contains(point))
                return StepButtonValues[index];
        return null;
    }

    internal static void Draw(GoScreenRenderer renderer, int currentMoveIndex, int moveCount,
        Point mousePoint, Func<int, Rectangle> getButtonBounds)
    {
        for (var index = 0; index < StepButtonValues.Length; index++)
        {
            var step = StepButtonValues[index];
            var enabled = step < 0 ? currentMoveIndex > 0 : currentMoveIndex < moveCount;
            renderer.DrawRightSidePanelCommandButton(getButtonBounds(index), FormatStep(step), mousePoint, enabled, 0.31f);
        }
    }

    private static string FormatStep(int step) => step switch
    {
        int.MinValue => "|<",
        int.MaxValue => ">|",
        > 0 => $"+{step}",
        _ => step.ToString(),
    };
}

public sealed class InitialPositionConciergeRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, InitialPositionConciergeView view, Point mousePoint) =>
        renderer.DrawInitialPositionConciergeContent(view, mousePoint);
}
