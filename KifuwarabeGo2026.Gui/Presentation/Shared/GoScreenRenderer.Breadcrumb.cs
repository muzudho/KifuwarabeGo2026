namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// 現在表示中の画面を示すパンくずリストを描画します。
/// </summary>
public sealed partial class GoScreenRenderer
{
    private const int BreadcrumbLeft = 24;
    private const int BreadcrumbTop = 1036;
    private const int BreadcrumbHeight = 36;

    /// <summary>
    /// 仮想画面の左下に、現在の画面階層を常時表示します。
    /// </summary>
    public void DrawBreadcrumb(string path)
    {
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        var textScale = 0.40f;
        var textWidth = (int)MathF.Ceiling(_font.MeasureString(path).X * textScale);
        var breadcrumbBounds = new Rectangle(
            BreadcrumbLeft,
            BreadcrumbTop,
            Math.Min(VirtualScreen.Width - BreadcrumbLeft * 2, textWidth + 28),
            BreadcrumbHeight);

        // 盤の下に置く、目立ちすぎない半透明の背景だけにします。
        FillRect(breadcrumbBounds, new Color(0, 0, 0, 160));
        DrawFittedText(
            path,
            new Rectangle(breadcrumbBounds.X + 14, breadcrumbBounds.Y + 5, breadcrumbBounds.Width - 28, breadcrumbBounds.Height - 10),
            new Color(225, 240, 232),
            textScale);

        _spriteBatch.End();
    }
}
