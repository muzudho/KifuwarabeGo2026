namespace KifuwarabeGo2026.Gui.Presentation;

using static KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.PopupTrendChartScreenBounds;

using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;
using static KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview.BoardAndReviewScreenBounds;

/// <summary>
/// ［盤編集画面］［棋譜レビュー画面］共通
/// </summary>
public sealed partial class GoScreenRenderer
{

    public static int? GetReviewStepButtonHit(Point point)
    {
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            if (ReviewChartPopupStepButtonBounds(i).Contains(point))
            {
                return ReviewStepButtonValues[i];
            }
        }

        return null;
    }


    internal void DrawReviewingRightSidePanelContent(GoAppSession session, Point mousePoint)
    {
        var controls = BoardAndReviewScreen.Default.Review;
        controls.UpdateBoardLensState(
            session.IsRenParseDisplayEnabled,
            session.IsMeasureBoardLens);
        new Headline("KIFU REVIEW", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f).Draw(_stationeryDrawingContext);
        if (session.HasUnsavedReviewCommentChanges)
        {
            DrawFittedText(
                "COMMENTS NOT SAVED TO FILE",
                ReviewUnsavedCommentsNoticeBounds,
                new Color(255, 205, 112),
                0.26f);
        }
        controls.BackToHomeButton.Draw(mousePoint, _stationeryDrawingContext);
        if (session.UseKind == GoAppUseKind.LocalPlay)
        {
            controls.UsePositionButton.Draw(mousePoint, _stationeryDrawingContext);
        }

        DrawVerticalResultSection(new Rectangle(1144, 204, 668, 120), "RULES", new Color(66, 104, 116));
        DrawResultRow(new Rectangle(1164, 208, 628, 52), "BOARD", $"{session.BoardSize} x {session.BoardSize}", new Color(62, 112, 105), Color.White);
        DrawResultRow(new Rectangle(1164, 264, 628, 52), "KOMI", FormatKomi(session.Komi), new Color(62, 112, 105), Color.White);

        DrawVerticalResultSection(new Rectangle(1144, 336, 668, 200), "PLAYERS", new Color(76, 91, 126));
        DrawBothPlayersComponent(
            1144,
            344,
            668,
            session.ReviewBlackPlayerName,
            session.ReviewWhitePlayerName,
            session.ReviewBlackUsedTime,
            session.ReviewWhiteUsedTime,
            session.ReviewTimeLimit,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            minimal: true,
            blackLiveElapsed: session.ReviewBlackUsedTime,
            whiteLiveElapsed: session.ReviewWhiteUsedTime);

        DrawReviewTrendChart(session, mousePoint);

        DrawVerticalResultSection(new Rectangle(1144, 850, 668, 142), "REVIEW", new Color(76, 91, 126));
        DrawResultLabel(
            new Rectangle(1164, 858, 468, 36),
            $"STEP {session.ReviewMoveIndex} / {session.ReviewMoveCount}",
            new Color(76, 91, 126));
        DrawReviewBoardLensFamilyButton(
            session.IsRenParseDisplayEnabled,
            session.IsMeasureBoardLens,
            mousePoint);
        DrawReviewBoardLensPreviousButton(session.IsRenParseDisplayEnabled, mousePoint);
        DrawReviewBoardLensExitButton(session.IsRenParseDisplayEnabled, mousePoint);
        DrawReviewBoardLensButton(session.IsRenParseDisplayEnabled, mousePoint);
        DrawMoveNavigationButtons(
            session.ReviewMoveIndex,
            session.ReviewMoveCount,
            mousePoint,
            ReviewChartPopupStepButtonBounds);
        DrawFittedText("[L] BOARD LENS    HOME/END    ARROWS: -/+1,10    PGDN/PGUP: -/+50", new Rectangle(1168, 950, 624, 24), new Color(147, 201, 190), 0.23f);

    }



    private static readonly int[] ReviewStepButtonValues =
        [int.MinValue, -50, -10, -1, 1, 10, 50, int.MaxValue];

    private void DrawMoveNavigationButtons(
        int currentMoveIndex,
        int moveCount,
        Point mousePoint,
        Func<int, Rectangle> getButtonBounds)
    {
        for (var index = 0; index < ReviewStepButtonValues.Length; index++)
        {
            var step = ReviewStepButtonValues[index];
            var enabled = step < 0
                ? currentMoveIndex > 0
                : currentMoveIndex < moveCount;
            DrawCommandButton(
                getButtonBounds(index),
                FormatMoveNavigationStep(step),
                false,
                mousePoint,
                enabled,
                0.31f);
        }
    }

    private static string FormatMoveNavigationStep(int step) => step switch
    {
        int.MinValue => "|<",
        int.MaxValue => ">|",
        > 0 => $"+{step}",
        _ => step.ToString(),
    };


    private void DrawReviewBoardLensPreviousButton(bool enabled, Point mousePoint)
    {
        var bounds = ReviewBoardLensPreviousButtonBounds;
        var hovered = enabled && bounds.Contains(mousePoint);
        FillRect(bounds, !enabled ? new Color(24, 27, 31) : hovered ? new Color(50, 75, 86) : new Color(32, 44, 53));
        DrawRect(bounds, 2, !enabled ? new Color(43, 50, 56) : new Color(111, 137, 150));
        var color = enabled ? new Color(220, 234, 237) : new Color(91, 100, 106);
        DrawCenteredText("<J", new Vector2(bounds.Center.X, bounds.Y + 30), color, 0.25f);
    }


    private void DrawReviewBoardLensExitButton(bool enabled, Point mousePoint)
    {
        var bounds = ReviewBoardLensExitButtonBounds;
        var hovered = enabled && bounds.Contains(mousePoint);
        var fill = !enabled
            ? new Color(24, 27, 31)
            : hovered
                ? new Color(104, 56, 56)
                : new Color(67, 39, 43);
        var border = !enabled
            ? new Color(43, 50, 56)
            : hovered
                ? new Color(255, 196, 186)
                : new Color(192, 119, 119);
        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, enabled ? 95 : 28));
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);

        var textColor = enabled ? new Color(255, 226, 220) : new Color(91, 100, 106);
        DrawCenteredText("OFF", new Vector2(bounds.Center.X, bounds.Y + 23), textColor, 0.22f);
        DrawCenteredText("1", new Vector2(bounds.Center.X, bounds.Y + 44), enabled ? new Color(255, 220, 128) : textColor, 0.19f);

        if (hovered)
        {
            var tooltip = new Rectangle(bounds.Right - 208, bounds.Y - 38, 208, 32);
            FillRect(tooltip, new Color(15, 24, 30, 245));
            DrawRect(tooltip, 1, new Color(255, 180, 170));
            DrawFittedText("EXIT BOARD LENS  [1]", new Rectangle(tooltip.X + 10, tooltip.Y + 4, tooltip.Width - 20, tooltip.Height - 8), Color.White, 0.25f);
        }
    }


    private void DrawReviewBoardLensFamilyButton(bool enabled, bool selected, Point mousePoint)
    {
        var bounds = ReviewBoardLensFamilyButtonBounds;
        var hovered = enabled && bounds.Contains(mousePoint);
        var fill = !enabled
            ? new Color(24, 27, 31)
            : selected
                ? new Color(26, 91, 99)
                : hovered
                    ? new Color(50, 75, 86)
                    : new Color(32, 44, 53);
        var border = !enabled
            ? new Color(43, 50, 56)
            : selected
                ? new Color(125, 225, 255)
                : hovered
                    ? new Color(178, 219, 226)
                    : new Color(111, 137, 150);
        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, enabled ? 95 : 28));
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);

        var iconColor = enabled
            ? selected ? new Color(151, 255, 215) : new Color(220, 234, 237)
            : new Color(91, 100, 106);
        DrawCenteredText("K>", new Vector2(bounds.Center.X, bounds.Y + 30), iconColor, 0.25f);

        if (hovered)
        {
            var tooltip = new Rectangle(bounds.Right - 276, bounds.Y - 38, 276, 32);
            FillRect(tooltip, new Color(15, 24, 30, 245));
            DrawRect(tooltip, 1, new Color(125, 225, 255));
            DrawFittedText("NEXT LENS  [K]", new Rectangle(tooltip.X + 10, tooltip.Y + 4, tooltip.Width - 20, tooltip.Height - 8), Color.White, 0.25f);
        }
    }


    private void DrawReviewBoardLensButton(bool selected, Point mousePoint)
    {
        var bounds = ReviewBoardLensButtonBounds;
        var hovered = bounds.Contains(mousePoint);
        var fill = selected
            ? new Color(26, 91, 99)
            : hovered
                ? new Color(50, 75, 86)
                : new Color(32, 44, 53);
        var border = selected
            ? new Color(125, 225, 255)
            : hovered
                ? new Color(178, 219, 226)
                : new Color(111, 137, 150);
        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);

        var iconColor = selected ? new Color(151, 255, 215) : new Color(220, 234, 237);
        var a = new Vector2(bounds.X + 19, bounds.Y + 20);
        var b = new Vector2(bounds.X + 39, bounds.Y + 20);
        var c = new Vector2(bounds.X + 39, bounds.Y + 40);
        DrawLine(a, b, 4f, iconColor);
        DrawLine(b, c, 4f, iconColor);
        DrawCircle(a, 7f, iconColor);
        DrawCircle(b, 7f, iconColor);
        DrawCircle(c, 7f, iconColor);
        DrawCenteredText("L", new Vector2(bounds.X + 14, bounds.Y + 46), new Color(255, 220, 128), 0.16f);

        if (hovered)
        {
            var tooltip = new Rectangle(bounds.Right - 238, bounds.Y - 38, 238, 32);
            FillRect(tooltip, new Color(15, 24, 30, 245));
            DrawRect(tooltip, 1, new Color(125, 225, 255));
            DrawFittedText("BOARD LENS  [L]", new Rectangle(tooltip.X + 10, tooltip.Y + 4, tooltip.Width - 20, tooltip.Height - 8), Color.White, 0.25f);
        }
    }


    private void DrawBoardEditingHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
        if (!TryGetBoardIntersection(mousePoint, session.BoardSize, out var intersection))
        {
            return;
        }

        var editingStone = session.CurrentMode.Kind == GoAppModeKind.VariationEditing
            ? session.VariationEditingStone ?? GoStone.Black
            : session.BoardEditingStone;
        var layout = GetBoardLayout(session.BoardSize);
        var center = BoardPoint(layout.Start, layout.Cell, intersection.X, intersection.Y);
        if (editingStone == GoStone.Empty)
        {
            var radius = cell * 0.32f;
            DrawLine(new Vector2(center.X - radius, center.Y - radius), new Vector2(center.X + radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            DrawLine(new Vector2(center.X + radius, center.Y - radius), new Vector2(center.X - radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            return;
        }

        var black = editingStone == GoStone.Black;
        DrawCircle(center, cell * 0.55f, black ? new Color(8, 10, 14, 105) : new Color(255, 250, 232, 120));
        DrawCircle(center, cell * 0.36f, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 105));
    }
}

