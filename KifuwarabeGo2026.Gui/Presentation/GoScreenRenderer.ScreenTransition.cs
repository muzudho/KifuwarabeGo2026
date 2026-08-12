namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// 白いワイヤーフレームの矩形を稲妻で左右に割る画面遷移演出です。
    /// </summary>
    public void DrawLightningScreenTransition(float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        var frameProgress = MathHelper.Clamp(progress / 0.20f, 0f, 1f);
        var splitProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.20f) / 0.80f, 0f, 1f));
        var fade = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.78f) / 0.22f, 0f, 1f));
        var alpha = (int)(255f * fade);
        if (alpha == 0)
            return;

        _spriteBatch.Begin(
            samplerState: Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        var center = new Vector2(VirtualScreen.Width / 2f, VirtualScreen.Height / 2f);
        var width = (int)MathHelper.Lerp(440f, 1320f, frameProgress);
        var height = (int)MathHelper.Lerp(280f, 760f, frameProgress);
        var separation = MathHelper.Lerp(0f, 980f, splitProgress);
        var lineColor = new Color(255, 255, 255, alpha);
        var glowColor = new Color(151, 235, 255, (int)(alpha * 0.55f));

        DrawSplitWireFrame(center, width, height, separation, lineColor, glowColor);
        DrawLightning(center, height, progress, fade, lineColor, glowColor);

        _spriteBatch.End();
    }

    private void DrawSplitWireFrame(
        Vector2 center,
        int width,
        int height,
        float separation,
        Color lineColor,
        Color glowColor)
    {
        var halfWidth = width / 2;
        var top = center.Y - height / 2f;
        var bottom = center.Y + height / 2f;
        var leftOuter = center.X - halfWidth - separation;
        var leftInner = center.X - separation * 0.16f;
        var rightInner = center.X + separation * 0.16f;
        var rightOuter = center.X + halfWidth + separation;

        DrawLine(new Vector2(leftOuter, top), new Vector2(leftInner, top), 3f, lineColor);
        DrawLine(new Vector2(leftOuter, bottom), new Vector2(leftInner, bottom), 3f, lineColor);
        DrawLine(new Vector2(leftOuter, top), new Vector2(leftOuter, bottom), 3f, lineColor);
        DrawLine(new Vector2(leftInner, top), new Vector2(leftInner, bottom), 2f, glowColor);

        DrawLine(new Vector2(rightInner, top), new Vector2(rightOuter, top), 3f, lineColor);
        DrawLine(new Vector2(rightInner, bottom), new Vector2(rightOuter, bottom), 3f, lineColor);
        DrawLine(new Vector2(rightOuter, top), new Vector2(rightOuter, bottom), 3f, lineColor);
        DrawLine(new Vector2(rightInner, top), new Vector2(rightInner, bottom), 2f, glowColor);
    }

    private void DrawLightning(
        Vector2 center,
        int height,
        float progress,
        float fade,
        Color lineColor,
        Color glowColor)
    {
        var top = center.Y - height / 2f - 38f;
        var segmentHeight = (height + 76f) / 10f;
        var previous = new Vector2(center.X, top);
        for (var index = 1; index <= 10; index++)
        {
            var y = top + segmentHeight * index;
            var zigzag = index == 10
                ? 0f
                : MathF.Sin(index * 2.43f + progress * 31f) * (30f + 22f * (1f - fade));
            var current = new Vector2(center.X + zigzag, y);
            DrawLine(previous, current, 9f, glowColor);
            DrawLine(previous, current, 3f, lineColor);
            previous = current;
        }
    }
}
