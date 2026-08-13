namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private readonly CgosMatchNotification _cgosMatchNotification = new();

    public static bool GetCgosMatchWatchNowHit(Point point, bool enabled) => CgosMatchNotification.IsWatchNowHit(point, enabled);
    public static bool GetCgosMatchWatchLaterHit(Point point, bool enabled) => CgosMatchNotification.IsWatchLaterHit(point, enabled);
    public static bool GetCgosMatchDeferredHit(Point point) => CgosMatchNotification.IsDeferredHit(point);
    public static bool GetCgosMatchDeferredBannerHit(Point point) => CgosMatchNotification.IsDeferredBannerHit(point);

    public void DrawCgosMatchNotification(Point mousePosition, bool deferred, bool finished, int secondsRemaining,
        float opacity, float buttonOpacity, bool buttonsEnabled, bool showDeferredAction)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        var message = finished ? "対局が終了しました。結果画面へ移動します。" : $"対局が始まりました。{secondsRemaining} 秒後に観戦画面へ移動します。";
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _cgosMatchNotification.Draw(mousePoint, deferred, finished, message, opacity, buttonOpacity, buttonsEnabled, showDeferredAction,
            new CgosMatchNotificationDrawingCallbacks(FillRect, DrawRect, DrawCircle, DrawDynamicOptionText, DrawFittedText));
        _spriteBatch.End();
    }
}
