namespace KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;

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
    }

    public Button StartReviewingButton { get; }
    public Button StartBoardEditingButton { get; }
    public BoardEditingControls BoardEditing { get; }
    public VariationEditingControls VariationEditing { get; }
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

    internal static Rectangle StartReviewingButtonBounds => Screen.StartReviewingButton.Bounds;
    internal static Rectangle StartBoardEditingButtonBounds => Screen.StartBoardEditingButton.Bounds;
    internal static Rectangle BoardEditingBlackButtonBounds => BoardEditing.BlackButton.Bounds;
    internal static Rectangle BoardEditingWhiteButtonBounds => BoardEditing.WhiteButton.Bounds;
    internal static Rectangle BoardEditingEraseButtonBounds => BoardEditing.EraseButton.Bounds;
    internal static Rectangle BoardEditingUndoButtonBounds => BoardEditing.UndoButton.Bounds;
    internal static Rectangle BoardEditingRedoButtonBounds => BoardEditing.RedoButton.Bounds;
    internal static Rectangle BoardEditingClearButtonBounds => BoardEditing.ClearButton.Bounds;
    internal static Rectangle BoardEditingCancelButtonBounds => BoardEditing.CancelButton.Bounds;
    internal static Rectangle BoardEditingAdoptButtonBounds => BoardEditing.AdoptButton.Bounds;
    internal static Rectangle VariationEditingDiscardButtonBounds => VariationEditing.DiscardButton.Bounds;
    internal static Rectangle VariationEditingLiveBoardBounds => VariationEditing.LiveBoardBounds;
    internal static Rectangle VariationEditingAdoptButtonBounds => VariationEditing.AdoptButton.Bounds;
    internal static Rectangle VariationEditingExportSgfButtonBounds => VariationEditing.ExportSgfButton.Bounds;
    internal static Rectangle VariationEditingCommentButtonBounds => VariationEditing.CommentButton.Bounds;
    internal static Rectangle VariationEditingUndoButtonBounds => VariationEditing.UndoButton.Bounds;
    internal static Rectangle VariationEditingPassButtonBounds => VariationEditing.PassButton.Bounds;
    internal static Rectangle VariationEditingPlayButtonBounds => VariationEditing.PlayButton.Bounds;
    internal static Rectangle VariationEditingBlackButtonBounds => VariationEditing.BlackButton.Bounds;
    internal static Rectangle VariationEditingWhiteButtonBounds => VariationEditing.WhiteButton.Bounds;
    internal static Rectangle VariationEditingEraseButtonBounds => VariationEditing.EraseButton.Bounds;
    internal static Rectangle VariationEditingClearButtonBounds => VariationEditing.ClearButton.Bounds;
    internal static Rectangle VariationEditingBoardLensButtonBounds => VariationEditing.BoardLensToggleBounds;
    internal static Rectangle VariationEditingBoardLensPreviousButtonBounds => VariationEditing.BoardLensPreviousBounds;
    internal static Rectangle VariationEditingBoardLensNextButtonBounds => VariationEditing.BoardLensNextBounds;
    internal static Rectangle VariationEditingBoardLensExitButtonBounds => VariationEditing.BoardLensExitBounds;
}
