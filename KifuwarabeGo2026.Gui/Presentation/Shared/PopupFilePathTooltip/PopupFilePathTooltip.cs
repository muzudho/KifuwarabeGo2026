namespace KifuwarabeGo2026.Gui.Presentation.Shared.PopupFilePathTooltip;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ファイルパス行から離れた位置へ拡大表示するツールチップと、コピー操作を所有します。
/// </summary>
public sealed class PopupFilePathTooltip
{
    private const int MaximumPathLineLength = 72;
    private const int MaximumPathLineCount = 2;

    public PopupFilePathTooltip()
    {
        CopyButton = new Button(Rectangle.Empty, "COPY", 0.34f);
    }

    public Button CopyButton { get; }

    public static bool IsHovered(
        StickyNoteScreenId screen,
        StickyNoteKind kind,
        Rectangle rowBounds,
        string fullPath,
        Point mousePoint) =>
        IsDisplayablePath(fullPath) &&
        (rowBounds.Contains(mousePoint) || TryGetPopupBounds(screen, kind, rowBounds, out var popupBounds) && popupBounds.Contains(mousePoint));

    public static bool TryGetCopyText(
        StickyNoteScreenId screen,
        StickyNoteKind kind,
        Rectangle rowBounds,
        string fullPath,
        Point point,
        out string text)
    {
        text = string.Empty;
        if (!IsDisplayablePath(fullPath) ||
            !TryGetPopupBounds(screen, kind, rowBounds, out var popupBounds) ||
            !GetCopyButtonBounds(popupBounds).Contains(point))
            return false;

        text = fullPath;
        return true;
    }

    public void Draw(
        StickyNoteScreenId screen,
        StickyNoteKind kind,
        Rectangle rowBounds,
        string fullPath,
        Point mousePoint,
        string heading,
        IReadOnlyList<string> descriptionLines,
        KfwStationeryDrawingTools drawingContext,
        Action<string, Rectangle, Color, float> drawText)
    {
        ArgumentNullException.ThrowIfNull(descriptionLines);
        ArgumentNullException.ThrowIfNull(drawingContext);
        ArgumentNullException.ThrowIfNull(drawText);
        if (!IsHovered(screen, kind, rowBounds, fullPath, mousePoint)) return;

        var lines = descriptionLines.Concat(WrapPath(fullPath).Take(MaximumPathLineCount)).ToArray();
        var note = new StickyNote(
            kind,
            new Vector2(rowBounds.Center.X, rowBounds.Bottom),
            new Color(147, 244, 200),
            new Color(87, 157, 128),
            heading,
            lines,
            bodyLineSpacing: 32,
            anchorBounds: rowBounds);
        if (!note.TryPlace(screen)) return;

        note.Draw(new StickyNoteDrawingCallbacks(
            drawingContext.DrawLine,
            drawingContext.FillRectangle,
            drawingContext.DrawRectangle,
            drawText));
        CopyButton.Bounds = GetCopyButtonBounds(note.Bounds);
        CopyButton.Draw(mousePoint, drawingContext);
    }

    private static bool IsDisplayablePath(string fullPath) =>
        !string.IsNullOrWhiteSpace(fullPath) && fullPath != "-";

    private static bool TryGetPopupBounds(
        StickyNoteScreenId screen,
        StickyNoteKind kind,
        Rectangle rowBounds,
        out Rectangle bounds)
    {
        if (StickyNotePlacementStrategies.TryGetPlacement(
                screen,
                kind,
                new StickyNotePlacementContext(Vector2.Zero, rowBounds),
                out var placement))
        {
            bounds = placement.Bounds;
            return true;
        }

        bounds = Rectangle.Empty;
        return false;
    }

    private static Rectangle GetCopyButtonBounds(Rectangle popupBounds) =>
        new(popupBounds.Right - 132, popupBounds.Bottom - 48, 108, 34);

    private static IEnumerable<string> WrapPath(string path)
    {
        while (path.Length > MaximumPathLineLength)
        {
            var split = path.LastIndexOfAny(['\\', '/'], Math.Min(MaximumPathLineLength, path.Length - 1));
            if (split <= 0) split = MaximumPathLineLength;
            var includesSeparator = path[split] is '\\' or '/';
            yield return path[..(split + (includesSeparator ? 1 : 0))];
            path = path[(split + (includesSeparator ? 1 : 0))..];
        }

        yield return path;
    }
}
