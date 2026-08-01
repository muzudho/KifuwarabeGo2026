namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private static Rectangle CgosMatchBannerBounds => new(460, 28, 1000, 116);
    private static Rectangle CgosMatchWatchNowBounds => new(1110, 76, 154, 48);
    private static Rectangle CgosMatchWatchLaterBounds => new(1276, 76, 164, 48);
    private static Rectangle CgosMatchDeferredBounds => new(1450, 30, 410, 62);
    private static Rectangle CgosMatchDeferredWatchBounds => new(1680, 38, 166, 46);

    public static bool GetCgosMatchWatchNowHit(Point point, bool enabled) =>
        enabled && CgosMatchWatchNowBounds.Contains(point);

    public static bool GetCgosMatchWatchLaterHit(Point point, bool enabled) =>
        enabled && CgosMatchWatchLaterBounds.Contains(point);

    public static bool GetCgosMatchDeferredHit(Point point) =>
        CgosMatchDeferredWatchBounds.Contains(point);

    public static bool GetCgosMatchDeferredBannerHit(Point point) =>
        CgosMatchDeferredBounds.Contains(point);

    public void DrawCgosMatchNotification(
        Point mousePosition,
        bool deferred,
        bool finished,
        int secondsRemaining,
        float opacity,
        float buttonOpacity,
        bool buttonsEnabled)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        if (deferred)
        {
            DrawDeferredMatchNotification(mousePoint, finished, opacity);
        }
        else
        {
            var alpha = (byte)(230 * MathHelper.Clamp(opacity, 0f, 1f));
            var bounds = CgosMatchBannerBounds;
            FillRect(new Rectangle(bounds.X + 8, bounds.Y + 9, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(110 * opacity)));
            FillRect(bounds, new Color(20, 30, 35, (int)alpha));
            DrawRect(bounds, 2, new Color(99, 223, 185, (int)(255 * opacity)));
            FillRect(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), new Color(99, 223, 185, (int)(255 * opacity)));

            var message = finished
                ? "対局が終了しました。結果画面へ遷移します"
                : $"対局が付きました。{secondsRemaining} 秒後に観戦画面へ遷移します";
            DrawDynamicOptionText(message, new Rectangle(bounds.X + 30, bounds.Y + 18, 930, 42), Color.White * opacity, 0.48f);

            DrawMatchNotificationButton(CgosMatchWatchNowBounds, buttonsEnabled ? finished ? "VIEW RESULT" : "WATCH NOW" : "", mousePoint, buttonOpacity, buttonsEnabled, 0.31f);
            DrawMatchNotificationButton(CgosMatchWatchLaterBounds, buttonsEnabled ? "WATCH LATER" : "", mousePoint, buttonOpacity, buttonsEnabled, 0.28f);
        }

        _spriteBatch.End();
    }

    private void DrawMatchNotificationButton(Rectangle bounds, string text, Point mousePoint, float opacity, bool enabled, float scale)
    {
        opacity = MathHelper.Clamp(opacity, 0f, 1f);
        var hovered = enabled && bounds.Contains(mousePoint);
        FillRect(bounds, hovered
            ? new Color(48, 77, 74, (int)(240 * opacity))
            : new Color(35, 44, 52, (int)(220 * opacity)));
        DrawRect(bounds, 2, hovered
            ? new Color(178, 219, 226, (int)(255 * opacity))
            : new Color(99, 130, 134, (int)(255 * opacity)));
        DrawFittedText(text, new Rectangle(bounds.X + 12, bounds.Y + 7, bounds.Width - 24, bounds.Height - 14), Color.White * opacity, scale);
    }

    private void DrawDeferredMatchNotification(Point mousePoint, bool finished, float opacity)
    {
        var bounds = CgosMatchDeferredBounds;
        var hovered = bounds.Contains(mousePoint);
        FillRect(new Rectangle(bounds.X + 6, bounds.Y + 7, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(100 * opacity)));
        FillRect(bounds, hovered ? new Color(35, 55, 57, (int)(240 * opacity)) : new Color(20, 30, 35, (int)(225 * opacity)));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(99, 223, 185, (int)(255 * opacity)));
        DrawCircle(new Vector2(bounds.X + 27, bounds.Center.Y), 7, finished ? new Color(255, 183, 146) : new Color(99, 223, 185));
        DrawDynamicOptionText(
            finished ? "対局が終了しました" : "自動遷移を中断中",
            new Rectangle(bounds.X + 48, bounds.Y + 12, 170, 38),
            Color.White * opacity,
            0.34f);
        var watchButtonBounds = CgosMatchDeferredWatchBounds;
        var watchButtonHovered = watchButtonBounds.Contains(mousePoint);
        FillRect(watchButtonBounds, watchButtonHovered
            ? new Color(48, 77, 74, (int)(240 * opacity))
            : new Color(35, 44, 52, (int)(220 * opacity)));
        DrawRect(watchButtonBounds, 2, watchButtonHovered
            ? new Color(178, 219, 226, (int)(255 * opacity))
            : new Color(99, 130, 134, (int)(255 * opacity)));
        DrawDynamicOptionText(
            finished ? "結果を見る" : "対局を観る",
            new Rectangle(watchButtonBounds.X + 12, watchButtonBounds.Y + 7, watchButtonBounds.Width - 24, watchButtonBounds.Height - 14),
            Color.White * opacity,
            0.31f);
    }
}
