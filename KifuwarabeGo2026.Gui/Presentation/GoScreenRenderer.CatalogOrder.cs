namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    public static bool GetCatalogOrderCancelButtonHit(Point point) =>
        CatalogOrderEditorLayout.CancelButtonBounds.Contains(point);

    public static bool GetCatalogOrderSaveButtonHit(Point point) =>
        CatalogOrderEditorLayout.SaveButtonBounds.Contains(point);

    public static int GetCatalogOrderMoveStep(Point point, int pageSize) =>
        CatalogOrderEditorLayout.TopButtonBounds.Contains(point) ? int.MinValue :
        CatalogOrderEditorLayout.PageUpButtonBounds.Contains(point) ? -pageSize :
        CatalogOrderEditorLayout.UpButtonBounds.Contains(point) ? -1 :
        CatalogOrderEditorLayout.DownButtonBounds.Contains(point) ? 1 :
        CatalogOrderEditorLayout.PageDownButtonBounds.Contains(point) ? pageSize :
        0;

    public static int GetCatalogOrderPagePairStep(Point point) =>
        CatalogOrderEditorLayout.PreviousPairButtonBounds.Contains(point) ? -1 :
        CatalogOrderEditorLayout.NextPairButtonBounds.Contains(point) ? 1 :
        0;

    public static int? GetCatalogOrderCardHit<T>(Point point, CatalogOrderEditor<T> editor)
    {
        var startIndex = editor.PagePairIndex * editor.PageSize * 2;
        for (var visibleIndex = 0; visibleIndex < editor.PageSize * 2; visibleIndex++)
        {
            if (!CatalogOrderEditorLayout.CardBounds(visibleIndex, editor.PageSize).Contains(point))
            {
                continue;
            }

            var index = startIndex + visibleIndex;
            return index < editor.Items.Count ? index : null;
        }

        return null;
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
        DrawCommandButton(CatalogOrderEditorLayout.CancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.34f);
        DrawCommandButton(CatalogOrderEditorLayout.SaveButtonBounds, "SAVE", false, mousePoint, scale: 0.38f);

        FillRect(CatalogOrderEditorLayout.BoardBounds, new Color(15, 20, 26));
        DrawRect(CatalogOrderEditorLayout.BoardBounds, 1, new Color(67, 84, 92));
        FillRect(CatalogOrderEditorLayout.PropertyBounds, new Color(15, 20, 26));
        DrawRect(CatalogOrderEditorLayout.PropertyBounds, 1, new Color(67, 84, 92));

        var firstPage = editor.PagePairIndex * 2;
        DrawText($"PAGE {firstPage + 1}", new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 16, CatalogOrderEditorLayout.BoardBounds.Y + 12), new Color(99, 223, 185), 0.38f);
        if (firstPage + 1 < editor.PageCount)
            DrawText($"PAGE {firstPage + 2}", new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 528, CatalogOrderEditorLayout.BoardBounds.Y + 12), new Color(99, 223, 185), 0.38f);
        DrawLine(
            new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 520, CatalogOrderEditorLayout.BoardBounds.Y + 12),
            new Vector2(CatalogOrderEditorLayout.BoardBounds.X + 520, CatalogOrderEditorLayout.BoardBounds.Bottom - 12),
            2,
            new Color(50, 91, 89));

        var startIndex = editor.PagePairIndex * editor.PageSize * 2;
        for (var visibleIndex = 0; visibleIndex < editor.PageSize * 2; visibleIndex++)
        {
            var index = startIndex + visibleIndex;
            if (index >= editor.Items.Count)
            {
                break;
            }

            var item = editor.Items[index];
            var cardBounds = CatalogOrderEditorLayout.CardBounds(visibleIndex, editor.PageSize);
            var selected = index == editor.SelectedIndex;
            var dragged = index == editor.DraggedIndex;
            var hovered = cardBounds.Contains(mousePoint);
            FillRect(cardBounds, dragged ? new Color(57, 82, 118) : selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
            DrawRect(cardBounds, dragged ? 3 : 1, dragged ? new Color(176, 194, 242) : selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
            DrawText($"{index + 1:00}", new Vector2(cardBounds.X + 12, cardBounds.Y + 14), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.36f);
            DrawFittedText(getName(item), new Rectangle(cardBounds.X + 58, cardBounds.Y + 4, cardBounds.Width - 70, 30), Color.White, 0.39f);
            var isComputer = getComputerRole?.Invoke(item);
            if (isComputer is { } computer)
            {
                DrawPlayerRoleFaceIcon(new Vector2(cardBounds.X + 72, cardBounds.Y + 49), computer);
                DrawFittedText(getSummary(item), new Rectangle(cardBounds.X + 94, cardBounds.Y + 36, cardBounds.Width - 106, 24), new Color(204, 211, 206), 0.28f);
            }
            else
                DrawFittedText(getSummary(item), new Rectangle(cardBounds.X + 58, cardBounds.Y + 36, cardBounds.Width - 70, 24), new Color(204, 211, 206), 0.28f);
        }

        DrawText($"PAGES {firstPage + 1}-{Math.Min(editor.PageCount, firstPage + 2)} / {editor.PageCount}", new Vector2(804, 837), new Color(227, 224, 210), 0.36f);
        DrawCommandButton(CatalogOrderEditorLayout.PreviousPairButtonBounds, "PREV", false, mousePoint, enabled: editor.PagePairIndex > 0, scale: 0.36f);
        DrawCommandButton(CatalogOrderEditorLayout.NextPairButtonBounds, "NEXT", false, mousePoint, enabled: editor.PagePairIndex < editor.PagePairCount - 1, scale: 0.36f);

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
