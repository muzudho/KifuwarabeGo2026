namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;

/// <summary>
/// 角丸の下線
/// </summary>
public sealed class RoundUnderline : AbstractUnderline
{
    /// <summary>
    /// 角丸の半径
    /// </summary>
    public int Radius { get; set; } = 2;

    protected override void DrawCore(KfwStationeryDrawingTools surface) =>
        surface.FillRoundedRectangle(UnderlineBounds, Radius, Color);
}
