namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenshotEffect;
using Microsoft.Xna.Framework.Graphics;

/// <summary>GoScreenRenderer とスクリーンショット演出を接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public ScreenshotEffect ScreenshotEffect { get; } = new();

    public void DrawScreenshotCaptureEffect(float progress)
    {
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ScreenshotEffect.Draw(progress, new ScreenshotEffectDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect));
        _spriteBatch.End();
    }
}
