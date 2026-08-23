namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.CatalogOrder;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.CatalogOrder;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;

public sealed class CatalogOrderPresenter
{
    public static CatalogOrderPresenter Default { get; } = new();

    private CatalogOrderPresenter()
    {
    }
    private static readonly CatalogOrder CatalogOrderControl = new();
    private readonly CatalogOrderCard _catalogOrderCard = new();
    private readonly CatalogOrderEditorFrame _catalogOrderEditorFrame = new();
    private readonly CatalogOrderPageHeader _catalogOrderPageHeader = new();
    private readonly CatalogOrderNavigationPanel _catalogOrderNavigationPanel = new();

    public static bool GetCatalogOrderCancelButtonHit(Point point) =>
        CatalogOrderControl.IsCancelButtonHit(point);

    public static bool GetCatalogOrderSaveButtonHit(Point point) =>
        CatalogOrderControl.IsSaveButtonHit(point);

    public static int GetCatalogOrderMoveStep(Point point, int pageSize) =>
        CatalogOrderControl.GetMoveStep(point, pageSize);

    public static int GetCatalogOrderPageStep(Point point) =>
        CatalogOrderControl.GetPageStep(point);

    public static int? GetCatalogOrderCardHit<T>(Point point, CatalogOrderEditor<T> editor)
    {
        return CatalogOrderControl.GetCardHit(point, editor);
    }

    public void Draw<T>(KfwStationeryDrawingTools drawingContext, CatalogOrderEditor<T> editor,
        string title,
        Point mousePoint,
        Func<T, string> getName,
        Func<T, string> getSummary,
        Func<T, bool?>? getComputerRole = null)
    {
        if (!editor.IsOpen)
        {
            return;
        }

        _catalogOrderEditorFrame.Draw(title, editor.HasChanges, mousePoint,
            new CatalogOrderEditorFrameDrawingCallbacks(drawingContext.FillRectangle, drawingContext.DrawRectangle,
                drawingContext.DrawText, drawingContext.DrawButton));

        var firstPage = editor.FirstVisiblePageIndex;
        _catalogOrderPageHeader.Draw(firstPage, editor.PageCount,
            new CatalogOrderPageHeaderDrawingCallbacks(drawingContext.DrawText, drawingContext.DrawLine));

        var startIndex = editor.FirstVisiblePageIndex * editor.PageSize;
        for (var visibleIndex = 0; visibleIndex < editor.PageSize * 2; visibleIndex++)
        {
            var index = startIndex + visibleIndex;
            if (index >= editor.Items.Count)
            {
                break;
            }

            var item = editor.Items[index];
            var cardBounds = CatalogOrderEditorLayout.CardBounds(visibleIndex, editor.PageSize);
            _catalogOrderCard.Draw(
                new CatalogOrderCardModel<T>(item, index + 1, cardBounds, index == editor.SelectedIndex,
                    index == editor.DraggedIndex, cardBounds.Contains(mousePoint), getName, getSummary, getComputerRole),
                new CatalogOrderCardDrawingCallbacks(drawingContext.FillRectangle, drawingContext.DrawRectangle,
                    drawingContext.DrawText, drawingContext.DrawFittedText, drawingContext.DrawPlayerRoleFaceIcon));
        }

        var hasSelection = editor.SelectedIndex >= 0 && editor.SelectedIndex < editor.Items.Count;
        var selectedItem = hasSelection ? editor.Items[editor.SelectedIndex] : default!;
        _catalogOrderNavigationPanel.Draw(new CatalogOrderNavigationModel(firstPage, editor.PageCount, editor.SelectedIndex,
            editor.Items.Count, hasSelection ? getName(selectedItem) : null, hasSelection ? getSummary(selectedItem) : null, mousePoint),
            new CatalogOrderNavigationDrawingCallbacks(drawingContext.DrawText, drawingContext.DrawFittedText,
                (bounds, label, point, enabled, scale) => drawingContext.DrawButton(bounds, label, false, point, enabled, scale)));
    }
}
