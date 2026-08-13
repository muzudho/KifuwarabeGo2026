namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using System;

/// <summary>リンクから開く整数入力用のモーダル画面です。</summary>
public sealed class PopupNumberUnderline
{
    #region Layout

    private static readonly Rectangle DialogBounds = new(610, 300, 700, 390);
    private static readonly Rectangle TextBounds = new(690, 454, 540, 70);
    private static readonly Rectangle TextContentBounds = new(TextBounds.X + 22, TextBounds.Y + 12, TextBounds.Width - 44, 46);

    #endregion

    #region Buttons

    /// <summary>入力を取り消します。</summary>
    public Button CancelButton { get; } = new(new Rectangle(DialogBounds.Right - 360, DialogBounds.Y + 22, 150, 54), "CANCEL", 0.34f);

    /// <summary>入力した数値を確定します。</summary>
    public Button OkButton { get; } = new(new Rectangle(DialogBounds.Right - 190, DialogBounds.Y + 22, 150, 54), "OK", 0.42f);

    /// <summary>
    /// 下げるボタン
    /// </summary>
    public Button StepDownButton { get; } = new(new Rectangle(812, 594, 82, 54), "▼", 0.46f);

    /// <summary>
    /// 上げるボタン
    /// </summary>
    public Button StepUpButton { get; } = new(new Rectangle(700, 594, 82, 54), "▲", 0.46f);

    #endregion

    #region Hit testing and caret

    public bool IsTextBoxHit(Point point) => TextBounds.Contains(point);

    public int GetCaretIndex(Point point, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, TextContentBounds, 0.55f);

    #endregion

    #region Drawing

    /// <summary>整数入力ダイアログを描画します。</summary>
    public void Draw(
        Point mousePoint,
        string title,
        string text,
        int caretIndex,
        int selectionStart,
        int selectionLength,
        string message,
        PopupNumberUnderlineDrawingCallbacks draw,
        PopupNumberUnderlineOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(draw);
        title ??= string.Empty;
        text ??= string.Empty;
        message ??= string.Empty;

        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 130));
        draw.FillRectangle(new Rectangle(DialogBounds.X + 14, DialogBounds.Y + 16, DialogBounds.Width, DialogBounds.Height), new Color(0, 0, 0, 155));
        draw.FillRectangle(DialogBounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(DialogBounds, 2, new Color(116, 145, 146));
        draw.DrawText(options.Caption ?? "NUMBER INPUT", new Vector2(DialogBounds.X + 34, DialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        draw.DrawFittedText(title, new Rectangle(DialogBounds.X + 36, DialogBounds.Y + 92, DialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);

        draw.FillRectangle(TextBounds, new Color(15, 20, 26));
        draw.DrawRectangle(TextBounds, 2, new Color(99, 223, 185));
        draw.DrawTextSelection(text, selectionStart, selectionLength, TextContentBounds, 0.55f);
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? " " : text, TextContentBounds, Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = TextContentBounds.X + (int)(draw.MeasureTextWidth(prefix) * 0.55f);
        draw.FillRectangle(new Rectangle(Math.Min(caretX, TextBounds.Right - 24), TextBounds.Y + 14, 2, 42), new Color(147, 244, 200));

        draw.DrawFittedText(message, new Rectangle(DialogBounds.X + 80, 540, DialogBounds.Width - 160, 32), new Color(255, 205, 140), 0.32f);

        // ステップアップ・ステップダウンボタンの描画
        if (options.ShowStepControls)
        {
            StepUpButton.Draw(mousePoint, draw.DrawButton);
            StepDownButton.Draw(mousePoint, draw.DrawButton);
            draw.DrawFittedText($"STEP {options.StepLabel ?? "1"}", new Rectangle(700, 558, 194, 28), new Color(180, 195, 195), 0.28f);
        }
        CancelButton.Draw(mousePoint, draw.DrawButton);
        OkButton.Draw(mousePoint, draw.DrawButton);
    }

    #endregion
}

/// <summary>
/// PopupNumberUnderline の描画オプションです。
/// </summary>
/// <param name="ShowStepControls"></param>
/// <param name="StepLabel"></param>
/// <param name="Caption"></param>
public readonly record struct PopupNumberUnderlineOptions(bool ShowStepControls = false, string? StepLabel = null, string? Caption = null);

/// <summary>PopupNumberUnderline に渡す描画機能です。</summary>
public sealed record PopupNumberUnderlineDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<string, int, int, Rectangle, float> DrawTextSelection,
    Func<string, float> MeasureTextWidth,
    Action<Rectangle, string, bool, Point, bool, float> DrawButton);
