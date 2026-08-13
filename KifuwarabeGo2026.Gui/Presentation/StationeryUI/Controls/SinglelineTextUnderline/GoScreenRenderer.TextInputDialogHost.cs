namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private readonly TextInputDialog _textInputDialog = new();

    public static bool GetTextInputDialogCancelButtonHit(Point point) => TextInputDialog.IsCancelButtonHit(point);
    public static bool GetTextInputDialogOkButtonHit(Point point) => TextInputDialog.IsOkButtonHit(point);
    public static bool GetTextInputDialogDefaultButtonHit(Point point) => TextInputDialog.IsDefaultButtonHit(point);
    public static bool IsTextInputDialogTextBoxHit(Point point) => TextInputDialog.IsTextBoxHit(point);
    public int GetTextInputDialogCaretIndex(Point point, string text) => GetTextBoxCaretIndex(point.X, text, TextInputDialog.TextContentBounds, 0.55f);

    public void DrawTextInputDialog(Point mousePosition, string title, string text, int caretIndex, int selectionStart,
        int selectionLength, string message, bool showDefaultButton = false, TextCompositionState composition = default,
        TextCompositionDiagnostics compositionDiagnostics = default, bool showCompositionDiagnostics = false)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _textInputDialog.Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message, showDefaultButton,
            composition, compositionDiagnostics, showCompositionDiagnostics,
            new TextInputDialogDrawingCallbacks(FillRect, DrawRect, DrawText, DrawFittedText, DrawTextBoxSelection,
                DrawDynamicCompositionText, _font.MeasureString, DrawLine, DrawCompositionLamp, DrawCommandButton));
        _spriteBatch.End();
    }
}
