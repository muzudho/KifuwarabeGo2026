namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;
using System;

/// <summary>手番中のプレイヤー行に表示するアクセントバーです。</summary>
public sealed class PlayerTurnIndicator
{
    public void Draw(int x, Rectangle rowBounds, bool active, Action<Rectangle, Color> fillRectangle)
    {
        ArgumentNullException.ThrowIfNull(fillRectangle);
        if (active) fillRectangle(new Rectangle(x, rowBounds.Y + 2, 4, rowBounds.Height - 4), new Color(99, 223, 185));
    }
}
