namespace KifuwarabeGo2026.Gui.Presentation.Shared.TextAreaDialog;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using System;

/// <summary>コメント入力ダイアログの領域と操作 UI を所有します。</summary>
public sealed class TextAreaDialog
{
    public static TextAreaDialog Default { get; } = new();

    private TextAreaDialog()
    {
        DiscardButton = new Button(new Rectangle(1230, 172, 150, 54), "DISCARD", 0.30f);
        ApplyButton = new Button(new Rectangle(1410, 172, 150, 54), "CLOSE", 0.34f);
    }

    public Rectangle Bounds { get; } = new(320, 150, 1280, 780);
    public Rectangle TextBounds { get; } = new(390, 330, 1140, 400);
    public Button DiscardButton { get; }
    public Button ApplyButton { get; }

    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePosition, string title, string text,
        int caretIndex, int selectionStart, int selectionLength, string message, bool hasChanges, TextCompositionState composition = default,
        TextCompositionDiagnostics compositionDiagnostics = default, bool showCompositionDiagnostics = false)
    {
        SetHasChanges(hasChanges);
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        drawingContext.FillRectangle(new Rectangle(0, 0, drawingContext.ScreenWidth, drawingContext.ScreenHeight), new Color(0, 0, 0, 145));
        drawingContext.FillRectangle(new Rectangle(Bounds.X + 14, Bounds.Y + 16, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 155));
        drawingContext.FillRectangle(Bounds, new Color(24, 29, 36, 252));
        drawingContext.DrawRectangle(Bounds, 2, new Color(116, 145, 146));
        drawingContext.DrawText("COMMENT EDITOR", new Vector2(Bounds.X + 34, Bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        drawingContext.DrawDynamicText(title, new Rectangle(Bounds.X + 36, Bounds.Y + 96, Bounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);
        drawingContext.DrawRectangle(TextBounds, 1, new Color(99, 223, 185));
        DrawLines(drawingContext, text, selectionStart, selectionLength);
        var caret = GetCaretPosition(drawingContext, text, caretIndex);
        if (composition.IsActive && !string.IsNullOrEmpty(composition.Text))
        {
            drawingContext.DrawText(composition.Text, caret, new Color(255, 225, 128), 0.42f);
            var width = drawingContext.MeasureText(composition.Text).X * 0.42f;
            drawingContext.DrawLine(caret + new Vector2(0, 29), caret + new Vector2(width, 29), 2, new Color(255, 225, 128));
        }
        drawingContext.FillRectangle(new Rectangle((int)caret.X, (int)caret.Y, 2, 29),
            composition.IsActive ? new Color(255, 225, 128) : new Color(147, 244, 200));
        drawingContext.DrawDynamicText(message, new Rectangle(Bounds.X + 70, 752, 820, 34), new Color(180, 195, 195), 0.34f);
        drawingContext.DrawFittedText("ENTER: NEW LINE   CTRL+ENTER: SAVE SGF", new Rectangle(Bounds.X + 70, 786, 800, 28), new Color(147, 201, 190), 0.29f);
        DiscardButton.Draw(mousePoint, drawingContext);
        ApplyButton.Draw(mousePoint, drawingContext);
        drawingContext.End();
    }

    private void DrawLines(KfwStationeryDrawingTools drawingContext, string text, int selectionStart, int selectionLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            drawingContext.DrawFittedText("(EMPTY COMMENT)", new Rectangle(TextBounds.X + 18, TextBounds.Y + 18, TextBounds.Width - 36, 34), new Color(112, 132, 136), 0.34f);
            return;
        }
        var normalizedText = text.Replace("\r", string.Empty);
        var lines = normalizedText.Split('\n');
        var lineStart = 0;
        var selectionEnd = selectionStart + selectionLength;
        for (var index = 0; index < lines.Length && index < 11; index++)
        {
            var line = lines[index];
            var bounds = new Rectangle(TextBounds.X + 18, TextBounds.Y + 18 + index * 32, TextBounds.Width - 36, 30);
            drawingContext.DrawFittedText(line, bounds, new Color(226, 232, 225), 0.34f);
            var overlapStart = Math.Max(selectionStart, lineStart);
            var overlapEnd = Math.Min(selectionEnd, lineStart + line.Length);
            if (overlapEnd > overlapStart)
                drawingContext.DrawTextSelection(line, overlapStart - lineStart, overlapEnd - overlapStart, bounds, 0.34f);
            lineStart += line.Length + 1;
        }
    }

    public bool IsTextBoxHit(Point point) => TextBounds.Contains(point);

    public int GetCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, string text)
    {
        var normalizedText = text.Replace("\r", string.Empty);
        var lines = normalizedText.Split('\n');
        var lineIndex = Math.Clamp((point.Y - (TextBounds.Y + 18)) / 32, 0, Math.Min(lines.Length - 1, 10));
        var lineStart = 0;
        for (var index = 0; index < lineIndex; index++) lineStart += lines[index].Length + 1;
        var bounds = new Rectangle(TextBounds.X + 18, TextBounds.Y + 18 + lineIndex * 32, TextBounds.Width - 36, 30);
        return lineStart + drawingContext.GetTextCaretIndex(point.X, lines[lineIndex], bounds, 0.34f);
    }

    private Vector2 GetCaretPosition(KfwStationeryDrawingTools drawingContext, string text, int caretIndex)
    {
        var before = text[..Math.Clamp(caretIndex, 0, text.Length)].Replace("\r", string.Empty);
        var lines = before.Split('\n');
        return new Vector2(TextBounds.X + 18 + drawingContext.MeasureText(lines[^1]).X * 0.34f,
            TextBounds.Y + 18 + (lines.Length - 1) * 32);
    }

    public void SetHasChanges(bool hasChanges)
    {
        DiscardButton.IsEnabled = hasChanges;
        ApplyButton.Label = hasChanges ? "SAVE & CLOSE" : "CLOSE";
        ApplyButton.LabelScale = hasChanges ? 0.25f : 0.34f;
    }
}
