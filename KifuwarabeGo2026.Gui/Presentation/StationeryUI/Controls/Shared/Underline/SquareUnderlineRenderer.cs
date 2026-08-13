namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

/// <summary>
/// 角が四角の下線
/// </summary>
public sealed class SquareUnderlineRenderer : AbstractUnderlineRenderer
{
    protected override void DrawCore(IUnderlineDrawingSurface surface) =>
        surface.FillRectangle(UnderlineBounds, Color);
}
