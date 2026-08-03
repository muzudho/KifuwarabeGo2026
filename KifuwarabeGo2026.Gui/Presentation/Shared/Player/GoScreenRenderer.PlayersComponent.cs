namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
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
            ? new Rectangle(1144, PlayingPlayersY, 668, 88)
            : new Rectangle(1144, PlayingPlayersY + 96, 668, 88);
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
        Point? mousePoint = null,
        bool minimal = false,
        TimeSpan? blackLiveElapsed = null,
        TimeSpan? whiteLiveElapsed = null)
    {
        DrawPlayerComponent(new Rectangle(x, y, width, 88), blackName, blackElapsed, blackLiveElapsed, mainTime, blackAgehama, black: true, currentTurn == GoStone.Black, engineErrorStone == GoStone.Black, mousePoint, minimal);
        DrawPlayerComponent(new Rectangle(x, y + 96, width, 88), whiteName, whiteElapsed, whiteLiveElapsed, mainTime, whiteAgehama, black: false, currentTurn == GoStone.White, engineErrorStone == GoStone.White, mousePoint, minimal);
    }

    private void DrawPlayerComponent(
        Rectangle bounds,
        string playerName,
        TimeSpan? elapsed,
        TimeSpan? liveElapsed,
        TimeSpan? mainTime,
        int agehama,
        bool black,
        bool active,
        bool engineError,
        Point? mousePoint,
        bool minimal)
    {
        if (!minimal) DrawDataRowFrame(bounds);
        var activeX = minimal ? bounds.X + 34 : bounds.X;
        if (active) FillRect(new Rectangle(activeX, bounds.Y + 2, 4, bounds.Height - 4), new Color(99, 223, 185));
        var valueX = minimal ? GameOverValueX : bounds.X + 62;
        var nameBounds = new Rectangle(valueX + (minimal ? 44 : 0), bounds.Y + 5, bounds.Right - valueX - 60, 34);
        var statusX = valueX + (minimal ? 44 : -44);
        var statusWidth = bounds.Right - statusX - (minimal ? 154 : 18);
        var statusBounds = new Rectangle(statusX, bounds.Y + 43, statusWidth, liveElapsed is null ? 30 : 20);
        if (minimal) DrawIconStone(new Vector2(valueX + 18, bounds.Y + 23), 16, black);
        else DrawStone(new Vector2(bounds.X + 31, bounds.Y + 23), 16, black);
        DrawFittedText(playerName, nameBounds, Color.White, 0.5f);

        var elapsedText = elapsed is { } used ? FormatElapsedTime(used) : "--:--";
        var mainTimeText = mainTime is { } limit ? FormatElapsedTime(limit) : "--:--";
        var statusText = minimal
            ? $"USED {elapsedText} / LIMIT {mainTimeText}"
            : $"USED {elapsedText} / LIMIT {mainTimeText}    AGEHAMA {agehama}";
        DrawFittedText(statusText, statusBounds, new Color(204, 211, 206), 0.34f);
        if (liveElapsed is { } currentElapsed)
        {
            DrawFittedText(
                $"NOW  {FormatElapsedTime(currentElapsed)}",
                new Rectangle(statusX, bounds.Y + 65, statusWidth, 18),
                active ? new Color(147, 244, 200) : new Color(158, 178, 178),
                0.30f);
        }
        if (minimal)
        {
            DrawAgehamaPlate(new Rectangle(bounds.Right - 136, bounds.Y + 43, 118, 38), agehama, capturedBlack: !black);
        }
        if (engineError)
        {
            var errorLogBounds = PlayerEngineErrorBounds(bounds);
            var hovered = mousePoint is { } point && errorLogBounds.Contains(point);
            FillRect(errorLogBounds, hovered ? new Color(104, 34, 38, 220) : new Color(57, 29, 34, 210));
            DrawRect(errorLogBounds, 1, new Color(255, 96, 96));
            DrawFittedText("ERROR LOG", new Rectangle(errorLogBounds.X + 10, errorLogBounds.Y + 4, errorLogBounds.Width - 20, errorLogBounds.Height - 8), new Color(255, 126, 126), 0.34f);
        }
    }

    private void DrawAgehamaPlate(Rectangle bounds, int agehama, bool capturedBlack)
    {
        _spriteBatch.Draw(_softCircle, bounds, new Color(91, 55, 31));
        _spriteBatch.Draw(
            _softCircle,
            new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 11),
            new Color(145, 92, 48));
        DrawStone(new Vector2(bounds.X + 30, bounds.Center.Y - 1), 12, capturedBlack);
        DrawFittedText(
            agehama.ToString(),
            new Rectangle(bounds.X + 53, bounds.Y + 5, bounds.Width - 62, bounds.Height - 10),
            Color.White,
            0.50f);
    }

    private static Rectangle PlayerEngineErrorBounds(Rectangle playerBounds) =>
        new(playerBounds.Right - 190, playerBounds.Y + 48, 172, 30);
}
