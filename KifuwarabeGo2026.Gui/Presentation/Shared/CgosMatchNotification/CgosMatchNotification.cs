namespace KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;

/// <summary>CGOS 対局開始・終了通知のレイアウト、描画状態、当たり判定を所有します。</summary>
public sealed class CgosMatchNotification
{
    public static CgosMatchNotification Default { get; } = new();

    private static Rectangle BannerBounds => new(460, 28, 1000, 116);
    private static Rectangle DeferredBounds => new(1450, 30, 410, 62);

    private CgosMatchNotification()
    {
        WatchNowButton = new Button(new Rectangle(1110, 76, 154, 48), "WATCH NOW", 0.31f);
        WatchLaterButton = new Button(new Rectangle(1276, 76, 164, 48), "WATCH LATER", 0.28f);
        DeferredWatchButton = new Button(new Rectangle(1680, 38, 166, 46), "対局を見る", 0.31f);
    }

    public Button WatchNowButton { get; }
    public Button WatchLaterButton { get; }
    public Button DeferredWatchButton { get; }

    public static bool IsDeferredBannerHit(Point point) => DeferredBounds.Contains(point);

    /// <summary>物理座標を受け取り、CGOS 対局通知の描画サイクル全体を実行します。</summary>
    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePosition, bool deferred, bool finished,
        int secondsRemaining, float opacity, float buttonOpacity, bool buttonsEnabled, bool showDeferredAction)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        var message = finished
            ? "対局が終了しました。結果画面へ移動します。"
            : $"対局が始まりました。{secondsRemaining} 秒後に観戦画面へ移動します。";

        drawingContext.Begin();
        Draw(mousePoint, deferred, finished, message, opacity, buttonOpacity, buttonsEnabled, showDeferredAction,
            new CgosMatchNotificationDrawingCallbacks(
                drawingContext.FillRectangle,
                drawingContext.DrawRectangle,
                drawingContext.DrawCircle,
                drawingContext.DrawDynamicText,
                drawingContext.DrawFittedText));
        drawingContext.End();
    }

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
        WatchNowButton.Label = finished ? "VIEW RESULT" : "WATCH NOW";
        WatchNowButton.IsEnabled = buttonsEnabled;
        WatchLaterButton.IsEnabled = buttonsEnabled;
        DrawButton(WatchNowButton, buttonsEnabled ? WatchNowButton.Label : "", mousePoint, buttonOpacity, draw);
        DrawButton(WatchLaterButton, buttonsEnabled ? WatchLaterButton.Label : "", mousePoint, buttonOpacity, draw);
    }

    private static void DrawButton(Button button, string text, Point mousePoint, float opacity, CgosMatchNotificationDrawingCallbacks draw)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        var bounds = button.Bounds;
        var hovered = button.IsHit(mousePoint);
        draw.FillRectangle(bounds, hovered ? new Color(48, 77, 74, (int)(240 * opacity)) : new Color(35, 44, 52, (int)(220 * opacity)));
        draw.DrawRectangle(bounds, 2, hovered ? new Color(178, 219, 226, (int)(255 * opacity)) : new Color(99, 130, 134, (int)(255 * opacity)));
        draw.DrawFittedText(text, new Rectangle(bounds.X + 12, bounds.Y + 7, bounds.Width - 24, bounds.Height - 14), Color.White * opacity, button.LabelScale);
    }

    private void DrawDeferred(Point mousePoint, bool finished, float opacity, bool showAction, CgosMatchNotificationDrawingCallbacks draw)
    {
        var bounds = DeferredBounds;
        var hovered = bounds.Contains(mousePoint);
        draw.FillRectangle(new Rectangle(bounds.X + 6, bounds.Y + 7, bounds.Width, bounds.Height), new Color(0, 0, 0, (int)(100 * opacity)));
        draw.FillRectangle(bounds, hovered ? new Color(35, 55, 57, (int)(240 * opacity)) : new Color(20, 30, 35, (int)(225 * opacity)));
        draw.DrawRectangle(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(99, 223, 185, (int)(255 * opacity)));
        draw.DrawCircle(new Vector2(bounds.X + 27, bounds.Center.Y), 7, finished ? new Color(255, 183, 146) : new Color(99, 223, 185));
        draw.DrawDynamicText(finished ? "対局が終了しました" : "自動遷移を中断中", new Rectangle(bounds.X + 48, bounds.Y + 12, 170, 38), Color.White * opacity, 0.34f);
        if (!showAction) return;
        DeferredWatchButton.Label = finished ? "結果を見る" : "対局を見る";
        DrawButton(DeferredWatchButton, DeferredWatchButton.Label, mousePoint, opacity, draw);
    }
}

public sealed record CgosMatchNotificationDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<Vector2, float, Color> DrawCircle,
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Rectangle, Color, float> DrawFittedText);
