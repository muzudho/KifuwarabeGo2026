namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

/// <summary>既存画面と独立 StickyNote を接続する薄い描画ホストです。</summary>
public sealed partial class GoScreenRenderer
{
    private StickyNoteScreenId _stickyNoteScreen = StickyNoteScreenId.Unknown;

    public void SetStickyNoteScreen(StickyNoteScreenId screen) => _stickyNoteScreen = screen;

    private void DrawStickyNote(
        StickyNoteKind kind,
        Vector2 connectorStart,
        Color accent,
        Color borderColor,
        string heading,
        IReadOnlyList<string> bodyLines,
        int bodyLineSpacing = 40,
        Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(_stickyNoteScreen)) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRect, DrawRect, DrawDynamicOptionText));
    }
}
