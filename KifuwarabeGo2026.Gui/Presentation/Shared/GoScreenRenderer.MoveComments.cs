namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

public sealed partial class GoScreenRenderer
{
    private static bool HasMoveComment(IReadOnlyList<GoGameMove> moves)
    {
        foreach (var move in moves)
        {
            if (!string.IsNullOrWhiteSpace(move.Comment)) return true;
        }

        return false;
    }

    private void DrawMoveCommentContent(IReadOnlyList<GoGameMove> moves, Rectangle bounds)
    {
        for (var index = moves.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(moves[index].Comment)) continue;
            DrawMoveCommentContent(moves[index].Comment, index + 1, bounds);
            return;
        }

        DrawFittedText(
            "NO COMMENT",
            new Rectangle(bounds.X + 24, bounds.Y + 70, bounds.Width - 48, 40),
            new Color(142, 163, 164),
            0.36f);
    }

    private void DrawMoveCommentContent(string comment, int moveNumber, Rectangle bounds)
    {
        DrawFittedText(
            $"MOVE {moveNumber} COMMENT",
            new Rectangle(bounds.X + 24, bounds.Y + 60, bounds.Width - 48, 30),
            new Color(255, 215, 92),
            0.32f);
        DrawWrappedText(
            comment,
            new Rectangle(bounds.X + 24, bounds.Y + 94, bounds.Width - 48, bounds.Height - 112),
            new Color(226, 232, 225),
            0.30f);
    }

    private void DrawWrappedText(string text, Rectangle bounds, Color color, float scale)
    {
        if (string.IsNullOrWhiteSpace(text) || bounds.Height <= 0) return;

        var lineHeight = Math.Max(1, (int)MathF.Ceiling(_font.LineSpacing * scale));
        var maximumLines = Math.Max(1, bounds.Height / lineHeight);
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var line = new StringBuilder();
        var y = bounds.Y;
        var lineCount = 0;

        void DrawLine(bool isLast)
        {
            if (lineCount >= maximumLines) return;
            var value = line.ToString();
            if (isLast && lineCount == maximumLines - 1 && value.Length > 3)
                value = value[..^3] + "...";
            DrawText(value, new Vector2(bounds.X, y), color, scale);
            y += lineHeight;
            lineCount++;
            line.Clear();
        }

        foreach (var character in normalized)
        {
            if (lineCount >= maximumLines) break;
            if (character == '\n')
            {
                DrawLine(false);
                continue;
            }

            line.Append(character);
            if (_font.MeasureString(line).X * scale <= bounds.Width) continue;
            line.Length--;
            DrawLine(false);
            line.Append(character);
        }

        if (lineCount < maximumLines && line.Length > 0)
            DrawLine(false);
    }
}
