namespace KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ChartAxisSectionLabel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.Shared.SeekButtonStrip;

/// <summary>検討チャートポップアップとリプレイ操作のレイアウトを所有します。</summary>
public sealed class PopupTrendChartScreen
{
    public static PopupTrendChartScreen Default { get; } = new();

    private PopupTrendChartScreen()
    {
        MoveCommentPanel = new MoveCommentPanelComponent();
        ScoreAxisSectionLabel = new(new Rectangle(400, 55, 150, 48), "SCORE", ChartAxisSide.Left);
        WinRateAxisSectionLabel = new(new Rectangle(558, 55, 190, 48), "WIN RATE", ChartAxisSide.Right);
        CloseButton = new Button(new Rectangle(1660, 55, 160, 48), "CLOSE", 0.38f);
        BackToLiveButton = new Button(new Rectangle(1026, 55, 216, 48), "BACK TO LIVE", 0.34f);
        ReplayBackToLiveButton = new Button(new Rectangle(836, 72, 170, 54), "BACK TO LIVE", 0.30f);
        SeekButtonStrip = new SeekButtonStripComponent(index => new Rectangle(512 + index * 112, 1028, 102, 44));
    }

    public MoveCommentPanelComponent MoveCommentPanel { get; }
    public ChartAxisSectionLabelComponent ScoreAxisSectionLabel { get; }
    public ChartAxisSectionLabelComponent WinRateAxisSectionLabel { get; }

    public Rectangle PopupBounds { get; } = new(56, 42, 1808, 1030);
    public Rectangle ChartBounds { get; } = new(100, 115, 1720, 850);
    public Button CloseButton { get; }
    public Button BackToLiveButton { get; }
    public Rectangle AutoUpdateBounds { get; } = new(1260, 55, 300, 48);
    public Rectangle SeekBounds { get; } = new(180, 994, 1560, 28);
    public Rectangle BottomNavigationControlsProximityBounds { get; } = new(150, 952, 1600, 126);
    public Rectangle ReplayEditButtonBounds { get; } = new(1018, 72, 72, 72);
    public Button ReplayBackToLiveButton { get; }
    public SeekButtonStripComponent SeekButtonStrip { get; }

    public Rectangle PlotBounds => new(
        ChartBounds.X + 72,
        ChartBounds.Y + 92,
        ChartBounds.Width - 144,
        ChartBounds.Height - 260);

}

internal static class PopupTrendChartScreenBounds
{
    private static PopupTrendChartScreen Screen => PopupTrendChartScreen.Default;

    internal static Rectangle ReviewChartPopupBounds => Screen.PopupBounds;
    internal static Rectangle ReviewChartPopupChartBounds => Screen.ChartBounds;
    internal static Rectangle ReviewChartPopupCloseButtonBounds => Screen.CloseButton.Bounds;
    internal static Rectangle ReviewChartPopupBackToLiveButtonBounds => Screen.BackToLiveButton.Bounds;
    internal static Rectangle ReviewChartPopupAutoUpdateBounds => Screen.AutoUpdateBounds;
    internal static Rectangle ReviewChartPopupSeekBounds => Screen.SeekBounds;
    internal static Rectangle BottomNavigationControlsProximityBounds => Screen.BottomNavigationControlsProximityBounds;
    internal static Rectangle ReviewChartPopupPlotBounds => Screen.PlotBounds;
    internal static Rectangle PopupTrendChartMoveCommentPanelBounds => Screen.MoveCommentPanel.Bounds;
    internal static Rectangle ReplayEditButtonBounds => Screen.ReplayEditButtonBounds;
    internal static Rectangle ReplayBackToLiveButtonBounds => Screen.ReplayBackToLiveButton.Bounds;
}
