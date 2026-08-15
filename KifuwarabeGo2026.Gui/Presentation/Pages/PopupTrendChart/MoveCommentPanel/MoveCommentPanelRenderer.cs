namespace KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;

public sealed class MoveCommentPanelRenderer
{
    private static readonly Rectangle CgosTrendChartBounds = new(1144, 498, 668, 342);
    private static readonly Rectangle LocalTrendChartBounds = new(1144, 466, 668, 300);
    private static readonly Rectangle CompletedLocalGameTrendChartBounds = new(1144, 376, 668, 466);
    private static readonly Rectangle ReviewTrendChartBounds = new(1144, 548, 668, 290);
    private static MoveCommentPanelComponent MoveComments => PopupTrendChartScreen.Default.MoveCommentPanel;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ITextRasterizer _textRasterizer;
    private readonly KfwStationeryDrawingTools _drawingContext;
    private Texture2D? _dynamicCommentTexture;
    private string _dynamicCommentTextureKey = "";

    public MoveCommentPanelRenderer(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        ITextRasterizer textRasterizer,
        KfwStationeryDrawingTools drawingContext)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _textRasterizer = textRasterizer;
        _drawingContext = drawingContext;
    }

    public static int? GetCgosCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetCompletedLocalGameCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, CompletedLocalGameTrendChartBounds);

    public static int? GetReviewCommentPageStepButtonHit(Point point) =>
        MoveComments.GetPageStepButtonHit(point, ReviewTrendChartBounds);

    public static int? GetCgosCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, CgosTrendChartBounds);

    public static int? GetLocalCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, LocalTrendChartBounds);

    public static int? GetCompletedLocalGameCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, CompletedLocalGameTrendChartBounds);

    public static int? GetReviewCommentMoveStepButtonHit(Point point) =>
        MoveComments.GetMoveStepButtonHit(point, ReviewTrendChartBounds);

    public static bool GetReviewCommentEditButtonHit(Point point) =>
        MoveComments.IsEditButtonHit(point, ReviewTrendChartBounds);

    public static bool HasMoveComment(IReadOnlyList<GoGameMove> moves, string rootComment = "")
    {
        if (!string.IsNullOrWhiteSpace(rootComment)) return true;

        foreach (var move in moves)
        {
            if (!string.IsNullOrWhiteSpace(move.Comment)) return true;
        }

        return false;
    }

    public void Draw(
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
        MoveComments.HeadingLabel.DrawFitted(_drawingContext.DrawFittedText);
        MoveComments.PreviousMoveButton.IsEnabled = moveNumber is { } previousAnchor
            && MoveCommentNavigator.FindAdjacent(moves, previousAnchor, -1) is not null;
        MoveComments.PreviousMoveButton.LabelScale = expanded ? 0.28f : 0.19f;
        MoveComments.PreviousMoveButton.Draw(mousePoint, _drawingContext);
        MoveComments.NextMoveButton.IsEnabled = moveNumber is { } nextAnchor
            && MoveCommentNavigator.FindAdjacent(moves, nextAnchor, 1) is not null;
        MoveComments.NextMoveButton.LabelScale = expanded ? 0.28f : 0.19f;
        MoveComments.NextMoveButton.Draw(mousePoint, _drawingContext);
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            MoveComments.EditButton.LabelScale = expanded ? 0.25f : 0.17f;
            MoveComments.EditButton.Draw(mousePoint, _drawingContext);
        }

        if (!hasSelectedComment)
        {
            _drawingContext.DrawFittedText(
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

        _drawingContext.DrawFittedText(
            $"PAGE {session.CommentPageIndex + 1} / {session.CommentPageCount}",
            new Rectangle(bounds.X + 36, bounds.Bottom - (expanded ? 70 : 44), expanded ? 340 : 220, expanded ? 50 : 32),
            new Color(174, 198, 198),
            expanded ? 0.40f : 0.25f);
        MoveComments.PreviousPageButton.IsEnabled = session.CommentPageIndex > 0;
        MoveComments.PreviousPageButton.LabelScale = expanded ? 0.30f : 0.20f;
        MoveComments.PreviousPageButton.Draw(mousePoint, _drawingContext);
        MoveComments.NextPageButton.IsEnabled = session.CommentPageIndex + 1 < session.CommentPageCount;
        MoveComments.NextPageButton.LabelScale = expanded ? 0.30f : 0.20f;
        MoveComments.NextPageButton.Draw(mousePoint, _drawingContext);
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
