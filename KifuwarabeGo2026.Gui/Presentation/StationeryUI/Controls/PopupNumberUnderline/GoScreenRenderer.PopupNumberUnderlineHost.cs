namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>GoScreenRenderer と PopupNumberUnderline を接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public PopupNumberUnderline PopupNumberUnderline { get; } = new();

    public void DrawPopupNumberUnderline(
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

        PopupNumberUnderline.Draw(
            mousePoint,
            title,
            text,
            caretIndex,
            selectionStart,
            selectionLength,
            message,
            new PopupNumberUnderlineDrawingCallbacks(
                VirtualScreen.Width,
                VirtualScreen.Height,
                FillRect,
                DrawRect,
                DrawText,
                DrawFittedText,
                DrawTextBoxSelection,
                value => _font.MeasureString(value).X,
                DrawCommandButton));
        _spriteBatch.End();
    }

    public int GetPopupNumberUnderlineCaretIndex(Point point, string text) =>
        PopupNumberUnderline.GetCaretIndex(point, text, GetTextBoxCaretIndex);
}
