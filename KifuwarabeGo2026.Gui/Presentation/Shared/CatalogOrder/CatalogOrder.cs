namespace KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;

/// <summary>カタログ順序編集画面の操作領域とカード選択規則を所有します。</summary>
public sealed class CatalogOrder
{
    public bool IsCancelButtonHit(Point point) => CatalogOrderEditorLayout.CancelButtonBounds.Contains(point);
    public bool IsSaveButtonHit(Point point) => CatalogOrderEditorLayout.SaveButtonBounds.Contains(point);

    public int GetMoveStep(Point point, int pageSize) =>
        CatalogOrderEditorLayout.TopButtonBounds.Contains(point) ? int.MinValue :
        CatalogOrderEditorLayout.PageUpButtonBounds.Contains(point) ? -pageSize :
        CatalogOrderEditorLayout.UpButtonBounds.Contains(point) ? -1 :
        CatalogOrderEditorLayout.DownButtonBounds.Contains(point) ? 1 :
        CatalogOrderEditorLayout.PageDownButtonBounds.Contains(point) ? pageSize : 0;

    public int GetPageStep(Point point) =>
        CatalogOrderEditorLayout.PreviousPairButtonBounds.Contains(point) ? -1 :
        CatalogOrderEditorLayout.NextPairButtonBounds.Contains(point) ? 1 : 0;

    public int? GetCardHit<T>(Point point, CatalogOrderEditor<T> editor)
    {
        var startIndex = editor.FirstVisiblePageIndex * editor.PageSize;
        for (var visibleIndex = 0; visibleIndex < editor.PageSize * 2; visibleIndex++)
        {
            if (!CatalogOrderEditorLayout.CardBounds(visibleIndex, editor.PageSize).Contains(point)) continue;
            var index = startIndex + visibleIndex;
            return index < editor.Items.Count ? index : null;
        }
        return null;
    }
}
