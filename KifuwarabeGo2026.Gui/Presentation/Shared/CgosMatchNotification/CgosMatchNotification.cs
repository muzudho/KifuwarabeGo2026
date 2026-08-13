namespace KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;

using Microsoft.Xna.Framework;
using System;

/// <summary>CGOS 対局開始・終了通知のレイアウト、描画状態、当たり判定を所有します。</summary>
public sealed class CgosMatchNotification
{
    private static Rectangle BannerBounds => new(460, 28, 1000, 116);
    private static Rectangle WatchNowBounds => new(1110, 76, 154, 48);
    private static Rectangle WatchLaterBounds => new(1276, 76, 164, 48);
    private static Rectangle DeferredBounds => new(1450, 30, 410, 62);
    private static Rectangle DeferredWatchBounds => new(1680, 38, 166, 46);

    public static bool IsWatchNowHit(Point point, bool enabled) => enabled && WatchNowBounds.Contains(point);
    public static bool IsWatchLaterHit(Point point, bool enabled) => enabled && WatchLaterBounds.Contains(point);
    public static bool IsDeferredHit(Point point) => DeferredWatchBounds.Contains(point);
    public static bool IsDeferredBannerHit(Point point) => DeferredBounds.Contains(point);

    public void Draw(Point mousePoint, bool deferred, bool finished, string message, float opacity, float buttonOpacity,
        bool buttonsEnabled, bool showDeferredAction, CgosMatchNotificationDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        if (deferred) { DrawDeferred(mousePoint, finished, opacity, showDeferredAction, draw); return; }
        var alpha = (byte)(230 * Math.Clamp(opacity, 0f, 1f));
        var bounds = BannerBounds;
        draw.FillRectangle(new Rectangle(bounds.X + 8, bounds.Y + 9, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(110 * opacity)));
        draw.FillRectangle(bounds, new Color(20, 30, 35, (int)alpha));
        draw.DrawRectangle(bounds, 2, new Color(99, 223, 185, (int)(255 * opacity)));
        draw.FillRectangle(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), new Color(99, 223, 185, (int)(255 * opacity)));
        draw.DrawDynamicText(message, new Rectangle(bounds.X + 30, bounds.Y + 18, 930, 42), Color.White * opacity, 0.48f);
        DrawButton(WatchNowBounds, buttonsEnabled ? finished ? "VIEW RESULT" : "WATCH NOW" : "", mousePoint, buttonOpacity, buttonsEnabled, 0.31f, draw);
        DrawButton(WatchLaterBounds, buttonsEnabled ? "WATCH LATER" : "", mousePoint, buttonOpacity, buttonsEnabled, 0.28f, draw);
    }

    private static void DrawButton(Rectangle bounds, string text, Point mousePoint, float opacity, bool enabled, float scale, CgosMatchNotificationDrawingCallbacks draw)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        var hovered = enabled && bounds.Contains(mousePoint);
        draw.FillRectangle(bounds, hovered ? new Color(48, 77, 74, (int)(240 * opacity)) : new Color(35, 44, 52, (int)(220 * opacity)));
        draw.DrawRectangle(bounds, 2, hovered ? new Color(178, 219, 226, (int)(255 * opacity)) : new Color(99, 130, 134, (int)(255 * opacity)));
        draw.DrawFittedText(text, new Rectangle(bounds.X + 12, bounds.Y + 7, bounds.Width - 24, bounds.Height - 14), Color.White * opacity, scale);
    }

    private static void DrawDeferred(Point mousePoint, bool finished, float opacity, bool showAction, CgosMatchNotificationDrawingCallbacks draw)
    {
        var bounds = DeferredBounds;
        var hovered = bounds.Contains(mousePoint);
        draw.FillRectangle(new Rectangle(bounds.X + 6, bounds.Y + 7, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(100 * opacity)));
        draw.FillRectangle(bounds, hovered ? new Color(35, 55, 57, (int)(240 * opacity)) : new Color(20, 30, 35, (int)(225 * opacity)));
        draw.DrawRectangle(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(99, 223, 185, (int)(255 * opacity)));
        draw.DrawCircle(new Vector2(bounds.X + 27, bounds.Center.Y), 7, finished ? new Color(255, 183, 146) : new Color(99, 223, 185));
        draw.DrawDynamicText(finished ? "対局が終了しました" : "自動遷移を中断中", new Rectangle(bounds.X + 48, bounds.Y + 12, 170, 38), Color.White * opacity, 0.34f);
        if (!showAction) return;
        var actionHovered = DeferredWatchBounds.Contains(mousePoint);
        draw.FillRectangle(DeferredWatchBounds, actionHovered ? new Color(48, 77, 74, (int)(240 * opacity)) : new Color(35, 44, 52, (int)(220 * opacity)));
        draw.DrawRectangle(DeferredWatchBounds, 2, actionHovered ? new Color(178, 219, 226, (int)(255 * opacity)) : new Color(99, 130, 134, (int)(255 * opacity)));
        draw.DrawDynamicText(finished ? "結果を見る" : "対局を見る", new Rectangle(DeferredWatchBounds.X + 12, DeferredWatchBounds.Y + 7, DeferredWatchBounds.Width - 24, DeferredWatchBounds.Height - 14), Color.White * opacity, 0.31f);
    }
}

public sealed record CgosMatchNotificationDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<Vector2, float, Color> DrawCircle,
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Rectangle, Color, float> DrawFittedText);
