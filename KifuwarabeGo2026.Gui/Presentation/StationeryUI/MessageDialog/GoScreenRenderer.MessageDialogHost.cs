namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.MessageDialog;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    public void DrawMessageDialog(MessageDialog dialog, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(samplerState: Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        dialog.Draw(mousePoint, new MessageDialogDrawingCallbacks(FillRect, DrawRect, DrawDynamicOptionText, DrawLine,
            (bounds, text, focused, point, scale) => DrawCommandButton(bounds, text, focused, point, scale: scale)));
        _spriteBatch.End();
    }
}
