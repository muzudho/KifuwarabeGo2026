namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.MultilineTextUnderline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;

/// <summary>複数行テキスト入力欄の罫線を描画する独立コンポーネントです。</summary>
public sealed class MultilineTextUnderline
{
    private readonly string? _actionBadgeLabel;
    private readonly float _actionBadgeTextScale;
    private Rectangle _bounds;

    public MultilineTextUnderline(IUnderline underline, string? actionBadgeLabel = null, float actionBadgeTextScale = 0.34f)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
        _actionBadgeLabel = actionBadgeLabel;
        _actionBadgeTextScale = actionBadgeTextScale;
    }

    public IUnderline Underline { get; }

    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            ActionBadge = _actionBadgeLabel is null ? null : ActionBadge.Create(_actionBadgeLabel, value, _actionBadgeTextScale);
        }
    }

    public bool IsEditing { get; private set; }

    public bool IsHovered { get; private set; }

    public ActionBadge? ActionBadge { get; private set; }

    public void SetEditing(bool isEditing) => IsEditing = isEditing;

    public void UpdatePointer(Point point)
    {
        IsHovered = Bounds.Contains(point);
        if (IsHovered)
            ActionBadge?.Show();
        else
            ActionBadge?.Hide();
    }

    public void Draw(IUnderlineDrawingSurface surface, ActionBadgeDrawingCallbacks? actionBadgeDrawing = null, int lineHeight = 31, int horizontalInset = 18)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var underlineColor = IsEditing ? new Color(147, 244, 200, 180) : IsHovered ? new Color(185, 196, 255, 180) : new Color(99, 223, 185, 180);
        for (var y = Bounds.Y + horizontalInset + lineHeight - 3; y < Bounds.Bottom - horizontalInset; y += lineHeight)
        {
            Underline.ContentBounds = new Rectangle(Bounds.X + horizontalInset, y, Bounds.Width - horizontalInset * 2, 0);
            Underline.TopOffset = 0;
            Underline.Color = underlineColor;
            Underline.Draw(surface);
        }
        if (actionBadgeDrawing is not null)
            ActionBadge?.Draw(actionBadgeDrawing);
    }
}
