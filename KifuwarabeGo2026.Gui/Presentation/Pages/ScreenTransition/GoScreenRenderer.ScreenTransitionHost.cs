namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;
using Microsoft.Xna.Framework.Graphics;

/// <summary>GoScreenRenderer と画面遷移演出を接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public ScreenTransition ScreenTransition { get; } = new();

    public void DrawLightningScreenTransition(float progress)
    {
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ScreenTransition.Draw(progress, new ScreenTransitionDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, DrawLine));
        _spriteBatch.End();
    }
}
