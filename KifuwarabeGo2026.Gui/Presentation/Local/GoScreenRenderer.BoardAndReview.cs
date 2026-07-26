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

    public static bool GetVariationEditingDiscardButtonHit(Point point) => VariationEditingDiscardButtonBounds.Contains(point);

    public static bool GetVariationEditingExportSgfButtonHit(Point point) => VariationEditingExportSgfButtonBounds.Contains(point);

    public static bool GetVariationEditingPassButtonHit(Point point) => VariationEditingPassButtonBounds.Contains(point);

    public static bool GetVariationEditingUndoButtonHit(Point point) => VariationEditingUndoButtonBounds.Contains(point);

    public static bool GetVariationEditingPlayButtonHit(Point point) => VariationEditingPlayButtonBounds.Contains(point);

    public static bool GetVariationEditingBlackButtonHit(Point point) => VariationEditingBlackButtonBounds.Contains(point);

    public static bool GetVariationEditingWhiteButtonHit(Point point) => VariationEditingWhiteButtonBounds.Contains(point);

    public static bool GetVariationEditingEraseButtonHit(Point point) => VariationEditingEraseButtonBounds.Contains(point);


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

    private void DrawVariationEditingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("ANALYSIS BOARD", new Vector2(1144, 136), new Color(42, 62, 68), 0.68f);
        DrawCommandButton(VariationEditingDiscardButtonBounds, "DISCARD", false, mousePoint, scale: 0.34f);
        DrawCommandButton(VariationEditingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.29f);

        DrawVerticalResultSection(new Rectangle(1144, 204, 668, 112), "EDITING", new Color(67, 112, 118));
        DrawResultRow(
            new Rectangle(1164, 210, 628, 44),
            "SOURCE",
            session.HasVariationCustomPosition ? "CUSTOM POSITION" : $"MOVE {session.VariationSourceMoveIndex}",
            new Color(67, 112, 118),
            Color.White);
        DrawResultRow(
            new Rectangle(1164, 260, 628, 44),
            "VARIATION",
            $"+{session.VariationMoveCount} MOVES",
            new Color(67, 112, 118),
            new Color(99, 223, 185));

        DrawVerticalResultSection(new Rectangle(1144, 332, 668, 200), "POSITION", new Color(76, 91, 126));
        DrawBothPlayersComponent(
            1144,
            340,
            668,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.BlackPlayerName) ? "BLACK" : session.CurrentGameRecord.BlackPlayerName,
            string.IsNullOrWhiteSpace(session.CurrentGameRecord.WhitePlayerName) ? "WHITE" : session.CurrentGameRecord.WhitePlayerName,
            null,
            null,
            null,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            minimal: true);

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

        DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
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
        if (session.UseKind == GoAppUseKind.LocalGame)
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
            new Rectangle(1164, 858, 628, 36),
            $"STEP {session.ReviewMoveIndex} / {session.ReviewMoveCount}   DISPLAY {FormatRenParseDisplayMode(session.RenParseDisplayMode)}",
            new Color(76, 91, 126));
        DrawMoveNavigationButtons(
            session.ReviewMoveIndex,
            session.ReviewMoveCount,
            mousePoint,
            ReviewChartPopupStepButtonBounds);
        DrawFittedText("KEYS  HOME/END: FIRST/LAST   ARROWS: -/+1,10   PGDN/PGUP: -/+50", new Rectangle(1168, 950, 624, 24), new Color(147, 201, 190), 0.23f);

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

    private static Rectangle VariationEditingDiscardButtonBounds => new(1448, 120, 176, 52);
    private static Rectangle VariationEditingExportSgfButtonBounds => new(1636, 120, 176, 52);
    private static Rectangle VariationEditingUndoButtonBounds => new(1164, 924, 306, 56);
    private static Rectangle VariationEditingPassButtonBounds => new(1486, 924, 306, 56);
    private static Rectangle VariationEditingPlayButtonBounds => new(1164, 584, 140, 56);
    private static Rectangle VariationEditingBlackButtonBounds => new(1320, 584, 140, 56);
    private static Rectangle VariationEditingWhiteButtonBounds => new(1476, 584, 140, 56);
    private static Rectangle VariationEditingEraseButtonBounds => new(1632, 584, 140, 56);


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


    private static string FormatRenParseDisplayMode(RenParseDisplayMode mode) => mode switch
    {
        RenParseDisplayMode.Off => "OFF",
        RenParseDisplayMode.Overlay => "REN NUMBER",
        RenParseDisplayMode.Graph => "REN RECT",
        RenParseDisplayMode.GraphStep2 => "REN GRAPH",
        RenParseDisplayMode.Eye => "EYE",
        _ => mode.ToString().ToUpperInvariant(),
    };


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

