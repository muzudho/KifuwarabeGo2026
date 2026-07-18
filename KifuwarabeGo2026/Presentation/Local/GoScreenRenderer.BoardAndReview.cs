namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
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


    private void DrawBoardEditingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("BOARD EDIT", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawInfoStrip(1144, 204, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 276, "BLACK", session.BlackStoneCount.ToString());
        DrawInfoStrip(1144, 348, "WHITE", session.WhiteStoneCount.ToString());

        DrawText("STONE", new Vector2(1144, 454), new Color(180, 195, 195), 0.56f);
        DrawCommandButton(BoardEditingBlackButtonBounds, "BLACK", session.BoardEditingStone == GoStone.Black, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingWhiteButtonBounds, "WHITE", session.BoardEditingStone == GoStone.White, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingEraseButtonBounds, "ERASE", session.BoardEditingStone == GoStone.Empty, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingUndoButtonBounds, "UNDO", false, mousePoint, enabled: session.CanUndoBoardEditing, scale: 0.5f);
        DrawCommandButton(BoardEditingRedoButtonBounds, "REDO", false, mousePoint, enabled: session.CanRedoBoardEditing, scale: 0.5f);

        DrawText("CURRENT POSITION", new Vector2(1144, 636), new Color(180, 195, 195), 0.52f);
        DrawStoneCountStrip(session, 676);
        DrawCommandButton(BoardEditingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.52f);
        DrawCommandButton(BoardEditingDoneButtonBounds, "DONE", false, mousePoint);
    }


    private void DrawReviewingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("KIFU REVIEW", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawInfoStrip(1144, 204, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 276, "MOVE", $"{session.ReviewMoveIndex} / {session.ReviewMoveCount}");
        DrawInfoStrip(1144, 348, "TURN", session.CurrentTurn == GoStone.Black ? "BLACK" : "WHITE");

        DrawText("STEP", new Vector2(1144, 454), new Color(180, 195, 195), 0.56f);
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            var step = ReviewStepButtonValues[i];
            var enabled = step < 0 ? session.ReviewMoveIndex > 0 : session.ReviewMoveIndex < session.ReviewMoveCount;
            DrawCommandButton(ReviewStepButtonBounds(i), step > 0 ? $"+{step}" : step.ToString(), false, mousePoint, enabled, 0.42f);
        }

        DrawText("Push R key:", new Vector2(1144, 636), new Color(180, 195, 195), 0.46f);
        DrawReviewRenParseModeStrip(session, mousePoint);

        DrawText("CURRENT POSITION", new Vector2(1144, 760), new Color(180, 195, 195), 0.52f);
        DrawStoneCountStrip(session, 800);
        DrawCommandButton(ReviewDoneButtonBounds, "USE POSITION", false, mousePoint, scale: 0.52f);
    }


    private static Rectangle StartReviewingButtonBounds => new(1315, 920, 154, 56);


    private static Rectangle StartBoardEditingButtonBounds => new(1486, 920, 154, 56);


    private static Rectangle BoardEditingBlackButtonBounds => new(1144, 506, 204, 62);


    private static Rectangle BoardEditingWhiteButtonBounds => new(1376, 506, 204, 62);


    private static Rectangle BoardEditingEraseButtonBounds => new(1608, 506, 204, 62);


    private static Rectangle BoardEditingUndoButtonBounds => new(1144, 588, 320, 56);


    private static Rectangle BoardEditingRedoButtonBounds => new(1492, 588, 320, 56);


    private static Rectangle BoardEditingExportSgfButtonBounds => new(1144, 920, 320, 56);


    private static Rectangle BoardEditingDoneButtonBounds => new(1492, 920, 320, 56);


    private static readonly int[] ReviewStepButtonValues = [-50, -10, -1, 1, 10, 50];


    private static Rectangle ReviewStepButtonBounds(int index) => new(1144 + index % 3 * 232, 504 + index / 3 * 64, 160, 46);


    private static readonly RenParseDisplayMode[] ReviewRenParseDisplayModes =
    [
        RenParseDisplayMode.Overlay,
        RenParseDisplayMode.Graph,
        RenParseDisplayMode.GraphStep2,
        RenParseDisplayMode.Eye,
    ];


    private static readonly string[] ReviewRenParseDisplayModeLabels =
    [
        "Ren Number",
        "Ren Rect",
        "Ren Graph",
        "Eye",
    ];


    private static Rectangle ReviewRenParseDisplayModeBounds(int index) => new(1144 + index * 166, 684, 150, 46);


    private static Rectangle ReviewDoneButtonBounds => new(1492, 920, 320, 56);


    private void DrawReviewRenParseModeStrip(GoAppSession session, Point mousePoint)
    {
        for (var i = 0; i < ReviewRenParseDisplayModes.Length; i++)
        {
            DrawCommandButton(
                ReviewRenParseDisplayModeBounds(i),
                ReviewRenParseDisplayModeLabels[i],
                session.RenParseDisplayMode == ReviewRenParseDisplayModes[i],
                mousePoint,
                enabled: true,
                scale: 0.28f);
        }
    }


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

