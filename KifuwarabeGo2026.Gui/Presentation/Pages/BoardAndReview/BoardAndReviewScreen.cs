namespace KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

/// <summary>盤編集・変化図編集・棋譜検討画面のUIを所有します。</summary>
public sealed class BoardAndReviewScreen
{
    public static BoardAndReviewScreen Default { get; } = new();

    private BoardAndReviewScreen()
    {
        StartReviewingButton = new Button(new Rectangle(1315, 920, 154, 56), "KIFU REVIEW", 0.32f);
        StartBoardEditingButton = new Button(new Rectangle(1486, 920, 154, 56), "EDIT BOARD", 0.36f);
        BoardEditing = new BoardEditingControls();
    }

    public Button StartReviewingButton { get; }
    public Button StartBoardEditingButton { get; }
    public BoardEditingControls BoardEditing { get; }
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
}
