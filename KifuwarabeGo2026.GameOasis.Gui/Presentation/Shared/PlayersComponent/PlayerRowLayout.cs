namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;

/// <summary>通常／最小プレイヤー行の文字と状態表示の配置を計算します。</summary>
public readonly record struct PlayerRowLayout(Rectangle NameBounds, Rectangle StatusBounds, Rectangle LiveStatusBounds,
    int ActiveIndicatorX, int StoneCenterX, int ValueX);

public static class PlayerRowLayouts
{
    public static PlayerRowLayout Create(Rectangle bounds, bool minimal, bool hasLiveElapsed, int gameOverValueX)
    {
        var valueX = minimal ? gameOverValueX : bounds.X + 62;
        var statusX = valueX + (minimal ? 44 : -44);
        var statusWidth = bounds.Right - statusX - (minimal ? 154 : 18);
        return new PlayerRowLayout(
            new Rectangle(valueX + (minimal ? 44 : 0), bounds.Y + 5, bounds.Right - valueX - 60, 34),
            new Rectangle(statusX, bounds.Y + 43, statusWidth, hasLiveElapsed ? 20 : 30),
            new Rectangle(statusX, bounds.Y + 65, statusWidth, 18),
            minimal ? bounds.X + 34 : bounds.X,
            minimal ? valueX + 18 : bounds.X + 31,
            valueX);
    }
}
