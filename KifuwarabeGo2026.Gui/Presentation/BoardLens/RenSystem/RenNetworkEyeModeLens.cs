namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>REN NETWORK EYE MODE LENS の目連マーカーを描画します。</summary>
    private void DrawRenGraphEyeMarkers(RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0)
            return;

        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;
        for (var i = 0; i < node.EyeNumbers.Count; i++)
        {
            var markerBounds = new Rectangle(
                (int)MathF.Round(startX + (i * spacing) - (markerSize * 0.5f)),
                (int)MathF.Round(startY - (markerSize * 0.5f)),
                (int)MathF.Round(markerSize),
                (int)MathF.Round(markerSize));
            FillRect(markerBounds, new Color(255, 238, 0, 245));
            DrawRect(markerBounds, 2, new Color(255, 250, 220));
            DrawRenNumber(node.EyeNumbers[i], new Vector2(markerBounds.Center.X, markerBounds.Center.Y), markerScale);
        }
    }
}
