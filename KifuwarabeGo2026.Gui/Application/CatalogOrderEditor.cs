namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// カタログ形式の一覧で共用する、保存前の順序編集状態です。
/// </summary>
public sealed class CatalogOrderEditor<T>
{
    private readonly List<T> _items = [];
    private readonly List<T> _originalItems = [];

    public bool IsOpen { get; private set; }

    public IReadOnlyList<T> Items => _items;

    public bool HasChanges => !_items.SequenceEqual(_originalItems);

    public int SelectedIndex { get; private set; } = -1;

    public int DraggedIndex { get; private set; } = -1;

    public int PageSize { get; private set; } = 1;

    /// <summary>見開きの左側に表示するページ番号（0 始まり）。</summary>
    public int FirstVisiblePageIndex { get; private set; }

    public int PageCount => Math.Max(1, (int)Math.Ceiling(_items.Count / (double)PageSize));

    public void Open(IEnumerable<T> items, int selectedIndex, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items.Clear();
        _items.AddRange(items);
        _originalItems.Clear();
        _originalItems.AddRange(_items);
        PageSize = Math.Max(1, pageSize);
        SelectedIndex = _items.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _items.Count - 1);
        FirstVisiblePageIndex = SelectedIndex < 0 ? 0 : SelectedIndex / PageSize;
        DraggedIndex = -1;
        IsOpen = true;
    }

    public void Cancel()
    {
        IsOpen = false;
        DraggedIndex = -1;
    }

    public IReadOnlyList<T> Commit()
    {
        IsOpen = false;
        DraggedIndex = -1;
        return _items.ToArray();
    }

    public void Select(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }

        SelectedIndex = index;
    }

    public void BeginDrag(int index)
    {
        Select(index);
        DraggedIndex = SelectedIndex;
    }

    public void DragTo(int index)
    {
        if (DraggedIndex < 0 || index < 0 || index >= _items.Count || index == DraggedIndex)
        {
            return;
        }

        var item = _items[DraggedIndex];
        _items.RemoveAt(DraggedIndex);
        _items.Insert(index, item);
        SelectedIndex = index;
        DraggedIndex = index;
    }

    public void EndDrag() => DraggedIndex = -1;

    public void MoveSelected(int offset)
    {
        if (SelectedIndex < 0 || _items.Count == 0)
        {
            return;
        }

        MoveSelectedTo(Math.Clamp(SelectedIndex + offset, 0, _items.Count - 1));
    }

    public void MoveSelectedToTop() => MoveSelectedTo(0);

    /// <summary>見開きを半ページずつスライドします。</summary>
    public void MoveVisiblePages(int offset) =>
        FirstVisiblePageIndex = Math.Clamp(FirstVisiblePageIndex + offset, 0, PageCount - 1);

    private void MoveSelectedTo(int destination)
    {
        if (SelectedIndex < 0 || destination == SelectedIndex)
        {
            return;
        }

        var item = _items[SelectedIndex];
        _items.RemoveAt(SelectedIndex);
        _items.Insert(destination, item);
        SelectedIndex = destination;
        FirstVisiblePageIndex = SelectedIndex / PageSize;
    }
}
