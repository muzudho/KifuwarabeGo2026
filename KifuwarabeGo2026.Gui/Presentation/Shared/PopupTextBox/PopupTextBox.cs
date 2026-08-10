namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    private static Rectangle TextInputDialogBounds => new(510, 310, 900, 400);
    private static Rectangle TextInputTextBounds => new(590, 455, 740, 70);
    private static Rectangle TextInputDefaultButtonBounds => new(820, 590, 150, 54);
    private static Rectangle TextInputCancelButtonBounds => new(990, 590, 150, 54);
    private static Rectangle TextInputOkButtonBounds => new(1160, 590, 150, 54);

    public static bool GetTextInputDialogCancelButtonHit(Point point) =>
        TextInputCancelButtonBounds.Contains(point);

    public static bool GetTextInputDialogOkButtonHit(Point point) =>
        TextInputOkButtonBounds.Contains(point);

    public static bool GetTextInputDialogDefaultButtonHit(Point point) =>
        TextInputDefaultButtonBounds.Contains(point);

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
        string message,
        bool showDefaultButton = false,
        TextCompositionState composition = default,
        TextCompositionDiagnostics compositionDiagnostics = default)
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
        // IME composition の受信確認用ランプ。灰色なら未確定文字列イベントなし、黄色なら受信中。
        DrawCompositionLamp("SDL", TextInputDialogBounds.Right - 120, compositionDiagnostics.IsSdlWindowResolved, new Color(99, 223, 185));
        DrawCompositionLamp("HOOK", TextInputDialogBounds.Right - 76, compositionDiagnostics.IsWindowProcedureAttached, new Color(99, 223, 185));
        DrawCompositionLamp("IME", TextInputDialogBounds.Right - 30, composition.IsActive, new Color(255, 225, 128));

        FillRect(TextInputTextBounds, new Color(15, 20, 26));
        DrawRect(TextInputTextBounds, 2, new Color(99, 223, 185));
        DrawTextBoxSelection(text, selectionStart, selectionLength, TextInputTextContentBounds, 0.55f);
        var displayText = string.IsNullOrEmpty(text) ? " " : text;
        DrawFittedText(displayText, TextInputTextContentBounds, Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = TextInputTextContentBounds.X + (int)(_font.MeasureString(prefix).X * 0.55f);
        if (composition.IsActive)
        {
            var compositionText = composition.Text ?? "";
            DrawText(compositionText, new Vector2(caretX, TextInputTextContentBounds.Y + 2), new Color(255, 225, 128), 0.55f);
            var compositionWidth = _font.MeasureString(compositionText).X * 0.55f;
            DrawLine(
                new Vector2(caretX, TextInputTextContentBounds.Bottom - 4),
                new Vector2(caretX + compositionWidth, TextInputTextContentBounds.Bottom - 4),
                2,
                new Color(255, 225, 128));
            var compositionCaret = compositionText[..Math.Clamp(composition.CaretIndex, 0, compositionText.Length)];
            var compositionCaretX = caretX + (int)(_font.MeasureString(compositionCaret).X * 0.55f);
            FillRect(new Rectangle(Math.Min(compositionCaretX, TextInputTextBounds.Right - 24), TextInputTextBounds.Y + 14, 2, TextInputTextBounds.Height - 28), new Color(255, 225, 128));
        }
        else
        {
            FillRect(new Rectangle(Math.Min(caretX, TextInputTextBounds.Right - 24), TextInputTextBounds.Y + 14, 2, TextInputTextBounds.Height - 28), new Color(147, 244, 200));
        }

        DrawFittedText(message, new Rectangle(TextInputDialogBounds.X + 80, 544, TextInputDialogBounds.Width - 160, 32), new Color(180, 195, 195), 0.32f);
        if (showDefaultButton)
            DrawCommandButton(TextInputDefaultButtonBounds, "DEFAULT", false, mousePoint, scale: 0.30f);
        DrawCommandButton(TextInputCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(TextInputOkButtonBounds, "OK", false, mousePoint, scale: 0.42f);
        _spriteBatch.End();
    }

    private static Rectangle TextInputTextContentBounds =>
        new(TextInputTextBounds.X + 22, TextInputTextBounds.Y + 12, TextInputTextBounds.Width - 44, TextInputTextBounds.Height - 24);

    private void DrawCompositionLamp(string label, int x, bool enabled, Color activeColor)
    {
        var center = new Vector2(x, TextInputDialogBounds.Y + 47);
        DrawCircle(center, 8, enabled ? activeColor : new Color(79, 89, 98));
        DrawText(label, new Vector2(center.X - _font.MeasureString(label).X * 0.11f, TextInputDialogBounds.Y + 66), new Color(180, 195, 195), 0.22f);
    }
}
