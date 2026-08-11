namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    private static readonly BoardLensButtonStrip LocalPlayingBoardLensButtons = new(1516, 800);

    /// <summary>Local Match の Board Lens 操作ボタンの押下を取得します。</summary>
    internal static BoardLensButton? GetLocalPlayingBoardLensButtonHit(Point point, bool isLensEnabled) =>
        LocalPlayingBoardLensButtons.GetHit(point, isLensEnabled);

    private void DrawLocalPlayingBoardLensButtonStrip(bool isLensEnabled, Point mousePoint)
    {
        DrawFittedText(
            "BOARD LENS  [L] / [J] / [K] / [1]",
            new Rectangle(1164, 812, 316, 36),
            new Color(147, 201, 190),
            0.26f);
        DrawBoardLensButtonStrip(LocalPlayingBoardLensButtons, isLensEnabled, mousePoint, 0.32f);
    }

    /// <summary>各画面で再利用する Board Lens 操作ボタンの描画です。</summary>
    private void DrawBoardLensButtonStrip(
        BoardLensButtonStrip buttons,
        bool isLensEnabled,
        Point mousePoint,
        float scale = 0.32f)
    {
        DrawCommandButton(buttons.ToggleBounds, "L", isLensEnabled, mousePoint, scale: scale);
        DrawCommandButton(buttons.PreviousBounds, "<J", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.82f);
        DrawCommandButton(buttons.NextBounds, "K>", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.82f);
        DrawCommandButton(buttons.ExitBounds, "OFF/1", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.66f);
    }
}
