namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    // GTP engine spin-option editor popup.
    private static Rectangle IntegerInputDialogBounds => new(610, 300, 700, 390);
    private static Rectangle IntegerInputTextBounds => new(690, 454, 540, 70);
    private static Rectangle IntegerInputCancelButtonBounds => new(910, 594, 150, 54);
    private static Rectangle IntegerInputOkButtonBounds => new(1080, 594, 150, 54);

    public static bool GetIntegerInputDialogCancelButtonHit(Point point) =>
        IntegerInputCancelButtonBounds.Contains(point);

    public static bool GetIntegerInputDialogOkButtonHit(Point point) =>
        IntegerInputOkButtonBounds.Contains(point);

    public void DrawIntegerInputDialog(Point mousePosition, string title, string text, int caretIndex, string message)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 130));
        FillRect(new Rectangle(IntegerInputDialogBounds.X + 14, IntegerInputDialogBounds.Y + 16, IntegerInputDialogBounds.Width, IntegerInputDialogBounds.Height), new Color(0, 0, 0, 155));
        FillRect(IntegerInputDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(IntegerInputDialogBounds, 2, new Color(116, 145, 146));
        DrawText("INTEGER INPUT", new Vector2(IntegerInputDialogBounds.X + 34, IntegerInputDialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawFittedText(title, new Rectangle(IntegerInputDialogBounds.X + 36, IntegerInputDialogBounds.Y + 92, IntegerInputDialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);

        FillRect(IntegerInputTextBounds, new Color(15, 20, 26));
        DrawRect(IntegerInputTextBounds, 2, new Color(99, 223, 185));
        var displayText = string.IsNullOrEmpty(text) ? " " : text;
        DrawFittedText(displayText, new Rectangle(IntegerInputTextBounds.X + 22, IntegerInputTextBounds.Y + 12, IntegerInputTextBounds.Width - 44, 46), Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = IntegerInputTextBounds.X + 22 + (int)(_font.MeasureString(prefix).X * 0.55f);
        FillRect(new Rectangle(Math.Min(caretX, IntegerInputTextBounds.Right - 24), IntegerInputTextBounds.Y + 14, 2, 42), new Color(147, 244, 200));

        DrawFittedText(message, new Rectangle(IntegerInputDialogBounds.X + 80, 540, IntegerInputDialogBounds.Width - 160, 32), new Color(255, 205, 140), 0.32f);
        DrawCommandButton(IntegerInputCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(IntegerInputOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);
        _spriteBatch.End();
    }
}
