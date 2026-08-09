namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// 撮影後に、カメラのフラッシュとシャッター幕を短く表示します。
    /// </summary>
    public void DrawScreenshotCaptureEffect(float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        var flashOpacity = progress < 0.30f
            ? (byte)(210f * (1f - progress / 0.30f))
            : (byte)0;
        if (flashOpacity > 0)
            FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(255, 255, 245, (int)flashOpacity));

        var shutterPhase = progress < 0.42f
            ? progress / 0.42f
            : 1f - (progress - 0.42f) / 0.58f;
        shutterPhase = Math.Clamp(shutterPhase, 0f, 1f);
        var shutterHeight = (int)(VirtualScreen.Height * 0.075f * shutterPhase);
        if (shutterHeight > 0)
        {
            var shutterColor = new Color(8, 10, 14, (int)(215f * shutterPhase));
            FillRect(new Rectangle(0, 0, VirtualScreen.Width, shutterHeight), shutterColor);
            FillRect(new Rectangle(0, VirtualScreen.Height - shutterHeight, VirtualScreen.Width, shutterHeight), shutterColor);
        }

        var lineOpacity = (byte)(255f * (1f - progress));
        if (lineOpacity > 0)
            FillRect(new Rectangle(0, VirtualScreen.Height / 2 - 2, VirtualScreen.Width, 4), new Color(255, 255, 255, (int)lineOpacity));

        _spriteBatch.End();
    }
}
