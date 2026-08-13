namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>GoScreenRenderer と未保存変更確認ページを接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public ReviewUnsavedChangesConfirmation ReviewUnsavedChangesConfirmation { get; } = new();

    public void DrawReviewUnsavedChangesConfirmation(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ReviewUnsavedChangesConfirmation.Draw(
            mousePoint,
            new ReviewUnsavedChangesConfirmationDrawingCallbacks(
                VirtualScreen.Width,
                VirtualScreen.Height,
                FillRect,
                DrawRect,
                DrawText,
                DrawFittedText,
                DrawCommandButton));
        _spriteBatch.End();
    }
}
