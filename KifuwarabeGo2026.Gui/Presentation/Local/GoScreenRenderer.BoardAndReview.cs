namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

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


    public static bool GetBoardEditingExportSgfButtonHit(Point point) => BoardEditingExportSgfButtonBounds.Contains(point);


    public static bool GetBoardEditingDoneButtonHit(Point point) => BoardEditingDoneButtonBounds.Contains(point);


    public static int? GetReviewStepButtonHit(Point point)
    {
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            if (ReviewStepButtonBounds(i).Contains(point))
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
        DrawCommandButton(BoardEditingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.32f);
        DrawCommandButton(BoardEditingDoneButtonBounds, "DONE", false, mousePoint, scale: 0.4f);

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

        DrawVerticalResultSection(new Rectangle(1144, 564, 668, 220), "POSITION", new Color(62, 112, 105));
        DrawStoneCountStrip(session, 584, showLeader: false, minimal: true);
        DrawCurrentStoneResultRow(new Rectangle(1164, 690, 628, 64), session);
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

        DrawVerticalResultSection(new Rectangle(1144, 548, 668, 132), "CALCULATION", new Color(76, 91, 126));
        DrawStoneCountStrip(session, 560, showLeader: true, minimal: true);

        DrawMoveAnalysisSection(session.ReviewCurrentMove, ReviewAnalysisSectionBounds);

        DrawVerticalResultSection(new Rectangle(1144, 850, 668, 142), "REVIEW", new Color(76, 91, 126));
        DrawResultLabel(
            new Rectangle(1164, 858, 628, 36),
            $"STEP {session.ReviewMoveIndex} / {session.ReviewMoveCount}   DISPLAY {FormatRenParseDisplayMode(session.RenParseDisplayMode)}",
            new Color(76, 91, 126));
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            var step = ReviewStepButtonValues[i];
            var enabled = step < 0 ? session.ReviewMoveIndex > 0 : session.ReviewMoveIndex < session.ReviewMoveCount;
            DrawCommandButton(ReviewStepButtonBounds(i), step > 0 ? $"+{step}" : step.ToString(), false, mousePoint, enabled, 0.34f);
        }
        DrawFittedText("KEYS  LEFT/RIGHT: -/+1   DOWN/UP: -/+10   PGDN/PGUP: -/+50   R: REN ANALYSIS", new Rectangle(1268, 950, 524, 24), new Color(147, 201, 190), 0.25f);

        DrawMoveAnalysisTooltip(session.ReviewCurrentMove, ReviewAnalysisSectionBounds, mousePoint, ReviewAnalysisTooltipBounds);

    }



    private static Rectangle StartReviewingButtonBounds => new(1315, 920, 154, 56);


    private static Rectangle StartBoardEditingButtonBounds => new(1486, 920, 154, 56);


    private static Rectangle BoardEditingBlackButtonBounds => new(GameOverValueX, 340, 140, 56);


    private static Rectangle BoardEditingWhiteButtonBounds => new(GameOverValueX + 156, 340, 140, 56);


    private static Rectangle BoardEditingEraseButtonBounds => new(GameOverValueX + 312, 340, 140, 56);


    private static Rectangle BoardEditingUndoButtonBounds => new(GameOverValueX, 458, 220, 56);


    private static Rectangle BoardEditingRedoButtonBounds => new(GameOverValueX + 244, 458, 220, 56);


    private static Rectangle BoardEditingExportSgfButtonBounds => new(1480, 120, 156, 52);


    private static Rectangle BoardEditingDoneButtonBounds => new(1648, 120, 164, 52);


    private static readonly int[] ReviewStepButtonValues = [-50, -10, -1, 1, 10, 50];


    private static Rectangle ReviewStepButtonBounds(int index) => new(1268 + index * 87, 898, 78, 44);


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

        var layout = GetBoardLayout(session.BoardSize);
        var center = BoardPoint(layout.Start, layout.Cell, intersection.X, intersection.Y);
        if (session.BoardEditingStone == GoStone.Empty)
        {
            var radius = cell * 0.32f;
            DrawLine(new Vector2(center.X - radius, center.Y - radius), new Vector2(center.X + radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            DrawLine(new Vector2(center.X + radius, center.Y - radius), new Vector2(center.X - radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            return;
        }

        var black = session.BoardEditingStone == GoStone.Black;
        DrawCircle(center, cell * 0.55f, black ? new Color(8, 10, 14, 105) : new Color(255, 250, 232, 120));
        DrawCircle(center, cell * 0.36f, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 105));
    }
}

