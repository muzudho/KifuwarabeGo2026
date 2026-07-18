namespace KifuwarabeGo2026.Presentation;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// ［両対局者名前等コンポーネント］
/// </summary>
public sealed partial class GoScreenRenderer
{
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
        Domain.GoStone currentTurn,
        Domain.GoStone? engineErrorStone = null)
    {
        DrawPlayerComponent(new Rectangle(x, y, width, 88), blackName, blackElapsed, mainTime, blackAgehama, black: true, currentTurn == Domain.GoStone.Black, engineErrorStone == Domain.GoStone.Black);
        DrawPlayerComponent(new Rectangle(x, y + 96, width, 88), whiteName, whiteElapsed, mainTime, whiteAgehama, black: false, currentTurn == Domain.GoStone.White, engineErrorStone == Domain.GoStone.White);
    }

    private void DrawPlayerComponent(
        Rectangle bounds,
        string playerName,
        TimeSpan? elapsed,
        TimeSpan? mainTime,
        int agehama,
        bool black,
        bool active,
        bool engineError)
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
            DrawFittedText("ENGINE ERROR", new Rectangle(bounds.Right - 190, bounds.Y + 48, 172, 30), new Color(255, 96, 96), 0.34f);
        }
    }
}
