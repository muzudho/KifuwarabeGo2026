namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    private static Rectangle TextInputDialogBounds => new(510, 260, 900, 470);
    private static Rectangle TextInputTextBounds => new(590, 434, 740, 150);
    private static Rectangle TextInputCancelButtonBounds => new(990, 644, 150, 54);
    private static Rectangle TextInputOkButtonBounds => new(1160, 644, 150, 54);

    public static bool GetTextInputDialogCancelButtonHit(Point point) =>
        TextInputCancelButtonBounds.Contains(point);

    public static bool GetTextInputDialogOkButtonHit(Point point) =>
        TextInputOkButtonBounds.Contains(point);

    public static bool IsTextInputDialogTextBoxHit(Point point) =>
        TextInputTextBounds.Contains(point);

    public int GetTextInputDialogCaretIndex(Point point, string text) =>
        GetTextBoxCaretIndex(point.X, text, TextInputTextContentBounds, 0.55f);

    public void DrawTextInputDialog(
        Point mousePosition,
        string title,
        string text,
        int caretIndex,
        int selectionStart,
        int selectionLength,
        string message)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 130));
        FillRect(new Rectangle(TextInputDialogBounds.X + 14, TextInputDialogBounds.Y + 16, TextInputDialogBounds.Width, TextInputDialogBounds.Height), new Color(0, 0, 0, 155));
        FillRect(TextInputDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(TextInputDialogBounds, 2, new Color(116, 145, 146));
        DrawText("TEXT INPUT", new Vector2(TextInputDialogBounds.X + 34, TextInputDialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawFittedText(title, new Rectangle(TextInputDialogBounds.X + 36, TextInputDialogBounds.Y + 92, TextInputDialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);

        FillRect(TextInputTextBounds, new Color(15, 20, 26));
        DrawRect(TextInputTextBounds, 2, new Color(99, 223, 185));
        DrawTextBoxSelection(text, selectionStart, selectionLength, TextInputTextContentBounds, 0.55f);
        var displayText = string.IsNullOrEmpty(text) ? " " : text;
        DrawFittedText(displayText, TextInputTextContentBounds, Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = TextInputTextContentBounds.X + (int)(_font.MeasureString(prefix).X * 0.55f);
        FillRect(new Rectangle(Math.Min(caretX, TextInputTextBounds.Right - 24), TextInputTextBounds.Y + 14, 2, TextInputTextBounds.Height - 28), new Color(147, 244, 200));

        DrawFittedText(message, new Rectangle(TextInputDialogBounds.X + 80, 604, TextInputDialogBounds.Width - 160, 32), new Color(180, 195, 195), 0.32f);
        DrawCommandButton(TextInputCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(TextInputOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);
        _spriteBatch.End();
    }

    private static Rectangle TextInputTextContentBounds =>
        new(TextInputTextBounds.X + 22, TextInputTextBounds.Y + 12, TextInputTextBounds.Width - 44, TextInputTextBounds.Height - 24);
}
