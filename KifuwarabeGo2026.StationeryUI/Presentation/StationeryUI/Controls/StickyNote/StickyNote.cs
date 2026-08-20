namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>
/// 文房具 UI の付箋です。配置、コネクター、見出し、本文、配色を所有し、
/// 実際の描画だけをホストのコールバックへ委譲します。
/// </summary>
public sealed class StickyNote
{
    public StickyNote(
        StickyNoteKind kind,
        Vector2 connectorStart,
        Color accentColor,
        Color borderColor,
        string heading,
        IReadOnlyList<string> bodyLines,
        int bodyLineSpacing = 40,
        Rectangle? anchorBounds = null)
    {
        Kind = kind;
        ConnectorStart = connectorStart;
        AccentColor = accentColor;
        BorderColor = borderColor;
        Heading = heading ?? throw new ArgumentNullException(nameof(heading));
        BodyLines = bodyLines ?? throw new ArgumentNullException(nameof(bodyLines));
        BodyLineSpacing = bodyLineSpacing;
        AnchorBounds = anchorBounds;
    }

    public StickyNoteKind Kind { get; }
    public Vector2 ConnectorStart { get; }
    public Color AccentColor { get; }
    public Color BorderColor { get; }
    public string Heading { get; }
    public IReadOnlyList<string> BodyLines { get; }
    public int BodyLineSpacing { get; }
    public Rectangle? AnchorBounds { get; }
    public Rectangle Bounds { get; private set; }
    public Vector2 ConnectorEnd { get; private set; }

    public bool TryPlace(StickyNoteScreenId screen)
    {
        if (!StickyNotePlacementStrategies.TryGetPlacement(
                screen,
                Kind,
                new StickyNotePlacementContext(ConnectorStart, AnchorBounds),
                out var placement))
            return false;

        var requiredHeight = 68 + Math.Max(1, BodyLines.Count) * BodyLineSpacing + 18;
        Bounds = new Rectangle(
            placement.Bounds.X,
            placement.Bounds.Y,
            placement.Bounds.Width,
            Math.Max(placement.Bounds.Height, requiredHeight));
        ConnectorEnd = placement.ConnectorEnd;
        return true;
    }

    public void Draw(StickyNoteDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawLine(ConnectorStart, ConnectorEnd, 2, AccentColor);
        draw.FillRectangle(new Rectangle(Bounds.X + 9, Bounds.Y + 11, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 115));
        draw.FillRectangle(Bounds, new Color(19, 25, 30, 248));
        draw.DrawRectangle(Bounds, 2, BorderColor);
        draw.FillRectangle(new Rectangle(Bounds.X, Bounds.Y, 7, Bounds.Height), AccentColor);
        draw.DrawText(Heading, new Rectangle(Bounds.X + 26, Bounds.Y + 20, Bounds.Width - 52, 38), AccentColor, 0.40f);
        for (var index = 0; index < BodyLines.Count; index++)
        {
            draw.DrawText(
                BodyLines[index],
                new Rectangle(Bounds.X + 26, Bounds.Y + 68 + index * BodyLineSpacing, Bounds.Width - 52, 28),
                Color.White,
                0.38f);
        }
    }
}

public sealed record StickyNoteDrawingCallbacks(
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Rectangle, Color, float> DrawText);
