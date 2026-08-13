namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private readonly Breadcrumb _breadcrumb = new();

    public void DrawBreadcrumb(string path, bool visible = true)
    {
        if (!visible) return;
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _breadcrumb.Draw(path, VirtualScreen.Width, _font.MeasureString, new BreadcrumbDrawingCallbacks(FillRect, DrawFittedText));
        _spriteBatch.End();
    }
}
