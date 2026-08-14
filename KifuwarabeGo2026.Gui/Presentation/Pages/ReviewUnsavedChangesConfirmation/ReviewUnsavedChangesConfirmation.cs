namespace KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using System;

/// <summary>未保存の棋譜コメントを確認してからレビュー画面を離れるためのページです。</summary>
public sealed class ReviewUnsavedChangesConfirmation
{
    #region Layout

    private static readonly Rectangle DialogBounds = new(570, 370, 780, 340);

    #endregion

    #region Buttons

    /// <summary>SGF へ保存してレビューを終了します。</summary>
    public Button SaveButton { get; } = new(new Rectangle(650, 612, 210, 54), "SAVE SGF", 0.34f);

    /// <summary>変更を破棄してレビューを終了します。</summary>
    public Button DiscardButton { get; } = new(new Rectangle(885, 612, 210, 54), "DON'T SAVE", 0.28f);

    /// <summary>確認画面を閉じてレビューへ戻ります。</summary>
    public Button CancelButton { get; } = new(new Rectangle(1120, 612, 150, 54), "CANCEL", 0.34f);

    #endregion

    #region Drawing

    /// <summary>未保存変更の確認画面を描画します。</summary>
    public void Draw(Point mousePoint, ReviewUnsavedChangesConfirmationDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 165));
        draw.FillRectangle(DialogBounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(DialogBounds, 2, new Color(255, 183, 146));
        draw.DrawText("UNSAVED COMMENTS", new Vector2(DialogBounds.X + 34, DialogBounds.Y + 30), new Color(255, 230, 160), 0.64f);
        draw.DrawFittedText("Comments have not been written to an SGF file.", new Rectangle(DialogBounds.X + 34, DialogBounds.Y + 112, 700, 38), Color.White, 0.44f);
        draw.DrawFittedText("Save before leaving this review?", new Rectangle(DialogBounds.X + 34, DialogBounds.Y + 160, 700, 36), new Color(180, 195, 195), 0.40f);
        SaveButton.Draw(mousePoint, draw.ButtonSurface);
        DiscardButton.Draw(mousePoint, draw.ButtonSurface);
        CancelButton.Draw(mousePoint, draw.ButtonSurface);
    }

    #endregion
}

/// <summary>ReviewUnsavedChangesConfirmation に渡す描画機能です。</summary>
public sealed record ReviewUnsavedChangesConfirmationDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    IButtonDrawingSurface ButtonSurface);
