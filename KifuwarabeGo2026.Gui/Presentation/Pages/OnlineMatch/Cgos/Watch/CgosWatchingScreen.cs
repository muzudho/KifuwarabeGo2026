namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>CGOS対局の観戦・結果画面に属する操作UIを所有します。</summary>
public sealed class CgosWatchingScreen
{
    public static CgosWatchingScreen Default { get; } = new();

    private CgosWatchingScreen()
    {
        LeaveViewButton = new Button(new Rectangle(1480, 120, 332, 52), "LEAVE VIEW", 0.38f);
        ExportSgfButton = new Button(new Rectangle(1486, 920, 306, 52), "SGF OUTPUT", 0.40f);
        ReviewButton = new Button(new Rectangle(1164, 920, 306, 52), "KIFU REVIEW", 0.36f);
    }

    public Button LeaveViewButton { get; }
    public Button ExportSgfButton { get; }
    public Button ReviewButton { get; }
}
