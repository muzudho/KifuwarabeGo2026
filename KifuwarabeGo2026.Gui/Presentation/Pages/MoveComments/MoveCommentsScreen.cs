namespace KifuwarabeGo2026.Gui.Presentation.Pages.MoveComments;

using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;

/// <summary>棋譜コメント表示のレイアウトと操作判定を所有します。</summary>
public sealed class MoveCommentsScreen
{
    public static MoveCommentsScreen Default { get; } = new();

    private MoveCommentsScreen()
    {
        PreviousMoveButton = new Button(Rectangle.Empty, "< PREV", 0.19f);
        NextMoveButton = new Button(Rectangle.Empty, "NEXT >", 0.19f);
        EditButton = new Button(Rectangle.Empty, "EDIT", 0.17f);
        PreviousPageButton = new Button(Rectangle.Empty, "< PAGE", 0.20f);
        NextPageButton = new Button(Rectangle.Empty, "PAGE >", 0.20f);
    }

    public Button PreviousMoveButton { get; }
    public Button NextMoveButton { get; }
    public Button EditButton { get; }
    public Button PreviousPageButton { get; }
    public Button NextPageButton { get; }

    public Rectangle GetHeadingBounds(Rectangle bounds) =>
        IsExpanded(bounds)
            ? new(bounds.X + 24, bounds.Y + 82, bounds.Width - 650, 52)
            : new(bounds.X + 20, bounds.Y + 58, bounds.Width - 440, 36);

    public Rectangle GetBodyBounds(Rectangle bounds)
    {
        var expanded = IsExpanded(bounds);
        var top = bounds.Y + (expanded ? 148 : 102);
        var footerHeight = expanded ? 92 : 56;
        return new Rectangle(bounds.X + 36, top, bounds.Width - 72, Math.Max(1, bounds.Bottom - top - footerHeight));
    }

    public void UpdateLayout(Rectangle bounds)
    {
        var expanded = IsExpanded(bounds);
        PreviousMoveButton.Bounds = expanded ? new(bounds.Right - 326, bounds.Y + 78, 140, 56) : new(bounds.Right - 206, bounds.Y + 58, 92, 36);
        NextMoveButton.Bounds = expanded ? new(bounds.Right - 174, bounds.Y + 78, 140, 56) : new(bounds.Right - 104, bounds.Y + 58, 92, 36);
        EditButton.Bounds = expanded ? new(bounds.Right - 478, bounds.Y + 78, 140, 56) : new(bounds.Right - 308, bounds.Y + 58, 92, 36);
        PreviousPageButton.Bounds = expanded ? new(bounds.Right - 330, bounds.Bottom - 76, 140, 56) : new(bounds.Right - 170, bounds.Bottom - 46, 70, 36);
        NextPageButton.Bounds = expanded ? new(bounds.Right - 174, bounds.Bottom - 76, 140, 56) : new(bounds.Right - 92, bounds.Bottom - 46, 70, 36);
        PreviousMoveButton.LabelScale = NextMoveButton.LabelScale = expanded ? 0.28f : 0.19f;
        EditButton.LabelScale = expanded ? 0.25f : 0.17f;
        PreviousPageButton.LabelScale = NextPageButton.LabelScale = expanded ? 0.30f : 0.20f;
    }

    public int? GetPageStepButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return PreviousPageButton.IsHit(point) ? -1 : NextPageButton.IsHit(point) ? 1 : null;
    }

    public int? GetMoveStepButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return PreviousMoveButton.IsHit(point) ? -1 : NextMoveButton.IsHit(point) ? 1 : null;
    }

    public bool IsEditButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return EditButton.IsHit(point);
    }

    private static bool IsExpanded(Rectangle bounds) => bounds.Width > 1000 || bounds.Height > 600;
}
