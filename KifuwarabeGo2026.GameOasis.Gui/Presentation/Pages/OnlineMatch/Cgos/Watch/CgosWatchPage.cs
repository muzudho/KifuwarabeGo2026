namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RightSidePanel;

/// <summary>CGOS 観戦・結果画面の描画と操作ボタンを所有します。</summary>
public sealed class CgosWatchPage
{
    public static CgosWatchPage Default { get; } = new();

    private CgosWatchPage()
    {
        LeaveViewButton = new Button(new Rectangle(1480, 120, 332, 52), "LEAVE VIEW", 0.38f);
        ExportSgfButton = new Button(new Rectangle(1486, 920, 306, 52), "SGF OUTPUT", 0.40f);
        ReviewButton = new Button(new Rectangle(1164, 920, 306, 52), "KIFU REVIEW", 0.36f);
        HumanPassButton = new Button(new Rectangle(1164, 930, 250, 48), "PASS", 0.38f);
        HumanResignButton = new Button(new Rectangle(1430, 930, 362, 48), "RESIGN", 0.34f);
    }

    public void Draw(CgosWatchingRenderer renderer, KfwStationeryDrawingTools drawingContext, GoAppSession session, CgosGameObservation observation, Point mousePosition) =>
        renderer.Draw(drawingContext, session, observation, mousePosition);

    public Button LeaveViewButton { get; }
    public Button ExportSgfButton { get; }
    public SgfAutoSaveCheckBox SgfAutoSaveCheckBox { get; } = new();
    public Button ReviewButton { get; }
    public Button HumanPassButton { get; }
    public Button HumanResignButton { get; }
}
