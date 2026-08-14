namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;

public sealed partial class GoScreenRenderer
{
    private static MoveCommentPanelComponent MoveComments => PopupTrendChartScreen.Default.MoveCommentPanel;
    private Texture2D? _dynamicCommentTexture;
    private string _dynamicCommentTextureKey = "";

    public static int? GetCgosCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetLocalGameOverCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, LocalGameOverTrendChartBounds);

    public static int? GetReviewCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, ReviewTrendChartBounds);

    public static int? GetCgosCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetLocalGameOverCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, LocalGameOverTrendChartBounds);

    public static int? GetReviewCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, ReviewTrendChartBounds);

    public static bool GetReviewCommentEditButtonHit(Point point) =>
        MoveComments.IsEditButtonHit(point, ReviewTrendChartBounds);

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
        MoveComments.UpdateLayout(bounds);
        var moveNumber = preferredMoveNumber
            ?? MoveCommentNavigator.FindAdjacent(moves, moves.Count + 1, -1);
        var commentCount = MoveCommentNavigator.Count(moves);
        var commentOrdinal = moveNumber is { } selectedMoveNumber
            ? MoveCommentNavigator.GetOrdinal(moves, selectedMoveNumber)
            : 0;
        var isRootComment = preferredMoveNumber == 0;
        var displayedComment = isRootComment
            ? session.CurrentMode.Kind == GoAppModeKind.Reviewing
                ? session.ReviewRootComment
                : session.CurrentGameRecord.RootComment
            : moveNumber is { } validMoveNumber && validMoveNumber > 0 && validMoveNumber <= moves.Count
                ? moves[validMoveNumber - 1].Comment
                : "";
        var hasSelectedComment = !string.IsNullOrWhiteSpace(displayedComment);
        var expanded = bounds.Width > 1000 || bounds.Height > 600;

        MoveComments.HeadingLabel.Text = isRootComment
            ? "ROOT COMMENT"
            : commentOrdinal > 0
                ? $"COMMENT {commentOrdinal} / {commentCount}   MOVE {moveNumber}"
                : $"COMMENT - / {commentCount}   MOVE {moveNumber ?? 0}";
        MoveComments.HeadingLabel.DrawFitted(DrawFittedText);
        DrawCommandButton(
            MoveComments.PreviousMoveButton.Bounds,
            "< PREV",
            false,
            mousePoint,
            enabled:
                moveNumber is { } previousAnchor
                && MoveCommentNavigator.FindAdjacent(moves, previousAnchor, -1) is not null,
            scale: expanded ? 0.28f : 0.19f);
        DrawCommandButton(
            MoveComments.NextMoveButton.Bounds,
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
                MoveComments.EditButton.Bounds,
                "EDIT",
                false,
                mousePoint,
                scale: expanded ? 0.25f : 0.17f);
        }

        if (!hasSelectedComment)
        {
            DrawFittedText(
                isRootComment ? "NO ROOT COMMENT" : commentCount == 0 ? "NO COMMENT" : "NO COMMENT ON THIS MOVE",
                MoveComments.GetBodyBounds(bounds),
                new Color(142, 163, 164),
                0.34f);
            session.UpdateCommentPageCount(1);
            return;
        }

        var pageCount = DrawDynamicCommentText(
            displayedComment,
            MoveComments.GetBodyBounds(bounds),
            session.CommentPageIndex);
        session.UpdateCommentPageCount(pageCount);

        DrawFittedText(
            $"PAGE {session.CommentPageIndex + 1} / {session.CommentPageCount}",
            new Rectangle(bounds.X + 36, bounds.Bottom - (expanded ? 70 : 44), expanded ? 340 : 220, expanded ? 50 : 32),
            new Color(174, 198, 198),
            expanded ? 0.40f : 0.25f);
        DrawCommandButton(
            MoveComments.PreviousPageButton.Bounds,
            "< PAGE",
            false,
            mousePoint,
            enabled: session.CommentPageIndex > 0,
            scale: expanded ? 0.30f : 0.20f);
        DrawCommandButton(
            MoveComments.NextPageButton.Bounds,
            "PAGE >",
            false,
            mousePoint,
            enabled: session.CommentPageIndex + 1 < session.CommentPageCount,
            scale: expanded ? 0.30f : 0.20f);
    }

    private int DrawDynamicCommentText(string text, Rectangle bounds, int requestedPage)
    {
        if (string.IsNullOrWhiteSpace(text) || bounds.Width <= 0 || bounds.Height <= 0) return 1;

        // ノートPCでも読み取りやすい通常表示の大きさ。展開表示は従来どおりです。
        var pixelHeight = bounds.Width > 1000 || bounds.Height >= 500 ? 36 : 20;
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

}
