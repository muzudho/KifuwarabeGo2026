namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.SelectEntry;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>エントリーとクライアントIDを選択するダイアログのUIを所有します。</summary>
public sealed class SelectEntryScreen
{
    public static SelectEntryScreen Default { get; } = new();

    private SelectEntryScreen()
    {
        CancelButton = new Button(new Rectangle(1116, 180, 156, 50), "CANCEL", 0.34f);
        SelectButton = new Button(new Rectangle(1302, 180, 180, 50), "SELECT", 0.34f);
        PreviousButton = new Button(new Rectangle(686, 782, 104, 48), "PREV", 0.34f);
        NextButton = new Button(new Rectangle(802, 782, 116, 48), "NEXT", 0.42f);
        AddButton = new Button(new Rectangle(270, 880, 110, 48), "ADD", 0.34f);
        DuplicateButton = new Button(new Rectangle(392, 880, 128, 48), "DUPLICATE", 0.29f);
        EditButton = new Button(new Rectangle(532, 880, 120, 48), "EDIT", 0.34f);
        DeleteButton = new Button(new Rectangle(664, 880, 120, 48), "DELETE", 0.34f);
        OrderButton = new Button(new Rectangle(796, 880, 140, 48), "ORDER", 0.34f);
    }

    public Rectangle DialogBounds { get; } = new(210, 120, 1500, 840);
    public Rectangle EntryListBounds { get; } = new(250, 270, 660, 510);
    public Rectangle ClientIdentityListBounds { get; } = new(970, 270, 700, 510);
    public Rectangle PageNumberBounds { get; } = new(610, 790, 64, 32);
    public Button CancelButton { get; }
    public Button SelectButton { get; }
    public Button PreviousButton { get; }
    public Button NextButton { get; }
    public Button AddButton { get; }
    public Button DuplicateButton { get; }
    public Button EditButton { get; }
    public Button DeleteButton { get; }
    public Button OrderButton { get; }

    public Rectangle GetEntryItemBounds(int slot) =>
        new(EntryListBounds.X + 16, EntryListBounds.Y + 14 + slot * 82, EntryListBounds.Width - 32, 72);

    public Rectangle GetClientIdentityItemBounds(int index) =>
        new(ClientIdentityListBounds.X + 16, ClientIdentityListBounds.Y + 14 + index * 82, ClientIdentityListBounds.Width - 32, 72);

    public int? GetEntryItemHit(Point point, int pageIndex, int pageSize, int itemCount)
    {
        var start = pageIndex * pageSize;
        for (var slot = 0; slot < pageSize; slot++)
        {
            var index = start + slot;
            if (index >= itemCount) break;
            if (GetEntryItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }

    public int? GetClientIdentityItemHit(Point point, int itemCount)
    {
        for (var index = 0; index < itemCount; index++)
            if (GetClientIdentityItemBounds(index).Contains(point)) return index;
        return null;
    }

    public void UpdateState(bool canSelect, bool hasSelection, bool canDelete, bool canOrder, bool canGoPrevious, bool canGoNext)
    {
        SelectButton.IsEnabled = canSelect;
        DuplicateButton.IsEnabled = hasSelection;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = canDelete;
        OrderButton.IsEnabled = canOrder;
        PreviousButton.IsEnabled = canGoPrevious;
        NextButton.IsEnabled = canGoNext;
    }
}

internal static class SelectEntryScreenBounds
{
    private static SelectEntryScreen Screen => SelectEntryScreen.Default;
    internal static Rectangle PlayerSelectionDialogBounds => Screen.DialogBounds;
    internal static Rectangle PlayerSelectionListBounds => Screen.EntryListBounds;
    internal static Rectangle PlayerSelectionClientIdentityListBounds => Screen.ClientIdentityListBounds;
    internal static Rectangle PlayerSelectionPageNumberBounds => Screen.PageNumberBounds;
    internal static Rectangle PlayerSelectionItemBounds(int slot) => Screen.GetEntryItemBounds(slot);
    internal static Rectangle PlayerSelectionClientIdentityItemBounds(int index) => Screen.GetClientIdentityItemBounds(index);
}
