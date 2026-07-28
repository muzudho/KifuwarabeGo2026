namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

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
        var expanded = bounds.Width > 1000 || bounds.Height > 600;
        DrawFittedText(
            $"MOVE {moveNumber} COMMENT",
            new Rectangle(bounds.X + 24, bounds.Y + (expanded ? 82 : 60), bounds.Width - 48, expanded ? 46 : 30),
            new Color(255, 215, 92),
            expanded ? 0.52f : 0.32f);
        var top = bounds.Y + (expanded ? 136 : 94);
        var footerHeight = expanded ? 92 : 56;
        var pageCount = DrawDynamicCommentText(
            comment,
            new Rectangle(bounds.X + 36, top, bounds.Width - 72, bounds.Bottom - top - footerHeight),
            session.CommentPageIndex);
        session.UpdateCommentPageCount(pageCount);

        DrawFittedText(
            $"PAGE {session.CommentPageIndex + 1} / {session.CommentPageCount}",
            new Rectangle(bounds.X + 36, bounds.Bottom - (expanded ? 70 : 44), expanded ? 340 : 220, expanded ? 50 : 32),
            new Color(174, 198, 198),
            expanded ? 0.40f : 0.25f);
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

        var pixelHeight = bounds.Width > 1000 || bounds.Height > 500 ? 36 : 16;
        const int extraLineSpacing = 3;
        var pageCount = _textRasterizer.GetWrappedPageCount(
            text,
            bounds.Width,
            bounds.Height,
            pixelHeight,
            extraLineSpacing);
        var page = Math.Clamp(requestedPage, 0, pageCount - 1);
        var key = $"{text.GetHashCode(StringComparison.Ordinal)}:{text.Length}:{bounds.Width}:{bounds.Height}:{page}";

        if (_dynamicCommentTexture is null || !string.Equals(_dynamicCommentTextureKey, key, StringComparison.Ordinal))
        {
            _dynamicCommentTexture?.Dispose();
            var png = _textRasterizer.RasterizeWrappedPagePng(
                text,
                bounds.Width,
                bounds.Height,
                pixelHeight,
                extraLineSpacing,
                page);
            using var stream = new MemoryStream(png, writable: false);
            _dynamicCommentTexture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicCommentTextureKey = key;
        }

        _spriteBatch.Draw(_dynamicCommentTexture, bounds, new Color(226, 232, 225));
        return pageCount;
    }

    private static int? GetCommentPageStepButtonHit(Point point, Rectangle bounds)
    {
        if (CommentPreviousPageButtonBounds(bounds).Contains(point)) return -1;
        if (CommentNextPageButtonBounds(bounds).Contains(point)) return 1;
        return null;
    }

    private static Rectangle CommentPreviousPageButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 330, bounds.Bottom - 76, 140, 56)
            : new(bounds.Right - 170, bounds.Bottom - 46, 70, 36);

    private static Rectangle CommentNextPageButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 174, bounds.Bottom - 76, 140, 56)
            : new(bounds.Right - 92, bounds.Bottom - 46, 70, 36);
}
