namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed partial class GoScreenRenderer
{
    private Texture2D? _dynamicCommentTexture;
    private string _dynamicCommentTextureKey = "";

    public static int? GetCgosCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetLocalGameOverCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, LocalGameOverTrendChartBounds);

    public static int? GetReviewCommentPageStepButtonHit(Point point) =>
        GetCommentPageStepButtonHit(point, ReviewTrendChartBounds);

    private static bool HasMoveComment(IReadOnlyList<GoGameMove> moves)
    {
        foreach (var move in moves)
        {
            if (!string.IsNullOrWhiteSpace(move.Comment)) return true;
        }

        return false;
    }

    private void DrawMoveCommentContent(
        IReadOnlyList<GoGameMove> moves,
        Rectangle bounds,
        GoAppSession session,
        Point mousePoint,
        int? preferredMoveNumber = null)
    {
        if (preferredMoveNumber is { } moveNumber)
        {
            var index = moveNumber - 1;
            if (index >= 0 && index < moves.Count && !string.IsNullOrWhiteSpace(moves[index].Comment))
            {
                DrawMoveCommentContent(moves[index].Comment, moveNumber, bounds, session, mousePoint);
                return;
            }

            DrawFittedText(
                "NO COMMENT ON THIS MOVE",
                new Rectangle(bounds.X + 24, bounds.Y + 70, bounds.Width - 48, 40),
                new Color(142, 163, 164),
                0.34f);
            return;
        }

        for (var index = moves.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(moves[index].Comment)) continue;
            DrawMoveCommentContent(moves[index].Comment, index + 1, bounds, session, mousePoint);
            return;
        }

        DrawFittedText(
            "NO COMMENT",
            new Rectangle(bounds.X + 24, bounds.Y + 70, bounds.Width - 48, 40),
            new Color(142, 163, 164),
            0.36f);
    }

    private void DrawMoveCommentContent(
        string comment,
        int moveNumber,
        Rectangle bounds,
        GoAppSession session,
        Point mousePoint)
    {
        DrawFittedText(
            $"MOVE {moveNumber} COMMENT",
            new Rectangle(bounds.X + 24, bounds.Y + 60, bounds.Width - 48, 30),
            new Color(255, 215, 92),
            0.32f);
        var pageCount = DrawDynamicCommentText(
            comment,
            new Rectangle(bounds.X + 24, bounds.Y + 94, bounds.Width - 48, bounds.Height - 150),
            session.CommentPageIndex);
        session.UpdateCommentPageCount(pageCount);

        DrawFittedText(
            $"PAGE {session.CommentPageIndex + 1} / {session.CommentPageCount}",
            new Rectangle(bounds.X + 24, bounds.Bottom - 44, 220, 32),
            new Color(174, 198, 198),
            0.25f);
        DrawCommandButton(
            CommentPreviousPageButtonBounds(bounds),
            "<",
            false,
            mousePoint,
            enabled: session.CommentPageIndex > 0,
            scale: 0.38f);
        DrawCommandButton(
            CommentNextPageButtonBounds(bounds),
            ">",
            false,
            mousePoint,
            enabled: session.CommentPageIndex + 1 < session.CommentPageCount,
            scale: 0.38f);
    }

    private int DrawDynamicCommentText(string text, Rectangle bounds, int requestedPage)
    {
        if (string.IsNullOrWhiteSpace(text) || bounds.Width <= 0 || bounds.Height <= 0) return 1;

        using var font = new System.Drawing.Font(
            "Meiryo",
            16f,
            System.Drawing.FontStyle.Regular,
            System.Drawing.GraphicsUnit.Pixel);
        using var measurementBitmap = new System.Drawing.Bitmap(1, 1);
        using var measurementGraphics = System.Drawing.Graphics.FromImage(measurementBitmap);
        measurementGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        var lines = WrapDynamicCommentLines(text, bounds.Width, font, measurementGraphics);
        var lineHeight = Math.Max(1, (int)MathF.Ceiling(font.GetHeight(measurementGraphics) + 3f));
        var linesPerPage = Math.Max(1, bounds.Height / lineHeight);
        var pageCount = Math.Max(1, (lines.Count + linesPerPage - 1) / linesPerPage);
        var page = Math.Clamp(requestedPage, 0, pageCount - 1);
        var key = $"{text.GetHashCode(StringComparison.Ordinal)}:{text.Length}:{bounds.Width}:{bounds.Height}:{page}";

        if (_dynamicCommentTexture is null || !string.Equals(_dynamicCommentTextureKey, key, StringComparison.Ordinal))
        {
            _dynamicCommentTexture?.Dispose();
            using var bitmap = new System.Drawing.Bitmap(
                bounds.Width,
                bounds.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 226, 232, 225));
                var firstLine = page * linesPerPage;
                var lastLine = Math.Min(lines.Count, firstLine + linesPerPage);
                for (var index = firstLine; index < lastLine; index++)
                {
                    graphics.DrawString(
                        lines[index],
                        font,
                        brush,
                        new System.Drawing.PointF(0, (index - firstLine) * lineHeight),
                        System.Drawing.StringFormat.GenericTypographic);
                }
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            _dynamicCommentTexture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicCommentTextureKey = key;
        }

        _spriteBatch.Draw(_dynamicCommentTexture, bounds, Color.White);
        return pageCount;
    }

    private static List<string> WrapDynamicCommentLines(
        string text,
        int maximumWidth,
        System.Drawing.Font font,
        System.Drawing.Graphics graphics)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        if (normalized.Length > 100_000)
            normalized = normalized[..99_997] + "...";

        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var character in normalized)
        {
            if (character == '\n')
            {
                lines.Add(line.ToString());
                line.Clear();
                continue;
            }

            line.Append(character);
            if (graphics.MeasureString(line.ToString(), font, int.MaxValue, System.Drawing.StringFormat.GenericTypographic).Width <= maximumWidth)
                continue;

            line.Length--;
            lines.Add(line.ToString());
            line.Clear();
            line.Append(character);
        }

        if (line.Length > 0 || lines.Count == 0)
            lines.Add(line.ToString());
        return lines;
    }

    private static int? GetCommentPageStepButtonHit(Point point, Rectangle bounds)
    {
        if (CommentPreviousPageButtonBounds(bounds).Contains(point)) return -1;
        if (CommentNextPageButtonBounds(bounds).Contains(point)) return 1;
        return null;
    }

    private static Rectangle CommentPreviousPageButtonBounds(Rectangle bounds) =>
        new(bounds.Right - 170, bounds.Bottom - 46, 70, 36);

    private static Rectangle CommentNextPageButtonBounds(Rectangle bounds) =>
        new(bounds.Right - 92, bounds.Bottom - 46, 70, 36);
}
