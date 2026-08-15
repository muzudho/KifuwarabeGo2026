namespace KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;

using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>画面遷移時に表示する稲妻付きの分割フレーム演出です。</summary>
public sealed class ScreenTransition
{
    public void Draw(StationeryDrawingContext drawingContext, float progress)
    {
        drawingContext.Begin();
        Draw(progress, new ScreenTransitionDrawingCallbacks(
            drawingContext.ScreenWidth, drawingContext.ScreenHeight, drawingContext.DrawLine));
        drawingContext.End();
    }

    /// <summary>演出の進行度を 0 から 1 の範囲で描画します。</summary>
    public void Draw(float progress, ScreenTransitionDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        progress = MathHelper.Clamp(progress, 0f, 1f);
        var frameProgress = MathHelper.Clamp(progress / 0.20f, 0f, 1f);
        var splitProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.20f) / 0.80f, 0f, 1f));
        var fade = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.78f) / 0.22f, 0f, 1f));
        var alpha = (int)(255f * fade);
        if (alpha == 0) return;

        var center = new Vector2(draw.VirtualScreenWidth / 2f, draw.VirtualScreenHeight / 2f);
        var width = (int)MathHelper.Lerp(440f, 1320f, frameProgress);
        var height = (int)MathHelper.Lerp(280f, 760f, frameProgress);
        var separation = MathHelper.Lerp(0f, 980f, splitProgress);
        var lineColor = new Color(255, 255, 255, alpha);
        var glowColor = new Color(151, 235, 255, (int)(alpha * 0.55f));

        DrawSplitWireFrame(center, width, height, separation, lineColor, glowColor, draw.DrawLine);
        DrawLightning(center, height, progress, fade, lineColor, glowColor, draw.DrawLine);
    }

    /// <summary>左右に分かれるフレームを描画します。</summary>
    private static void DrawSplitWireFrame(
        Vector2 center,
        int width,
        int height,
        float separation,
        Color lineColor,
        Color glowColor,
        Action<Vector2, Vector2, float, Color> drawLine)
    {
        var halfWidth = width / 2;
        var top = center.Y - height / 2f;
        var bottom = center.Y + height / 2f;
        var leftOuter = center.X - halfWidth - separation;
        var leftInner = center.X - separation * 0.16f;
        var rightInner = center.X + separation * 0.16f;
        var rightOuter = center.X + halfWidth + separation;

        drawLine(new Vector2(leftOuter, top), new Vector2(leftInner, top), 3f, lineColor);
        drawLine(new Vector2(leftOuter, bottom), new Vector2(leftInner, bottom), 3f, lineColor);
        drawLine(new Vector2(leftOuter, top), new Vector2(leftOuter, bottom), 3f, lineColor);
        drawLine(new Vector2(leftInner, top), new Vector2(leftInner, bottom), 2f, glowColor);
        drawLine(new Vector2(rightInner, top), new Vector2(rightOuter, top), 3f, lineColor);
        drawLine(new Vector2(rightInner, bottom), new Vector2(rightOuter, bottom), 3f, lineColor);
        drawLine(new Vector2(rightOuter, top), new Vector2(rightOuter, bottom), 3f, lineColor);
        drawLine(new Vector2(rightInner, top), new Vector2(rightInner, bottom), 2f, glowColor);
    }

    /// <summary>中央を走る稲妻を描画します。</summary>
    private static void DrawLightning(
        Vector2 center,
        int height,
        float progress,
        float fade,
        Color lineColor,
        Color glowColor,
        Action<Vector2, Vector2, float, Color> drawLine)
    {
        var top = center.Y - height / 2f - 38f;
        var segmentHeight = (height + 76f) / 10f;
        var previous = new Vector2(center.X, top);
        for (var index = 1; index <= 10; index++)
        {
            var y = top + segmentHeight * index;
            var zigzag = index == 10 ? 0f : MathF.Sin(index * 2.43f + progress * 31f) * (30f + 22f * (1f - fade));
            var current = new Vector2(center.X + zigzag, y);
            drawLine(previous, current, 9f, glowColor);
            drawLine(previous, current, 3f, lineColor);
            previous = current;
        }
    }
}

/// <summary>ScreenTransition に渡す描画機能です。</summary>
public sealed record ScreenTransitionDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Vector2, Vector2, float, Color> DrawLine);
