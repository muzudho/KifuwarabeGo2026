namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;

/// <summary>
/// XXX: 未整理。
/// どちらかというと、下線というより、GoScreenRenderer を拡張するソースコード。
/// </summary>
public sealed partial class GoScreenRenderer : IUnderlineDrawingSurface
{
    void IUnderlineDrawingSurface.FillRectangle(Rectangle bounds, Color color) => FillRect(bounds, color);
    void IUnderlineDrawingSurface.FillRoundedRectangle(Rectangle bounds, int radius, Color color) => DrawRoundedFill(bounds, radius, color);
    void IUnderlineDrawingSurface.DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => DrawLine(start, end, thickness, color);
}
