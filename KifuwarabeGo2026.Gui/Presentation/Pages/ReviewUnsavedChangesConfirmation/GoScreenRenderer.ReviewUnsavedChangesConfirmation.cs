namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private static Rectangle ReviewUnsavedChangesDialogBounds => new(570, 370, 780, 340);
    private static Rectangle ReviewUnsavedChangesSaveButtonBounds => new(650, 612, 210, 54);
    private static Rectangle ReviewUnsavedChangesDiscardButtonBounds => new(885, 612, 210, 54);
    private static Rectangle ReviewUnsavedChangesCancelButtonBounds => new(1120, 612, 150, 54);

    public static bool GetReviewUnsavedChangesSaveButtonHit(Point point) => ReviewUnsavedChangesSaveButtonBounds.Contains(point);
    public static bool GetReviewUnsavedChangesDiscardButtonHit(Point point) => ReviewUnsavedChangesDiscardButtonBounds.Contains(point);
    public static bool GetReviewUnsavedChangesCancelButtonHit(Point point) => ReviewUnsavedChangesCancelButtonBounds.Contains(point);

    public void DrawReviewUnsavedChangesConfirmation(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 165));
        FillRect(ReviewUnsavedChangesDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(ReviewUnsavedChangesDialogBounds, 2, new Color(255, 183, 146));
        DrawText("UNSAVED COMMENTS", new Vector2(ReviewUnsavedChangesDialogBounds.X + 34, ReviewUnsavedChangesDialogBounds.Y + 30), new Color(255, 230, 160), 0.64f);
        DrawFittedText("Comments have not been written to an SGF file.", new Rectangle(ReviewUnsavedChangesDialogBounds.X + 34, ReviewUnsavedChangesDialogBounds.Y + 112, 700, 38), Color.White, 0.44f);
        DrawFittedText("Save before leaving this review?", new Rectangle(ReviewUnsavedChangesDialogBounds.X + 34, ReviewUnsavedChangesDialogBounds.Y + 160, 700, 36), new Color(180, 195, 195), 0.40f);
        DrawCommandButton(ReviewUnsavedChangesSaveButtonBounds, "SAVE SGF", false, mousePoint, scale: 0.34f);
        DrawCommandButton(ReviewUnsavedChangesDiscardButtonBounds, "DON'T SAVE", false, mousePoint, scale: 0.28f);
        DrawCommandButton(ReviewUnsavedChangesCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        _spriteBatch.End();
    }
}
