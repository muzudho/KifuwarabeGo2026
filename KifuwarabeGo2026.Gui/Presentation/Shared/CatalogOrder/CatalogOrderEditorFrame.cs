namespace KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;

using Microsoft.Xna.Framework;
using System;

/// <summary>カタログ順序編集モーダルの幕、見出し、編集領域の枠を所有します。</summary>
public sealed class CatalogOrderEditorFrame
{
    public void Draw(string title, Point mousePoint, CatalogOrderEditorFrameDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var bounds = CatalogOrderEditorLayout.Bounds;
        draw.FillRectangle(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        draw.FillRectangle(new Rectangle(bounds.X + 18, bounds.Y + 20, bounds.Width, bounds.Height), new Color(0, 0, 0, 155));
        draw.FillRectangle(bounds, new Color(19, 24, 31, 252));
        draw.DrawRectangle(bounds, 2, new Color(116, 145, 146));
        draw.DrawText($"{title} - EDIT ORDER", new Vector2(bounds.X + 30, bounds.Y + 24), new Color(244, 238, 218), 0.68f);
        draw.DrawCommandButton(CatalogOrderEditorLayout.CancelButtonBounds, "DISCARD", false, mousePoint, true, 0.28f);
        draw.DrawCommandButton(CatalogOrderEditorLayout.SaveButtonBounds, "SAVE & CLOSE", false, mousePoint, true, 0.23f);
        draw.FillRectangle(CatalogOrderEditorLayout.BoardBounds, new Color(15, 20, 26));
        draw.DrawRectangle(CatalogOrderEditorLayout.BoardBounds, 1, new Color(67, 84, 92));
        draw.FillRectangle(CatalogOrderEditorLayout.PropertyBounds, new Color(15, 20, 26));
        draw.DrawRectangle(CatalogOrderEditorLayout.PropertyBounds, 1, new Color(67, 84, 92));
    }
}

public sealed record CatalogOrderEditorFrameDrawingCallbacks(Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle, Action<string, Vector2, Color, float> DrawText,
    Action<Rectangle, string, bool, Point, bool, float> DrawCommandButton);
