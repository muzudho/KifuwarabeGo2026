namespace KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;

/// <summary>検討チャートポップアップとリプレイ操作のレイアウトを所有します。</summary>
public sealed class PopupTrendChartScreen
{
    public static PopupTrendChartScreen Default { get; } = new();

    private PopupTrendChartScreen()
    {
        MoveCommentPanel = new MoveCommentPanelComponent();
    }

    public MoveCommentPanelComponent MoveCommentPanel { get; }

    public Rectangle PopupBounds { get; } = new(56, 42, 1808, 1030);
    public Rectangle ChartBounds { get; } = new(100, 115, 1720, 850);
    public Rectangle CloseButtonBounds { get; } = new(1660, 55, 160, 48);
    public Rectangle BackToLiveButtonBounds { get; } = new(1026, 55, 216, 48);
    public Rectangle AutoUpdateBounds { get; } = new(1260, 55, 300, 48);
    public Rectangle SeekBounds { get; } = new(180, 994, 1560, 28);
    public Rectangle BottomNavigationControlsProximityBounds { get; } = new(150, 952, 1600, 126);
    public Rectangle ReplayEditButtonBounds { get; } = new(1018, 72, 72, 72);
    public Rectangle ReplayBackToLiveButtonBounds { get; } = new(836, 72, 170, 54);

    public Rectangle PlotBounds => new(
        ChartBounds.X + 72,
        ChartBounds.Y + 92,
        ChartBounds.Width - 144,
        ChartBounds.Height - 260);

    public Rectangle GetStepButtonBounds(int index) => new(512 + index * 112, 1028, 102, 44);
}

internal static class PopupTrendChartScreenBounds
{
    private static PopupTrendChartScreen Screen => PopupTrendChartScreen.Default;

    internal static Rectangle ReviewChartPopupBounds => Screen.PopupBounds;
    internal static Rectangle ReviewChartPopupChartBounds => Screen.ChartBounds;
    internal static Rectangle ReviewChartPopupCloseButtonBounds => Screen.CloseButtonBounds;
    internal static Rectangle ReviewChartPopupBackToLiveButtonBounds => Screen.BackToLiveButtonBounds;
    internal static Rectangle ReviewChartPopupAutoUpdateBounds => Screen.AutoUpdateBounds;
    internal static Rectangle ReviewChartPopupSeekBounds => Screen.SeekBounds;
    internal static Rectangle BottomNavigationControlsProximityBounds => Screen.BottomNavigationControlsProximityBounds;
    internal static Rectangle ReviewChartPopupPlotBounds => Screen.PlotBounds;
    internal static Rectangle PopupTrendChartMoveCommentPanelBounds => Screen.MoveCommentPanel.Bounds;
    internal static Rectangle ReplayEditButtonBounds => Screen.ReplayEditButtonBounds;
    internal static Rectangle ReplayBackToLiveButtonBounds => Screen.ReplayBackToLiveButtonBounds;
    internal static Rectangle ReviewChartPopupStepButtonBounds(int index) => Screen.GetStepButtonBounds(index);
}
