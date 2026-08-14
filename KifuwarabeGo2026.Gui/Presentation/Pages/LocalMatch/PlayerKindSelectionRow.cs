namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>一方のプレイヤー種別と、人間プレイヤー名の入力領域を所有します。</summary>
public sealed class PlayerKindSelectionRow
{
    public PlayerKindSelectionRow(int playerKindY)
    {
        HumanButton = new Button(new Rectangle(1328, playerKindY, 236, 52), "HUMAN", 0.52f);
        ComputerButton = new Button(new Rectangle(1564, playerKindY, 236, 52), "COMPUTER", 0.52f);
        SegmentBounds = new Rectangle(1328, playerKindY, 472, 52);
        HumanNameRowBounds = new Rectangle(1144, playerKindY + 54, 668, 44);
        HumanNameTextBounds = new Rectangle(1328, playerKindY + 60, 468, 32);
    }

    public Button HumanButton { get; }
    public Button ComputerButton { get; }
    public Rectangle SegmentBounds { get; }
    public Rectangle HumanNameRowBounds { get; }
    public Rectangle HumanNameTextBounds { get; }

    public GoPlayerKind? GetPlayerKindHit(Point point) =>
        HumanButton.IsHit(point) ? GoPlayerKind.Human :
        ComputerButton.IsHit(point) ? GoPlayerKind.Computer : null;
}
