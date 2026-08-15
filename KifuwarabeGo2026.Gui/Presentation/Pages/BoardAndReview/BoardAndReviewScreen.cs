namespace KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

/// <summary>盤編集・変化図編集・棋譜検討画面のUIを所有します。</summary>
public sealed class BoardAndReviewScreen
{
    public static BoardAndReviewScreen Default { get; } = new();

    private BoardAndReviewScreen()
    {
        StartReviewingButton = new Button(new Rectangle(1315, 920, 154, 56), "KIFU REVIEW", 0.32f);
        StartBoardEditingButton = new Button(new Rectangle(1486, 920, 154, 56), "EDIT BOARD", 0.36f);
        BoardEditing = new BoardEditingControls();
        VariationEditing = new VariationEditingControls();
        Review = new ReviewControls();
    }

    public Button StartReviewingButton { get; }
    public Button StartBoardEditingButton { get; }
    public BoardEditingControls BoardEditing { get; }
    public VariationEditingControls VariationEditing { get; }
    public ReviewControls Review { get; }
    public BoardEditingRightSidePanel BoardEditingRightSidePanel { get; } = new();
    public VariationEditingRightSidePanel VariationEditingRightSidePanel { get; } = new();
    public ReviewingRightSidePanel ReviewingRightSidePanel { get; } = new();

    public void DrawEditingHoverStone(
        BoardLensModel boardLensModel,
        GoAppSession session,
        Point intersection,
        Vector2 start,
        float cell)
    {
        var editingStone = session.CurrentMode.Kind == GoAppModeKind.VariationEditing
            ? session.VariationEditingStone ?? GoStone.Black
            : session.BoardEditingStone;
        var center = boardLensModel.GetBoardPoint(start, cell, intersection.X, intersection.Y);
        if (editingStone == GoStone.Empty)
        {
            var radius = cell * 0.32f;
            boardLensModel.DrawLine(
                new Vector2(center.X - radius, center.Y - radius),
                new Vector2(center.X + radius, center.Y + radius),
                6,
                new Color(180, 42, 42, 205));
            boardLensModel.DrawLine(
                new Vector2(center.X + radius, center.Y - radius),
                new Vector2(center.X - radius, center.Y + radius),
                6,
                new Color(180, 42, 42, 205));
            return;
        }

        var black = editingStone == GoStone.Black;
        boardLensModel.DrawCircle(
            center,
            cell * 0.55f,
            black ? new Color(8, 10, 14, 105) : new Color(255, 250, 232, 120));
        boardLensModel.DrawCircle(
            center,
            cell * 0.36f,
            black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 105));
    }
}

/// <summary>棋譜検討画面の操作と表示領域を所有します。</summary>
public sealed class ReviewControls
{
    public ReviewControls()
    {
        UsePositionButton = new Button(new Rectangle(1480, 120, 156, 52), "USE POSITION", 0.30f);
        BackToHomeButton = new Button(new Rectangle(1648, 120, 164, 52), "BACK TO HOME", 0.29f);
        ExportSgfButton = new Button(new Rectangle(1480, 120, 156, 52), "SGF OUTPUT", 0.26f);
        BoardLensButton = new Button(new Rectangle(1508, 858, 60, 60), "L", 0.16f);
        BoardLensNextButton = new Button(new Rectangle(1652, 858, 60, 60), "K>", 0.25f);
        BoardLensExitButton = new Button(new Rectangle(1724, 858, 60, 60), "OFF/1", 0.19f);
        BoardLensPreviousButton = new Button(new Rectangle(1580, 858, 60, 60), "<J", 0.25f);
        BoardLensNextButton.IsEnabled = false;
        BoardLensExitButton.IsEnabled = false;
        BoardLensPreviousButton.IsEnabled = false;
    }

    public Rectangle AnalysisSectionBounds { get; } = new(1144, 692, 668, 146);
    public Rectangle AnalysisTooltipBounds { get; } = new(1164, 734, 628, 104);
    public Rectangle UnsavedCommentsNoticeBounds { get; } = new(1334, 165, 430, 24);
    public Button UsePositionButton { get; }
    public Button BackToHomeButton { get; }
    public Button ExportSgfButton { get; }
    public Button BoardLensButton { get; }
    public Button BoardLensNextButton { get; }
    public Button BoardLensExitButton { get; }
    public Button BoardLensPreviousButton { get; }

    public void UpdateBoardLensState(bool enabled, bool isMeasureLens)
    {
        BoardLensButton.IsSelected = enabled;
        BoardLensNextButton.IsEnabled = enabled;
        BoardLensNextButton.IsSelected = isMeasureLens;
        BoardLensPreviousButton.IsEnabled = enabled;
        BoardLensExitButton.IsEnabled = enabled;
    }
}

/// <summary>変化図編集画面の操作、プレビュー領域、Board Lens操作列を所有します。</summary>
public sealed class VariationEditingControls
{
    public VariationEditingControls()
    {
        DiscardButton = new Button(new Rectangle(1480, 120, 156, 52), "DISCARD", 0.34f);
        AdoptButton = new Button(new Rectangle(1648, 120, 164, 52), "ADOPT", 0.34f);
        ExportSgfButton = new Button(new Rectangle(1164, 924, 145, 56), "KIFU OUTPUT (SGF)", 0.20f);
        CommentButton = new Button(new Rectangle(1321, 924, 145, 56), "COMMENT", 0.30f);
        UndoButton = new Button(new Rectangle(1478, 924, 145, 56), "UNDO", 0.42f);
        PassButton = new Button(new Rectangle(1635, 924, 145, 56), "PASS", 0.44f);
        PlayButton = new Button(new Rectangle(1164, 584, 140, 56), "PLAY", 0.42f);
        BlackButton = new Button(new Rectangle(1320, 584, 140, 56), "BLACK", 0.40f);
        WhiteButton = new Button(new Rectangle(1476, 584, 140, 56), "WHITE", 0.40f);
        EraseButton = new Button(new Rectangle(1632, 584, 140, 56), "ERASE", 0.40f);
        ClearButton = new Button(new Rectangle(1164, 810, 352, 52), "CLEAR BOARD", 0.32f);
    }

    public Rectangle LiveBoardBounds { get; } = new(1540, 188, 252, 252);
    private BoardLensButtonStrip BoardLensButtons { get; } = new(1532, 806);
    public Rectangle BoardLensToggleBounds => BoardLensButtons.ToggleBounds;
    public Rectangle BoardLensPreviousBounds => BoardLensButtons.PreviousBounds;
    public Rectangle BoardLensNextBounds => BoardLensButtons.NextBounds;
    public Rectangle BoardLensExitBounds => BoardLensButtons.ExitBounds;
    public Button DiscardButton { get; }
    public Button AdoptButton { get; }
    public Button ExportSgfButton { get; }
    public Button CommentButton { get; }
    public Button UndoButton { get; }
    public Button PassButton { get; }
    public Button PlayButton { get; }
    public Button BlackButton { get; }
    public Button WhiteButton { get; }
    public Button EraseButton { get; }
    public Button ClearButton { get; }

    public void UpdateState(GoStone? selectedStone, bool canAdopt, bool canUndo)
    {
        AdoptButton.IsEnabled = canAdopt;
        UndoButton.IsEnabled = canUndo;
        PlayButton.IsSelected = selectedStone is null;
        BlackButton.IsSelected = selectedStone == GoStone.Black;
        WhiteButton.IsSelected = selectedStone == GoStone.White;
        EraseButton.IsSelected = selectedStone == GoStone.Empty;
    }
}

/// <summary>盤編集画面の操作列と、その選択・有効状態を所有します。</summary>
public sealed class BoardEditingControls
{
    public BoardEditingControls()
    {
        BlackButton = new Button(new Rectangle(1328, 340, 140, 56), "BLACK", 0.50f);
        WhiteButton = new Button(new Rectangle(1484, 340, 140, 56), "WHITE", 0.50f);
        EraseButton = new Button(new Rectangle(1640, 340, 140, 56), "ERASE", 0.50f);
        UndoButton = new Button(new Rectangle(1328, 458, 140, 56), "UNDO", 0.50f);
        RedoButton = new Button(new Rectangle(1484, 458, 140, 56), "REDO", 0.50f);
        ClearButton = new Button(new Rectangle(1640, 458, 140, 56), "CLEAR BOARD", 0.28f);
        CancelButton = new Button(new Rectangle(1480, 120, 156, 52), "CANCEL", 0.34f);
        AdoptButton = new Button(new Rectangle(1648, 120, 164, 52), "ADOPT", 0.40f);
    }

    public Button BlackButton { get; }
    public Button WhiteButton { get; }
    public Button EraseButton { get; }
    public Button UndoButton { get; }
    public Button RedoButton { get; }
    public Button ClearButton { get; }
    public Button CancelButton { get; }
    public Button AdoptButton { get; }

    public void UpdateState(GoStone selectedStone, bool canUndo, bool canRedo)
    {
        BlackButton.IsSelected = selectedStone == GoStone.Black;
        WhiteButton.IsSelected = selectedStone == GoStone.White;
        EraseButton.IsSelected = selectedStone == GoStone.Empty;
        UndoButton.IsEnabled = canUndo;
        RedoButton.IsEnabled = canRedo;
    }
}

internal static class BoardAndReviewScreenBounds
{
    private static BoardAndReviewScreen Screen => BoardAndReviewScreen.Default;
    private static BoardEditingControls BoardEditing => Screen.BoardEditing;
    private static VariationEditingControls VariationEditing => Screen.VariationEditing;
    private static ReviewControls Review => Screen.Review;

    internal static Rectangle VariationEditingLiveBoardBounds => VariationEditing.LiveBoardBounds;
    internal static Rectangle VariationEditingBoardLensButtonBounds => VariationEditing.BoardLensToggleBounds;
    internal static Rectangle VariationEditingBoardLensPreviousButtonBounds => VariationEditing.BoardLensPreviousBounds;
    internal static Rectangle VariationEditingBoardLensNextButtonBounds => VariationEditing.BoardLensNextBounds;
    internal static Rectangle VariationEditingBoardLensExitButtonBounds => VariationEditing.BoardLensExitBounds;
    internal static Rectangle ReviewAnalysisSectionBounds => Review.AnalysisSectionBounds;
    internal static Rectangle ReviewAnalysisTooltipBounds => Review.AnalysisTooltipBounds;
    internal static Rectangle ReviewUnsavedCommentsNoticeBounds => Review.UnsavedCommentsNoticeBounds;
    internal static Rectangle ReviewBoardLensButtonBounds => Review.BoardLensButton.Bounds;
    internal static Rectangle ReviewBoardLensFamilyButtonBounds => Review.BoardLensNextButton.Bounds;
    internal static Rectangle ReviewBoardLensExitButtonBounds => Review.BoardLensExitButton.Bounds;
    internal static Rectangle ReviewBoardLensPreviousButtonBounds => Review.BoardLensPreviousButton.Bounds;
}
