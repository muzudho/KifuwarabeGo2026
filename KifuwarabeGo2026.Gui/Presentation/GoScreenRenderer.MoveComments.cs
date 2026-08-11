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

    public static int? GetCgosCommentMoveStepButtonHit(Point point) =>
        GetCommentMoveStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentMoveStepButtonHit(Point point) =>
        GetCommentMoveStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetLocalGameOverCommentMoveStepButtonHit(Point point) =>
        GetCommentMoveStepButtonHit(point, LocalGameOverTrendChartBounds);

    public static int? GetReviewCommentMoveStepButtonHit(Point point) =>
        GetCommentMoveStepButtonHit(point, ReviewTrendChartBounds);

    public static bool GetReviewCommentEditButtonHit(Point point) =>
        CommentEditButtonBounds(ReviewTrendChartBounds).Contains(point);

    private static bool HasMoveComment(IReadOnlyList<GoGameMove> moves, string rootComment = "")
    {
        if (!string.IsNullOrWhiteSpace(rootComment)) return true;

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
        var moveNumber = preferredMoveNumber
            ?? MoveCommentNavigator.FindAdjacent(moves, moves.Count + 1, -1);
        var commentCount = MoveCommentNavigator.Count(moves);
        var commentOrdinal = moveNumber is { } selectedMoveNumber
            ? MoveCommentNavigator.GetOrdinal(moves, selectedMoveNumber)
            : 0;
        var isRootComment = preferredMoveNumber == 0;
        var displayedComment = isRootComment
            ? session.CurrentGameRecord.RootComment
            : moveNumber is { } validMoveNumber && validMoveNumber > 0 && validMoveNumber <= moves.Count
                ? moves[validMoveNumber - 1].Comment
                : "";
        var hasSelectedComment = !string.IsNullOrWhiteSpace(displayedComment);
        var expanded = bounds.Width > 1000 || bounds.Height > 600;

        DrawFittedText(
            isRootComment
                ? "ROOT COMMENT"
                : commentOrdinal > 0
                ? $"COMMENT {commentOrdinal} / {commentCount}   MOVE {moveNumber}"
                : $"COMMENT - / {commentCount}   MOVE {moveNumber ?? 0}",
            CommentHeadingBounds(bounds),
            new Color(255, 215, 92),
            expanded ? 0.46f : 0.27f);
        DrawCommandButton(
            CommentPreviousMoveButtonBounds(bounds),
            "< PREV",
            false,
            mousePoint,
            enabled:
                moveNumber is { } previousAnchor
                && MoveCommentNavigator.FindAdjacent(moves, previousAnchor, -1) is not null,
            scale: expanded ? 0.28f : 0.19f);
        DrawCommandButton(
            CommentNextMoveButtonBounds(bounds),
            "NEXT >",
            false,
            mousePoint,
            enabled:
                moveNumber is { } nextAnchor
                && MoveCommentNavigator.FindAdjacent(moves, nextAnchor, 1) is not null,
            scale: expanded ? 0.28f : 0.19f);
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            DrawCommandButton(
                CommentEditButtonBounds(bounds),
                "EDIT",
                false,
                mousePoint,
                scale: expanded ? 0.25f : 0.17f);
        }

        if (!hasSelectedComment)
        {
            DrawFittedText(
                isRootComment ? "NO ROOT COMMENT" : commentCount == 0 ? "NO COMMENT" : "NO COMMENT ON THIS MOVE",
                CommentBodyBounds(bounds),
                new Color(142, 163, 164),
                0.34f);
            session.UpdateCommentPageCount(1);
            return;
        }

        var pageCount = DrawDynamicCommentText(
            displayedComment,
            CommentBodyBounds(bounds),
            session.CommentPageIndex);
        session.UpdateCommentPageCount(pageCount);

        DrawFittedText(
            $"PAGE {session.CommentPageIndex + 1} / {session.CommentPageCount}",
            new Rectangle(bounds.X + 36, bounds.Bottom - (expanded ? 70 : 44), expanded ? 340 : 220, expanded ? 50 : 32),
            new Color(174, 198, 198),
            expanded ? 0.40f : 0.25f);
        DrawCommandButton(
            CommentPreviousPageButtonBounds(bounds),
            "< PAGE",
            false,
            mousePoint,
            enabled: session.CommentPageIndex > 0,
            scale: expanded ? 0.30f : 0.20f);
        DrawCommandButton(
            CommentNextPageButtonBounds(bounds),
            "PAGE >",
            false,
            mousePoint,
            enabled: session.CommentPageIndex + 1 < session.CommentPageCount,
            scale: expanded ? 0.30f : 0.20f);
    }

    private int DrawDynamicCommentText(string text, Rectangle bounds, int requestedPage)
    {
        if (string.IsNullOrWhiteSpace(text) || bounds.Width <= 0 || bounds.Height <= 0) return 1;

        var pixelHeight = bounds.Width > 1000 || bounds.Height >= 500 ? 36 : 16;
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

    private static int? GetCommentMoveStepButtonHit(Point point, Rectangle bounds)
    {
        if (CommentPreviousMoveButtonBounds(bounds).Contains(point)) return -1;
        if (CommentNextMoveButtonBounds(bounds).Contains(point)) return 1;
        return null;
    }

    private static Rectangle CommentHeadingBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.X + 24, bounds.Y + 82, bounds.Width - 650, 52)
            : new(bounds.X + 20, bounds.Y + 58, bounds.Width - 440, 36);

    private static Rectangle CommentBodyBounds(Rectangle bounds)
    {
        var expanded = bounds.Width > 1000 || bounds.Height > 600;
        var top = bounds.Y + (expanded ? 148 : 102);
        var footerHeight = expanded ? 92 : 56;
        return new Rectangle(
            bounds.X + 36,
            top,
            bounds.Width - 72,
            Math.Max(1, bounds.Bottom - top - footerHeight));
    }

    private static Rectangle CommentPreviousMoveButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 326, bounds.Y + 78, 140, 56)
            : new(bounds.Right - 206, bounds.Y + 58, 92, 36);

    private static Rectangle CommentNextMoveButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 174, bounds.Y + 78, 140, 56)
            : new(bounds.Right - 104, bounds.Y + 58, 92, 36);

    private static Rectangle CommentEditButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 478, bounds.Y + 78, 140, 56)
            : new(bounds.Right - 308, bounds.Y + 58, 92, 36);

    private static Rectangle CommentPreviousPageButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 330, bounds.Bottom - 76, 140, 56)
            : new(bounds.Right - 170, bounds.Bottom - 46, 70, 36);

    private static Rectangle CommentNextPageButtonBounds(Rectangle bounds) =>
        bounds.Width > 1000 || bounds.Height > 600
            ? new(bounds.Right - 174, bounds.Bottom - 76, 140, 56)
            : new(bounds.Right - 92, bounds.Bottom - 46, 70, 36);
}
