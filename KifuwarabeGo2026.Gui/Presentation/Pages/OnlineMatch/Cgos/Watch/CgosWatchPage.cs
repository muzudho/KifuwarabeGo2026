namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

/// <summary>CGOS 観戦・結果画面の描画と操作ボタンを所有します。</summary>
public sealed class CgosWatchPage
{
    public static CgosWatchPage Default { get; } = new();

    private CgosWatchPage()
    {
        LeaveViewButton = new Button(new Rectangle(1480, 120, 332, 52), "LEAVE VIEW", 0.38f);
        ExportSgfButton = new Button(new Rectangle(1486, 920, 306, 52), "SGF OUTPUT", 0.40f);
        ReviewButton = new Button(new Rectangle(1164, 920, 306, 52), "KIFU REVIEW", 0.36f);
    }

    public void Draw(CgosWatchingRenderer renderer, KfwStationeryDrawingTools drawingContext, GoAppSession session, CgosGameObservation observation, Point mousePosition) =>
        renderer.Draw(drawingContext, session, observation, mousePosition);

    public Button LeaveViewButton { get; }
    public Button ExportSgfButton { get; }
    public SgfAutoSaveCheckBox SgfAutoSaveCheckBox { get; } = new();
    public Button ReviewButton { get; }
}
