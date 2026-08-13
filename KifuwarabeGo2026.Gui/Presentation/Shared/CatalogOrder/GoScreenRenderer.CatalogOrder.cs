namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    private static readonly CatalogOrder CatalogOrderControl = new();
    private readonly CatalogOrderCard _catalogOrderCard = new();

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

    private void DrawCatalogOrderEditor<T>(
        CatalogOrderEditor<T> editor,
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

        var bounds = CatalogOrderEditorLayout.Bounds;
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(new Rectangle(bounds.X + 18, bounds.Y + 20, bounds.Width, bounds.Height), new Color(0, 0, 0, 155));
        FillRect(bounds, new Color(19, 24, 31, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText($"{title} - EDIT ORDER", new Vector2(bounds.X + 30, bounds.Y + 24), new Color(244, 238, 218), 0.68f);
        DrawCommandButton(CatalogOrderEditorLayout.CancelButtonBounds, "DISCARD", false, mousePoint, scale: 0.28f);
        DrawCommandButton(CatalogOrderEditorLayout.SaveButtonBounds, "SAVE & CLOSE", false, mousePoint, scale: 0.23f);

        FillRect(CatalogOrderEditorLayout.BoardBounds, new Color(15, 20, 26));
        DrawRect(CatalogOrderEditorLayout.BoardBounds, 1, new Color(67, 84, 92));
        FillRect(CatalogOrderEditorLayout.PropertyBounds, new Color(15, 20, 26));
        DrawRect(CatalogOrderEditorLayout.PropertyBounds, 1, new Color(67, 84, 92));

        var firstPage = editor.FirstVisiblePageIndex;
        DrawText($"PAGE {firstPage + 1}", new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 16, CatalogOrderEditorLayout.BoardBounds.Y + 12), new Color(99, 223, 185), 0.38f);
        if (firstPage + 1 < editor.PageCount)
            DrawText($"PAGE {firstPage + 2}", new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 528, CatalogOrderEditorLayout.BoardBounds.Y + 12), new Color(99, 223, 185), 0.38f);
        DrawLine(
            new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 520, CatalogOrderEditorLayout.BoardBounds.Y + 12),
            new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 520, CatalogOrderEditorLayout.BoardBounds.Bottom - 12),
            2,
            new Color(50, 91, 89));

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
                new CatalogOrderCardDrawingCallbacks(FillRect, DrawRect, DrawText, DrawFittedText, DrawPlayerRoleFaceIcon));
        }

        DrawText($"PAGES {firstPage + 1}-{Math.Min(editor.PageCount, firstPage + 2)} / {editor.PageCount}", new Vector2(804, 837), new Color(227, 224, 210), 0.36f);
        DrawCommandButton(CatalogOrderEditorLayout.PreviousPairButtonBounds, "PREV", false, mousePoint, enabled: editor.FirstVisiblePageIndex > 0, scale: 0.36f);
        DrawCommandButton(CatalogOrderEditorLayout.NextPairButtonBounds, "NEXT", false, mousePoint, enabled: editor.FirstVisiblePageIndex < editor.PageCount - 1, scale: 0.36f);

        DrawText("SELECTED", new Vector2(1370, 270), new Color(180, 195, 195), 0.36f);
        if (editor.SelectedIndex >= 0 && editor.SelectedIndex < editor.Items.Count)
        {
            var selectedItem = editor.Items[editor.SelectedIndex];
            DrawFittedText(getName(selectedItem), new Rectangle(1370, 306, 244, 42), Color.White, 0.4f);
            DrawFittedText(getSummary(selectedItem), new Rectangle(1370, 352, 244, 52), new Color(204, 211, 206), 0.3f);
        }

        DrawCommandButton(CatalogOrderEditorLayout.TopButtonBounds, "TO TOP", false, mousePoint, enabled: editor.SelectedIndex > 0, scale: 0.34f);
        DrawCommandButton(CatalogOrderEditorLayout.PageUpButtonBounds, "PAGE UP", false, mousePoint, enabled: editor.SelectedIndex > 0, scale: 0.34f);
        DrawCommandButton(CatalogOrderEditorLayout.UpButtonBounds, "UP", false, mousePoint, enabled: editor.SelectedIndex > 0, scale: 0.4f);
        DrawCommandButton(CatalogOrderEditorLayout.DownButtonBounds, "DOWN", false, mousePoint, enabled: editor.SelectedIndex >= 0 && editor.SelectedIndex < editor.Items.Count - 1, scale: 0.4f);
        DrawCommandButton(CatalogOrderEditorLayout.PageDownButtonBounds, "PAGE DOWN", false, mousePoint, enabled: editor.SelectedIndex >= 0 && editor.SelectedIndex < editor.Items.Count - 1, scale: 0.34f);
        DrawFittedText("DRAG A CARD OR USE THE BUTTONS", new Rectangle(1370, 730, 244, 52), new Color(99, 223, 185), 0.27f);
    }
}
