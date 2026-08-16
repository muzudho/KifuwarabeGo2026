namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>
/// 角が四角の下線
/// </summary>
public sealed class SquareUnderline : AbstractUnderline
{
    protected override void DrawCore(KfwStationeryDrawingTools surface) =>
        surface.FillRectangle(UnderlineBounds, Color);
}
