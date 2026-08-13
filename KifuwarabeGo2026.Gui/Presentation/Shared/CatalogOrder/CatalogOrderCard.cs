namespace KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;

using Microsoft.Xna.Framework;
using System;

/// <summary>カタログ順序編集画面に並ぶ1件分のカード表示を所有します。</summary>
public sealed class CatalogOrderCard
{
    public void Draw<T>(CatalogOrderCardModel<T> model, CatalogOrderCardDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var background = model.IsDragged ? new Color(57, 82, 118) : model.IsSelected ? new Color(38, 103, 86) : model.IsHovered ? new Color(43, 52, 62) : new Color(24, 31, 37);
        var border = model.IsDragged ? new Color(176, 194, 242) : model.IsSelected ? new Color(147, 244, 200) : new Color(70, 85, 94);
        draw.FillRectangle(model.Bounds, background);
        draw.DrawRectangle(model.Bounds, model.IsDragged ? 3 : 1, border);
        draw.DrawText($"{model.DisplayIndex:00}", new Vector2(model.Bounds.X + 12, model.Bounds.Y + 14), model.IsSelected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.36f);
        draw.DrawFittedText(model.GetName(model.Item), new Rectangle(model.Bounds.X + 58, model.Bounds.Y + 4, model.Bounds.Width - 70, 30), Color.White, 0.39f);
        var isComputer = model.GetComputerRole?.Invoke(model.Item);
        if (isComputer is { } computer)
        {
            draw.DrawPlayerRoleFaceIcon(new Vector2(model.Bounds.X + 72, model.Bounds.Y + 49), computer);
            draw.DrawFittedText(model.GetSummary(model.Item), new Rectangle(model.Bounds.X + 94, model.Bounds.Y + 36, model.Bounds.Width - 106, 24), new Color(204, 211, 206), 0.28f);
            return;
        }
        draw.DrawFittedText(model.GetSummary(model.Item), new Rectangle(model.Bounds.X + 58, model.Bounds.Y + 36, model.Bounds.Width - 70, 24), new Color(204, 211, 206), 0.28f);
    }
}

public sealed record CatalogOrderCardModel<T>(T Item, int DisplayIndex, Rectangle Bounds, bool IsSelected, bool IsDragged,
    bool IsHovered, Func<T, string> GetName, Func<T, string> GetSummary, Func<T, bool?>? GetComputerRole);

public sealed record CatalogOrderCardDrawingCallbacks(Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle, Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText, Action<Vector2, bool> DrawPlayerRoleFaceIcon);
