namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

/// <summary>
/// 角丸の下線
/// </summary>
public sealed class RoundUnderlineRenderer : AbstractUnderlineRenderer
{
    public int Radius { get; set; } = 2;

    protected override void DrawCore(IUnderlineDrawingSurface surface) =>
        surface.FillRoundedRectangle(UnderlineBounds, Radius, Color);
}
