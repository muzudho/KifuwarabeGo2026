namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>一行テキスト入力ダイアログのレイアウトと操作領域を所有します。</summary>
public sealed class TextInputDialog
{
    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePosition, string title, string text,
        int caretIndex, int selectionStart, int selectionLength, string message, bool showDefaultButton = false,
        TextCompositionState composition = default, TextCompositionDiagnostics compositionDiagnostics = default,
        bool showCompositionDiagnostics = false)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message, showDefaultButton,
            composition, compositionDiagnostics, showCompositionDiagnostics,
            new TextInputDialogDrawingCallbacks(
                drawingContext.FillRectangle, drawingContext.DrawRectangle, drawingContext.DrawText,
                drawingContext.DrawFittedText, drawingContext.DrawTextSelection,
                (value, position, color, scale) =>
                {
                    var width = drawingContext.MeasureText(value).X * scale;
                    drawingContext.DrawDynamicText(value, new Rectangle((int)position.X, (int)position.Y,
                        Math.Max(1, (int)MathF.Ceiling(width)), 40), color, scale);
                    return width;
                },
                drawingContext.MeasureText, drawingContext.DrawLine,
                (label, x, enabled, color) => DrawCompositionLamp(drawingContext, label, x, enabled, color),
                drawingContext.DrawButton));
        drawingContext.End();
    }

    public int GetCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, string text) =>
        drawingContext.GetTextCaretIndex(point.X, text, TextContentBounds, 0.55f);

    private static void DrawCompositionLamp(KfwStationeryDrawingTools drawingContext, string label, int x,
        bool enabled, Color activeColor)
    {
        var center = new Vector2(x, Bounds.Y + 47);
        drawingContext.DrawCircle(center, 8, enabled ? activeColor : new Color(79, 89, 98));
        drawingContext.DrawText(label,
            new Vector2(center.X - drawingContext.MeasureText(label).X * 0.11f, Bounds.Y + 66),
            new Color(180, 195, 195), 0.22f);
    }

    public static Rectangle Bounds => new(510, 310, 900, 400);
    public static Rectangle TextBounds => new(590, 455, 740, 70);
    public static Rectangle DefaultButtonBounds => new(820, 590, 150, 54);
    public static Rectangle CancelButtonBounds => new(990, 590, 150, 54);
    public static Rectangle OkButtonBounds => new(1160, 590, 150, 54);
    public static Rectangle TextContentBounds => new(TextBounds.X + 22, TextBounds.Y + 12, TextBounds.Width - 44, TextBounds.Height - 24);

    public static bool IsCancelButtonHit(Point point) => CancelButtonBounds.Contains(point);
    public static bool IsOkButtonHit(Point point) => OkButtonBounds.Contains(point);
    public static bool IsDefaultButtonHit(Point point) => DefaultButtonBounds.Contains(point);
    public static bool IsTextBoxHit(Point point) => TextBounds.Contains(point);

    public void Draw(Point mousePoint, string title, string text, int caretIndex, int selectionStart, int selectionLength,
        string message, bool showDefaultButton, TextCompositionState composition,
        TextCompositionDiagnostics compositionDiagnostics, bool showCompositionDiagnostics, TextInputDialogDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.FillRectangle(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 130));
        draw.FillRectangle(new Rectangle(Bounds.X + 14, Bounds.Y + 16, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 155));
        draw.FillRectangle(Bounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(Bounds, 2, new Color(116, 145, 146));
        draw.DrawText("TEXT INPUT", new Vector2(Bounds.X + 34, Bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        draw.DrawFittedText(title, new Rectangle(Bounds.X + 36, Bounds.Y + 92, Bounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);
        draw.DrawCompositionLamp("SDL", Bounds.Right - 120, compositionDiagnostics.IsSdlWindowResolved, new Color(99, 223, 185));
        if (showCompositionDiagnostics)
        {
            draw.DrawCompositionLamp("HOOK", Bounds.Right - 76, compositionDiagnostics.IsWindowProcedureAttached, new Color(99, 223, 185));
            draw.DrawCompositionLamp("IME", Bounds.Right - 30, composition.IsActive, new Color(255, 225, 128));
        }
        draw.DrawLine(new Vector2(TextBounds.X, TextBounds.Bottom - 6), new Vector2(TextBounds.Right, TextBounds.Bottom - 6), 2, new Color(99, 223, 185));
        draw.DrawTextSelection(text, selectionStart, selectionLength, TextContentBounds, 0.55f);
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? " " : text, TextContentBounds, Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = TextContentBounds.X + (int)(draw.MeasureText(prefix).X * 0.55f);
        if (composition.IsActive)
        {
            var compositionText = composition.Text ?? "";
            var compositionWidth = draw.DrawDynamicCompositionText(compositionText, new Vector2(caretX, TextContentBounds.Y + 2), new Color(255, 225, 128), 0.55f);
            draw.DrawLine(new Vector2(caretX, TextContentBounds.Bottom - 4), new Vector2(caretX + compositionWidth, TextContentBounds.Bottom - 4), 2, new Color(255, 225, 128));
            var compositionPrefix = compositionText[..Math.Clamp(composition.CaretIndex, 0, compositionText.Length)];
            var compositionCaretX = caretX + (int)(draw.MeasureText(compositionPrefix).X * 0.55f);
            draw.FillRectangle(new Rectangle(Math.Min(compositionCaretX, TextBounds.Right - 24), TextBounds.Y + 14, 2, TextBounds.Height - 28), new Color(255, 225, 128));
        }
        else draw.FillRectangle(new Rectangle(Math.Min(caretX, TextBounds.Right - 24), TextBounds.Y + 14, 2, TextBounds.Height - 28), new Color(147, 244, 200));
        draw.DrawFittedText(message, new Rectangle(Bounds.X + 80, 544, Bounds.Width - 160, 32), new Color(180, 195, 195), 0.32f);
        if (showDefaultButton) draw.DrawButton(DefaultButtonBounds, "DEFAULT", false, mousePoint, true, 0.30f);
        draw.DrawButton(CancelButtonBounds, "CANCEL", false, mousePoint, true, 0.34f);
        draw.DrawButton(OkButtonBounds, "OK", false, mousePoint, true, 0.42f);
    }
}

public sealed record TextInputDialogDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<string, int, int, Rectangle, float> DrawTextSelection,
    Func<string, Vector2, Color, float, float> DrawDynamicCompositionText,
    Func<string, Vector2> MeasureText,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<string, int, bool, Color> DrawCompositionLamp,
    Action<Rectangle, string, bool, Point, bool, float> DrawButton);
