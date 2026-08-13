namespace KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;

using Microsoft.Xna.Framework;
using System;

/// <summary>ページ送り、選択項目、並べ替え操作のパネルを所有します。</summary>
public sealed class CatalogOrderNavigationPanel
{
    public void Draw(CatalogOrderNavigationModel model, CatalogOrderNavigationDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawText($"PAGES {model.FirstPageIndex + 1}-{Math.Min(model.PageCount, model.FirstPageIndex + 2)} / {model.PageCount}", new Vector2(804, 837), new Color(227, 224, 210), 0.36f);
        draw.DrawButton(CatalogOrderEditorLayout.PreviousPairButtonBounds, "PREV", model.MousePoint, model.FirstPageIndex > 0, 0.36f);
        draw.DrawButton(CatalogOrderEditorLayout.NextPairButtonBounds, "NEXT", model.MousePoint, model.FirstPageIndex < model.PageCount - 1, 0.36f);
        draw.DrawText("SELECTED", new Vector2(1370, 270), new Color(180, 195, 195), 0.36f);
        if (model.SelectedName is not null)
        {
            draw.DrawFittedText(model.SelectedName, new Rectangle(1370, 306, 244, 42), Color.White, 0.4f);
            draw.DrawFittedText(model.SelectedSummary ?? "", new Rectangle(1370, 352, 244, 52), new Color(204, 211, 206), 0.3f);
        }
        var canMoveUp = model.SelectedIndex > 0;
        var canMoveDown = model.SelectedIndex >= 0 && model.SelectedIndex < model.ItemCount - 1;
        draw.DrawButton(CatalogOrderEditorLayout.TopButtonBounds, "TO TOP", model.MousePoint, canMoveUp, 0.34f);
        draw.DrawButton(CatalogOrderEditorLayout.PageUpButtonBounds, "PAGE UP", model.MousePoint, canMoveUp, 0.34f);
        draw.DrawButton(CatalogOrderEditorLayout.UpButtonBounds, "UP", model.MousePoint, canMoveUp, 0.4f);
        draw.DrawButton(CatalogOrderEditorLayout.DownButtonBounds, "DOWN", model.MousePoint, canMoveDown, 0.4f);
        draw.DrawButton(CatalogOrderEditorLayout.PageDownButtonBounds, "PAGE DOWN", model.MousePoint, canMoveDown, 0.34f);
        draw.DrawFittedText("DRAG A CARD OR USE THE BUTTONS", new Rectangle(1370, 730, 244, 52), new Color(99, 223, 185), 0.27f);
    }
}

public sealed record CatalogOrderNavigationModel(int FirstPageIndex, int PageCount, int SelectedIndex, int ItemCount,
    string? SelectedName, string? SelectedSummary, Point MousePoint);
public sealed record CatalogOrderNavigationDrawingCallbacks(Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText, Action<Rectangle, string, Point, bool, float> DrawButton);
