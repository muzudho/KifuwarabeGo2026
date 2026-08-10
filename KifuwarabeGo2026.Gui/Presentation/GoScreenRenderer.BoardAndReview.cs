namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// ［盤編集画面］［棋譜レビュー画面］共通
/// </summary>
public sealed partial class GoScreenRenderer
{

    public static bool GetStartReviewingButtonHit(Point point, bool enabled) =>
        enabled && StartReviewingButtonBounds.Contains(point);


    public static bool GetStartBoardEditingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartBoardEditingButtonBounds.Contains(point);


    public static bool GetBoardEditingBlackButtonHit(Point point) => BoardEditingBlackButtonBounds.Contains(point);


    public static bool GetBoardEditingWhiteButtonHit(Point point) => BoardEditingWhiteButtonBounds.Contains(point);


    public static bool GetBoardEditingEraseButtonHit(Point point) => BoardEditingEraseButtonBounds.Contains(point);


    public static bool GetBoardEditingUndoButtonHit(Point point) => BoardEditingUndoButtonBounds.Contains(point);


    public static bool GetBoardEditingRedoButtonHit(Point point) => BoardEditingRedoButtonBounds.Contains(point);

    public static bool GetBoardEditingClearButtonHit(Point point) => BoardEditingClearButtonBounds.Contains(point);


    public static bool GetBoardEditingCancelButtonHit(Point point) => BoardEditingCancelButtonBounds.Contains(point);

    public static bool GetBoardEditingAdoptButtonHit(Point point) => BoardEditingAdoptButtonBounds.Contains(point);

    public static bool GetVariationEditingDiscardButtonHit(Point point) =>
        VariationEditingDiscardButtonBounds.Contains(point);

    public static bool GetVariationEditingAdoptButtonHit(Point point) => VariationEditingAdoptButtonBounds.Contains(point);

    public static bool GetVariationEditingExportSgfButtonHit(Point point) =>
        VariationEditingExportSgfButtonBounds.Contains(point);

    public static bool GetVariationEditingPassButtonHit(Point point) => VariationEditingPassButtonBounds.Contains(point);

    public static bool GetVariationEditingUndoButtonHit(Point point) => VariationEditingUndoButtonBounds.Contains(point);

    public static bool GetVariationEditingPlayButtonHit(Point point) => VariationEditingPlayButtonBounds.Contains(point);

    public static bool GetVariationEditingBlackButtonHit(Point point) => VariationEditingBlackButtonBounds.Contains(point);

    public static bool GetVariationEditingWhiteButtonHit(Point point) => VariationEditingWhiteButtonBounds.Contains(point);

    public static bool GetVariationEditingEraseButtonHit(Point point) => VariationEditingEraseButtonBounds.Contains(point);

    public static bool GetVariationEditingClearButtonHit(Point point) => VariationEditingClearButtonBounds.Contains(point);


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


    public static bool GetReviewDoneButtonHit(Point point) => ReviewDoneButtonBounds.Contains(point);


    public static bool GetReviewBackToRestButtonHit(Point point) => ReviewBackToRestButtonBounds.Contains(point);

    public static bool GetReviewBoardLensButtonHit(Point point) => ReviewBoardLensButtonBounds.Contains(point);

    public static bool GetReviewBoardLensFamilyButtonHit(Point point, bool enabled) =>
        enabled && ReviewBoardLensFamilyButtonBounds.Contains(point);

    public static bool GetReviewBoardLensPreviousButtonHit(Point point, bool enabled) =>
        enabled && ReviewBoardLensPreviousButtonBounds.Contains(point);

    public static bool GetReviewBoardLensExitButtonHit(Point point, bool enabled) =>
        enabled && ReviewBoardLensExitButtonBounds.Contains(point);

    private void DrawBoardEditingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("BOARD EDIT", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f);
        DrawCommandButton(BoardEditingCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(BoardEditingAdoptButtonBounds, "ADOPT", false, mousePoint, scale: 0.4f);

        DrawVerticalResultSection(new Rectangle(1144, 204, 668, 76), "BOARD", new Color(66, 104, 116));
        DrawResultRow(new Rectangle(1164, 208, 628, 60), "SIZE", $"{session.BoardSize} x {session.BoardSize}", new Color(62, 112, 105), Color.White);

        DrawVerticalResultSection(new Rectangle(1144, 292, 668, 260), "EDIT", new Color(76, 91, 126));
        DrawResultLabel(new Rectangle(1164, 296, 628, 40), "STONE", new Color(76, 91, 126));
        DrawCommandButton(BoardEditingBlackButtonBounds, "BLACK", session.BoardEditingStone == GoStone.Black, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingWhiteButtonBounds, "WHITE", session.BoardEditingStone == GoStone.White, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingEraseButtonBounds, "ERASE", session.BoardEditingStone == GoStone.Empty, mousePoint, scale: 0.5f);

        DrawResultLabel(new Rectangle(1164, 414, 628, 40), "HISTORY", new Color(76, 91, 126));
        DrawCommandButton(BoardEditingUndoButtonBounds, "UNDO", false, mousePoint, enabled: session.CanUndoBoardEditing, scale: 0.5f);
        DrawCommandButton(BoardEditingRedoButtonBounds, "REDO", false, mousePoint, enabled: session.CanRedoBoardEditing, scale: 0.5f);
        DrawCommandButton(BoardEditingClearButtonBounds, "CLEAR BOARD", false, mousePoint, scale: 0.28f);

        DrawVerticalResultSection(new Rectangle(1144, 564, 668, 220), "POSITION", new Color(62, 112, 105));
        DrawStoneCountStrip(session, 584, showLeader: false, minimal: true);
        DrawCurrentStoneResultRow(new Rectangle(1164, 690, 628, 64), session);
    }

    private void DrawVariationEditingSidePanel(
        GoAppSession session,
        Point mousePoint,
        LiveBoardPreview? liveBoardPreview)
    {
        DrawText("ANALYSIS BOARD", new Vector2(1144, 136), new Color(42, 62, 68), 0.68f);
        DrawCommandButton(VariationEditingDiscardButtonBounds, "DISCARD", false, mousePoint, scale: 0.34f);
        if (session.CanAdoptVariationPosition)
            DrawCommandButton(VariationEditingAdoptButtonBounds, "ADOPT", false, mousePoint, scale: 0.34f);

        var informationWidth = liveBoardPreview is null ? 668 : 372;
        var informationRowWidth = liveBoardPreview is null ? 628 : 332;
        DrawVerticalResultSection(
            new Rectangle(1144, 204, informationWidth, 112),
            "EDITING",
            new Color(67, 112, 118));
        DrawResultRow(
            new Rectangle(1164, 210, informationRowWidth, 44),
            "SOURCE",
            session.HasVariationCustomPosition ? "CUSTOM POSITION" : $"MOVE {session.VariationSourceMoveIndex}",
            new Color(67, 112, 118),
            Color.White);
        DrawResultRow(
            new Rectangle(1164, 260, informationRowWidth, 44),
            "VARIATION",
            $"+{session.VariationMoveCount} MOVES",
            new Color(67, 112, 118),
            new Color(99, 223, 185));

        DrawVerticalResultSection(
            new Rectangle(1144, 332, informationWidth, 200),
            "POSITION",
            new Color(76, 91, 126));
        DrawBothPlayersComponent(
            1144,
            340,
            informationWidth,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.BlackPlayerName) ? "BLACK" : session.CurrentGameRecord.BlackPlayerName,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.WhitePlayerName) ? "WHITE" : session.CurrentGameRecord.WhitePlayerName,
            null,
            null,
            null,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            minimal: true);

        if (liveBoardPreview is not null)
        {
            DrawLiveBoardWipe(liveBoardPreview);
        }

        DrawVerticalResultSection(new Rectangle(1144, 548, 668, 112), "TOOL", new Color(67, 112, 118));
        DrawCommandButton(VariationEditingPlayButtonBounds, "PLAY", session.VariationEditingStone is null, mousePoint, scale: 0.42f);
        DrawCommandButton(VariationEditingBlackButtonBounds, "BLACK", session.VariationEditingStone == GoStone.Black, mousePoint, scale: 0.4f);
        DrawCommandButton(VariationEditingWhiteButtonBounds, "WHITE", session.VariationEditingStone == GoStone.White, mousePoint, scale: 0.4f);
        DrawCommandButton(VariationEditingEraseButtonBounds, "ERASE", session.VariationEditingStone == GoStone.Empty, mousePoint, scale: 0.4f);

        DrawVerticalResultSection(new Rectangle(1144, 676, 668, 110), "HOW TO USE", new Color(86, 99, 104));
        DrawFittedText(
            "PLAY: LEGAL MOVES.  BLACK / WHITE / ERASE: EDIT THE POSITION DIRECTLY. THE ORIGINAL GAME IS NEVER CHANGED.",
            new Rectangle(1166, 690, 624, 78),
            new Color(218, 228, 226),
            0.3f);

        DrawVerticalResultSection(new Rectangle(1144, 802, 668, 74), "BOARD", new Color(76, 91, 126));
        DrawCommandButton(VariationEditingClearButtonBounds, "CLEAR BOARD", false, mousePoint, scale: 0.32f);

        DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        DrawCommandButton(VariationEditingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.29f);
        DrawCommandButton(
            VariationEditingUndoButtonBounds,
            "UNDO",
            false,
            mousePoint,
            enabled: session.CanUndoVariation,
            scale: 0.42f);
        DrawCommandButton(VariationEditingPassButtonBounds, "PASS", false, mousePoint, scale: 0.44f);
    }


    private void DrawReviewingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("KIFU REVIEW", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f);
        DrawCommandButton(ReviewBackToRestButtonBounds, "BACK TO HOME", false, mousePoint, scale: 0.32f);
        if (session.UseKind == GoAppUseKind.LocalPlay)
        {
            DrawCommandButton(ReviewDoneButtonBounds, "USE POSITION", false, mousePoint, scale: 0.34f);
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
            null,
            null,
            null,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            minimal: true);

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



    private static Rectangle StartReviewingButtonBounds => new(1315, 920, 154, 56);


    private static Rectangle StartBoardEditingButtonBounds => new(1486, 920, 154, 56);


    private static Rectangle BoardEditingBlackButtonBounds => new(GameOverValueX, 340, 140, 56);


    private static Rectangle BoardEditingWhiteButtonBounds => new(GameOverValueX + 156, 340, 140, 56);


    private static Rectangle BoardEditingEraseButtonBounds => new(GameOverValueX + 312, 340, 140, 56);


    private static Rectangle BoardEditingUndoButtonBounds => new(GameOverValueX, 458, 140, 56);


    private static Rectangle BoardEditingRedoButtonBounds => new(GameOverValueX + 156, 458, 140, 56);

    private static Rectangle BoardEditingClearButtonBounds => new(GameOverValueX + 312, 458, 140, 56);


    private static Rectangle BoardEditingCancelButtonBounds => new(1480, 120, 156, 52);

    private static Rectangle BoardEditingAdoptButtonBounds => new(1648, 120, 164, 52);

    private static Rectangle VariationEditingDiscardButtonBounds => new(1684, 120, 128, 52);
    private static Rectangle VariationEditingLiveBoardBounds => new(1540, 188, 252, 252);
    private static Rectangle VariationEditingAdoptButtonBounds => new(1396, 120, 128, 52);
    private static Rectangle VariationEditingExportSgfButtonBounds => new(1164, 924, 196, 56);
    private static Rectangle VariationEditingUndoButtonBounds => new(1374, 924, 196, 56);
    private static Rectangle VariationEditingPassButtonBounds => new(1584, 924, 196, 56);
    private static Rectangle VariationEditingPlayButtonBounds => new(1164, 584, 140, 56);
    private static Rectangle VariationEditingBlackButtonBounds => new(1320, 584, 140, 56);
    private static Rectangle VariationEditingWhiteButtonBounds => new(1476, 584, 140, 56);
    private static Rectangle VariationEditingEraseButtonBounds => new(1632, 584, 140, 56);
    private static Rectangle VariationEditingClearButtonBounds => new(1164, 810, 608, 52);

    private void DrawLiveBoardWipe(LiveBoardPreview preview)
    {
        var bounds = VariationEditingLiveBoardBounds;
        FillRect(new Rectangle(bounds.X + 7, bounds.Y + 8, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, new Color(31, 43, 45));
        DrawRect(bounds, 3, new Color(91, 218, 211));
        DrawText(
            $"CURRENT  MOVE {preview.MoveCount}",
            new Vector2(bounds.X + 12, bounds.Y + 8),
            new Color(91, 218, 211),
            0.28f);

        var board = new Rectangle(bounds.X + 15, bounds.Y + 38, bounds.Width - 30, bounds.Height - 53);
        FillRect(board, new Color(221, 166, 82));
        DrawRect(board, 2, new Color(83, 55, 32));
        var margin = 12f;
        var start = new Vector2(board.X + margin, board.Y + margin);
        var usable = board.Width - margin * 2f;
        var cell = preview.BoardSize <= 1 ? usable : usable / (preview.BoardSize - 1);
        var end = start + new Vector2(cell * (preview.BoardSize - 1), cell * (preview.BoardSize - 1));

        for (var index = 0; index < preview.BoardSize; index++)
        {
            var offset = cell * index;
            DrawLine(
                new Vector2(start.X + offset, start.Y),
                new Vector2(start.X + offset, end.Y),
                1,
                new Color(55, 38, 25));
            DrawLine(
                new Vector2(start.X, start.Y + offset),
                new Vector2(end.X, start.Y + offset),
                1,
                new Color(55, 38, 25));
        }

        var stoneRadius = Math.Max(3f, cell * 0.38f);
        for (var y = 0; y < preview.BoardSize; y++)
        {
            for (var x = 0; x < preview.BoardSize; x++)
            {
                var stone = preview.GetStone(x, y);
                if (stone == GoStone.Empty)
                    continue;

                var center = new Vector2(start.X + cell * x, start.Y + cell * y);
                DrawCircle(
                    center,
                    stoneRadius,
                    stone == GoStone.Black ? new Color(27, 31, 34) : new Color(247, 245, 237));
            }
        }

        if (preview.LatestMove?.Point is { } point)
        {
            var center = new Vector2(start.X + cell * point.X, start.Y + cell * point.Y);
            var radius = stoneRadius + 2f;
            const int segments = 16;
            for (var index = 0; index < segments; index++)
            {
                var angleA = MathHelper.TwoPi * index / segments;
                var angleB = MathHelper.TwoPi * (index + 1) / segments;
                DrawLine(
                    center + new Vector2(MathF.Cos(angleA), MathF.Sin(angleA)) * radius,
                    center + new Vector2(MathF.Cos(angleB), MathF.Sin(angleB)) * radius,
                    2,
                    new Color(91, 218, 211));
            }
        }
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


    private static Rectangle ReviewAnalysisSectionBounds => new(1144, 692, 668, 146);


    private static Rectangle ReviewAnalysisTooltipBounds => new(1164, 734, 628, 104);


    private static Rectangle ReviewDoneButtonBounds => new(1648, 120, 164, 52);


    private static Rectangle ReviewBackToRestButtonBounds => new(1480, 120, 156, 52);


    private static Rectangle ReviewBoardLensButtonBounds => new(1508, 858, 60, 60);

    private static Rectangle ReviewBoardLensFamilyButtonBounds => new(1652, 858, 60, 60);

    private static Rectangle ReviewBoardLensExitButtonBounds => new(1724, 858, 60, 60);

    private static Rectangle ReviewBoardLensPreviousButtonBounds => new(1580, 858, 60, 60);


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

