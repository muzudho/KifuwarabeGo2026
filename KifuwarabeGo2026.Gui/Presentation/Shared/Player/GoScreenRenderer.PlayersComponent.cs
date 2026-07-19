namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// ［両対局者名前等コンポーネント］
/// </summary>
public sealed partial class GoScreenRenderer
{
    public static bool GetEngineErrorLogHit(Point point, GoAppSession session)
    {
        if (session.CurrentMode.Kind != GoAppModeKind.Playing || session.EngineErrorStone is not { } errorStone)
        {
            return false;
        }

        var playerBounds = errorStone == GoStone.Black
            ? new Rectangle(1144, 348, 668, 88)
            : new Rectangle(1144, 444, 668, 88);
        return PlayerEngineErrorBounds(playerBounds).Contains(point);
    }

    /// <summary>
    /// 黒番と白番の名前、時間、アゲハマを共通レイアウトで描画します。
    /// </summary>
    private void DrawBothPlayersComponent(
        int x,
        int y,
        int width,
        string blackName,
        string whiteName,
        TimeSpan? blackElapsed,
        TimeSpan? whiteElapsed,
        TimeSpan? mainTime,
        int blackAgehama,
        int whiteAgehama,
        GoStone currentTurn,
        GoStone? engineErrorStone = null,
        Point? mousePoint = null)
    {
        DrawPlayerComponent(new Rectangle(x, y, width, 88), blackName, blackElapsed, mainTime, blackAgehama, black: true, currentTurn == GoStone.Black, engineErrorStone == GoStone.Black, mousePoint);
        DrawPlayerComponent(new Rectangle(x, y + 96, width, 88), whiteName, whiteElapsed, mainTime, whiteAgehama, black: false, currentTurn == GoStone.White, engineErrorStone == GoStone.White, mousePoint);
    }

    private void DrawPlayerComponent(
        Rectangle bounds,
        string playerName,
        TimeSpan? elapsed,
        TimeSpan? mainTime,
        int agehama,
        bool black,
        bool active,
        bool engineError,
        Point? mousePoint)
    {
        DrawDataRowFrame(bounds);
        if (active) FillRect(new Rectangle(bounds.X, bounds.Y + 2, 6, bounds.Height - 4), new Color(99, 223, 185));
        var nameBounds = new Rectangle(bounds.X + 62, bounds.Y + 5, bounds.Width - 78, 34);
        var statusBounds = new Rectangle(bounds.X + 18, bounds.Y + 48, bounds.Width - 36, 30);
        DrawStone(new Vector2(bounds.X + 31, bounds.Y + 23), 16, black);
        DrawFittedText(playerName, nameBounds, Color.White, 0.5f);

        var elapsedText = elapsed is { } used ? FormatElapsedTime(used) : "--:--";
        var mainTimeText = mainTime is { } limit ? FormatElapsedTime(limit) : "--:--";
        DrawFittedText($"USED {elapsedText} / LIMIT {mainTimeText}    AGEHAMA {agehama}", statusBounds, new Color(204, 211, 206), 0.34f);
        if (engineError)
        {
            var errorLogBounds = PlayerEngineErrorBounds(bounds);
            var hovered = mousePoint is { } point && errorLogBounds.Contains(point);
            FillRect(errorLogBounds, hovered ? new Color(104, 34, 38, 220) : new Color(57, 29, 34, 210));
            DrawRect(errorLogBounds, 1, new Color(255, 96, 96));
            DrawFittedText("ERROR LOG", new Rectangle(errorLogBounds.X + 10, errorLogBounds.Y + 4, errorLogBounds.Width - 20, errorLogBounds.Height - 8), new Color(255, 126, 126), 0.34f);
        }
    }

    private static Rectangle PlayerEngineErrorBounds(Rectangle playerBounds) =>
        new(playerBounds.Right - 190, playerBounds.Y + 48, 172, 30);
}
