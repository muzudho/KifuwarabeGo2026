namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>単一行テキスト入力のアンダーライン表示を担当します。</summary>
public sealed class SinglelineTextUnderline
{
    private readonly string? _actionBadgeLabel;
    private readonly float _actionBadgeTextScale;
    private Rectangle _bounds;

    public SinglelineTextUnderline(IUnderline underline, string? actionBadgeLabel = null, float actionBadgeTextScale = 0.34f)
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

    public void Draw(StationeryDrawingContext surface, ActionBadgeDrawingCallbacks? actionBadgeDrawing = null)
    {
        Underline.ContentBounds = Bounds;
        Underline.Color = IsEditing
            ? new Color(147, 244, 200)
            : IsHovered ? new Color(185, 196, 255) : new Color(100, 110, 145);
        Underline.Draw(surface);
        if (actionBadgeDrawing is not null)
            ActionBadge?.Draw(actionBadgeDrawing);
    }
}
