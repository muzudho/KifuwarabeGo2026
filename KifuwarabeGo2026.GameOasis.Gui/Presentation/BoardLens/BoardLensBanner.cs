namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;

/// <summary>現在選択中の Board Lens 通知を描画します。</summary>
public static class BoardLensBanner
{
    public static void Draw(KfwStationeryDrawingTools drawingContext, string lensName, string lensAlias,
        string guide, float opacity, float compactProgress)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        compactProgress = Math.Clamp(compactProgress, 0f, 1f);
        compactProgress = compactProgress * compactProgress * (3f - 2f * compactProgress);
        drawingContext.Begin();
        var hasAlias = !string.IsNullOrWhiteSpace(lensAlias);
        var large = new Rectangle(560, 48, 800, 122);
        var compact = hasAlias ? new Rectangle(209, 4, 670, 88) : new Rectangle(209, 10, 670, 72);
        var bounds = new Rectangle(
            (int)MathF.Round(MathHelper.Lerp(large.X, compact.X, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(large.Y, compact.Y, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(large.Width, compact.Width, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(large.Height, compact.Height, compactProgress)));
        var textAlpha = (int)(255f * opacity);
        drawingContext.FillRectangle(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(150f * opacity)));
        drawingContext.FillRectangle(bounds, new Color(13, 24, 31, (int)(235f * opacity)));
        drawingContext.DrawRectangle(bounds, 2, new Color(125, 225, 255, textAlpha));
        drawingContext.FillRectangle(new Rectangle(bounds.X, bounds.Y, bounds.Width, 4), new Color(125, 225, 255, textAlpha));

        DrawCentered(lensName, MathHelper.Lerp(hasAlias ? bounds.Y + 15f : bounds.Y + 43f, bounds.Y + 5f, compactProgress),
            MathHelper.Lerp(MathF.Min(.58f, (large.Width - 48f) / Math.Max(1f, drawingContext.MeasureText(lensName).X)),
                MathF.Min(.34f, (compact.Width - 28f) / Math.Max(1f, drawingContext.MeasureText(lensName).X)), compactProgress),
            new Color(235, 251, 255, textAlpha));
        if (hasAlias)
            DrawCentered(lensAlias, MathHelper.Lerp(bounds.Y + 51f, bounds.Y + 34f, compactProgress),
                MathHelper.Lerp(.34f, .25f, compactProgress), new Color(159, 215, 225, textAlpha));
        DrawCentered(guide, MathHelper.Lerp(bounds.Y + 82f, hasAlias ? bounds.Y + 61f : bounds.Y + 41f, compactProgress),
            hasAlias ? MathHelper.Lerp(.28f, .23f, compactProgress) : MathHelper.Lerp(.30f, .27f, compactProgress),
            new Color(255, 220, 128, textAlpha));
        drawingContext.End();

        void DrawCentered(string text, float y, float scale, Color color)
        {
            var size = drawingContext.MeasureText(text) * scale;
            drawingContext.DrawText(text, new Vector2(bounds.Center.X - size.X / 2f, y), color, scale);
        }
    }
}
