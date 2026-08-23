namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using static KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart.PopupTrendChartScreenBounds;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;

public sealed class PopupTrendChartRenderer
{
    private readonly MoveTrendChartRenderer _moveTrendChartRenderer;
    private KfwStationeryDrawingTools _drawingContext = null!;

    public PopupTrendChartRenderer(MoveTrendChartRenderer moveTrendChartRenderer)
    {
        _moveTrendChartRenderer = moveTrendChartRenderer;
    }
    // シークバーと手順ボタンの少し外側までを含める。ここではパンくずを一時退避する。

    public static bool GetReviewChartPopupOpenHit(Point point) =>
        MoveTrendChartRenderer.ReviewTrendChartBounds.Contains(point);

    public static bool GetLocalLiveChartPopupOpenHit(Point point) =>
        MoveTrendChartRenderer.LocalTrendChartBounds.Contains(point);

    public static bool GetCompletedLocalGameChartPopupOpenHit(Point point) =>
        MoveTrendChartRenderer.CompletedLocalGameTrendChartBounds.Contains(point);

    public static bool GetCgosLiveChartPopupOpenHit(Point point) =>
        MoveTrendChartRenderer.CgosTrendChartBounds.Contains(point);

    public static bool GetReviewChartPopupCloseHit(Point point) =>
        ReviewChartPopupCloseButtonBounds.Contains(point);

    public static bool IsBottomNavigationControlsNearby(Point point) =>
        BottomNavigationControlsProximityBounds.Contains(point);

    public static bool GetReviewChartPopupBackToLiveHit(Point point) =>
        ReviewChartPopupBackToLiveButtonBounds.Contains(point);

    public static bool GetReviewChartPopupAutoUpdateHit(Point point) =>
        ReviewChartPopupAutoUpdateBounds.Contains(point);

    public static bool GetReviewChartPopupScoreToggleHit(Point point) =>
        PopupTrendChartScreen.Default.ScoreAxisSectionLabel.IsVisibilityPinHit(point);

    public static bool GetReviewChartPopupWinRateToggleHit(Point point) =>
        PopupTrendChartScreen.Default.WinRateAxisSectionLabel.IsVisibilityPinHit(point);

    public static bool GetReviewChartPopupCommentToggleHit(Point point) =>
        PopupTrendChartScreen.Default.MoveCommentPanel.IsVisibilityPinHit(point);

    public static MoveTrendDisplayMode? GetReviewChartPopupTrendDisplayModeButtonHit(
        Point point,
        MoveTrendDisplayMode currentMode) =>
        MoveTrendChartRenderer.GetMoveTrendDisplayModeButtonHit(point, ReviewChartPopupChartBounds, currentMode);

    public static int? GetReviewChartPopupCommentPageStepButtonHit(Point point) =>
        PopupTrendChartScreen.Default.MoveCommentPanel.GetPageStepButtonHit(point, PopupTrendChartMoveCommentPanelBounds);

    public static int? GetReviewChartPopupCommentMoveStepButtonHit(Point point) =>
        PopupTrendChartScreen.Default.MoveCommentPanel.GetMoveStepButtonHit(point, PopupTrendChartMoveCommentPanelBounds);

    public static bool GetReviewChartPopupCommentEditButtonHit(Point point) =>
        PopupTrendChartScreen.Default.MoveCommentPanel.IsEditButtonHit(point, PopupTrendChartMoveCommentPanelBounds);

    public static bool IsReviewChartPopupCommentOverlayHit(Point point) =>
        PopupTrendChartMoveCommentPanelBounds.Contains(point);


    public static int? GetReviewChartPopupSeekMove(Point point, int moveCount)
    {
        var navigationBounds = ReviewChartPopupSeekBounds.Contains(point)
            ? ReviewChartPopupSeekBounds
            : ReviewChartPopupPlotBounds.Contains(point) ? ReviewChartPopupPlotBounds : (Rectangle?)null;
        if (navigationBounds is not { } bounds || moveCount <= 0) return null;
        var ratio = Math.Clamp(
            (point.X - bounds.Left) / (double)Math.Max(1, bounds.Width),
            0d,
            1d);
        return Math.Clamp((int)Math.Round(ratio * moveCount), 0, moveCount);
    }

    public static int? GetReviewChartPopupStepButtonHit(Point point)
    {
        return PopupTrendChartScreen.Default.SeekButtonStrip.GetButtonHit(point);
    }

    public static int? GetReplayStepButtonHit(Point point) =>
        GetReviewChartPopupStepButtonHit(point);

    public static bool GetReplayEditButtonHit(Point point) =>
        ReplayEditButtonBounds.Contains(point);

    public static bool GetReplayBackToLiveButtonHit(Point point) =>
        ReplayBackToLiveButtonBounds.Contains(point);

    public void DrawReview(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        _drawingContext = drawingContext;
        FillRect(new Rectangle(0, 0, drawingContext.ScreenWidth, drawingContext.ScreenHeight), new Color(65, 80, 125, 58));
        FillRect(ReviewChartPopupBounds, new Color(54, 69, 112, 108));
        DrawRect(ReviewChartPopupBounds, 4, new Color(158, 177, 229, 230));
        DrawText("KIFU NAVIGATION", new Vector2(92, 58), new Color(238, 242, 255), 0.62f);
        PopupTrendChartScreen.Default.CloseButton.Draw(mousePoint, _drawingContext);

        _moveTrendChartRenderer.Draw(
            _drawingContext,
            session,
            session.ReviewMoves,
            ReviewChartPopupChartBounds,
            mousePoint,
            session.ReviewMoveIndex,
            popup: true);

        PopupTrendChartScreen.Default.SeekButtonStrip.Draw(
            _drawingContext,
            session.ReviewTimelineIndex,
            session.ReviewTimelineMaximum,
            mousePoint);
        DrawReviewChartPopupSeekBar(session);
    }

    public void DrawLocal(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        _drawingContext = drawingContext;
        DrawReadOnlyChartPopup(
            session,
            session.CurrentGameRecord.Moves,
            mousePoint,
            session.CurrentMode.Kind == GoAppModeKind.GameOver
                ? "GAME RESULT TREND"
                : session.IsLocalReplayMode ? "REPLAY GAME TREND" : "CURRENT GAME TREND",
            session.LocalDisplayMoveIndex,
            seekable: true,
            showBackToLive: session.CurrentMode.Kind == GoAppModeKind.Playing,
            backToLiveEnabled: session.IsLocalReplayMode,
            backToLiveLabel: "BACK TO CURRENT",
            showUnsavedNotice:
                session.CurrentMode.Kind == GoAppModeKind.GameOver &&
                !session.IsLocalResultSgfSaved);
    }

    public void DrawCgos(
        KfwStationeryDrawingTools drawingContext,
        GoAppSession session,
        CgosGameObservation observation,
        Point mousePoint)
    {
        _drawingContext = drawingContext;
        DrawReadOnlyChartPopup(
            session,
            observation.Moves,
            mousePoint,
            observation.IsReplayMode ? "CGOS REPLAY TREND" : "CGOS LIVE TREND",
            observation.DisplayMoveIndex,
            seekable: true,
            showBackToLive: !observation.IsFinished,
            backToLiveEnabled: observation.IsReplayMode,
            showUnsavedNotice: observation.IsFinished && !session.IsCgosResultSgfSaved);
    }

    private void DrawReadOnlyChartPopup(
        GoAppSession session,
        IReadOnlyList<GoGameMove> moves,
        Point mousePoint,
        string title,
        int? selectedMoveIndex = null,
        bool seekable = false,
        bool showBackToLive = false,
        bool backToLiveEnabled = false,
        string backToLiveLabel = "BACK TO LIVE",
        bool showUnsavedNotice = false)
    {
        var visibleMoves = new MovePrefixView(
            moves,
            showBackToLive
                ? session.GetLiveChartVisibleMoveCount(moves.Count)
                : moves.Count);
        FillRect(new Rectangle(0, 0, _drawingContext.ScreenWidth, _drawingContext.ScreenHeight), new Color(65, 80, 125, 58));
        FillRect(ReviewChartPopupBounds, new Color(54, 69, 112, 108));
        DrawRect(ReviewChartPopupBounds, 4, new Color(158, 177, 229, 230));
        DrawText(title, new Vector2(92, 58), new Color(238, 242, 255), 0.62f);
        if (showUnsavedNotice)
        {
            DrawFittedText(
                "SGF NOT SAVED — SAVE IT FROM THE RESULT SCREEN",
                new Rectangle(690, 59, 930, 42),
                new Color(255, 205, 112),
                0.34f);
        }
        PopupTrendChartScreen.Default.CloseButton.Draw(mousePoint, _drawingContext);
        if (showBackToLive)
        {
            DrawLiveChartAutoUpdateCheckBox(session, mousePoint);
            var backToLiveButton = PopupTrendChartScreen.Default.BackToLiveButton;
            backToLiveButton.Label = backToLiveLabel;
            backToLiveButton.IsEnabled = backToLiveEnabled;
            backToLiveButton.Draw(mousePoint, _drawingContext);
        }

        _moveTrendChartRenderer.Draw(
            _drawingContext,
            session,
            visibleMoves,
            ReviewChartPopupChartBounds,
            mousePoint,
            Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
            popup: true);

        if (seekable)
        {
            PopupTrendChartScreen.Default.SeekButtonStrip.Draw(
                _drawingContext,
                Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
                visibleMoves.Count,
                mousePoint);
            DrawChartPopupSeekBar(
                Math.Min(selectedMoveIndex ?? visibleMoves.Count, visibleMoves.Count),
                visibleMoves.Count);
        }
        else
        {
            DrawCenteredText(
                $"{moves.Count} MOVES   VIEW ONLY",
                new Vector2(ReviewChartPopupBounds.Center.X, ReviewChartPopupBounds.Bottom - 30),
                new Color(238, 242, 255),
                0.48f);
        }
    }

    public void DrawReplayNavigationControls(
        KfwStationeryDrawingTools drawingContext,
        int currentMoveIndex,
        int moveCount,
        Point mousePoint,
        bool showBackToLive,
        string backToLiveLabel)
    {
        _drawingContext = drawingContext;
        PopupTrendChartScreen.Default.SeekButtonStrip.Draw(
            _drawingContext, currentMoveIndex, moveCount, mousePoint);
        if (showBackToLive)
        {
            var backToLiveButton = PopupTrendChartScreen.Default.ReplayBackToLiveButton;
            backToLiveButton.Label = backToLiveLabel;
            backToLiveButton.Draw(mousePoint, _drawingContext);
        }
        DrawReplayEditIconButton(_drawingContext, mousePoint);
    }


    public void DrawReplayEditIconButton(KfwStationeryDrawingTools drawingContext, Point mousePoint)
    {
        _drawingContext = drawingContext;
        var hovered = ReplayEditButtonBounds.Contains(mousePoint);
        FillRect(
            new Rectangle(
                ReplayEditButtonBounds.X + 5,
                ReplayEditButtonBounds.Y + 7,
                ReplayEditButtonBounds.Width,
                ReplayEditButtonBounds.Height),
            new Color(0, 0, 0, 100));
        FillRect(
            ReplayEditButtonBounds,
            hovered ? new Color(53, 92, 97, 245) : new Color(30, 43, 54, 245));
        DrawRect(
            ReplayEditButtonBounds,
            hovered ? 4 : 2,
            hovered ? new Color(91, 218, 211) : new Color(137, 160, 205));

        var board = new Rectangle(
            ReplayEditButtonBounds.X + 13,
            ReplayEditButtonBounds.Y + 12,
            40,
            40);
        FillRect(board, new Color(239, 241, 235));
        DrawRect(board, 3, new Color(88, 100, 102));
        DrawLine(
            new Vector2(board.X + 10, board.Y + 13),
            new Vector2(board.Right - 9, board.Y + 13),
            2,
            new Color(109, 121, 120));
        DrawLine(
            new Vector2(board.X + 10, board.Y + 25),
            new Vector2(board.Right - 9, board.Y + 25),
            2,
            new Color(109, 121, 120));

        var penColor = hovered ? new Color(255, 225, 128) : new Color(235, 190, 86);
        DrawLine(
            new Vector2(ReplayEditButtonBounds.X + 43, ReplayEditButtonBounds.Y + 55),
            new Vector2(ReplayEditButtonBounds.X + 60, ReplayEditButtonBounds.Y + 31),
            8,
            new Color(18, 27, 32));
        DrawLine(
            new Vector2(ReplayEditButtonBounds.X + 43, ReplayEditButtonBounds.Y + 54),
            new Vector2(ReplayEditButtonBounds.X + 59, ReplayEditButtonBounds.Y + 32),
            5,
            penColor);
        DrawCircle(
            new Vector2(ReplayEditButtonBounds.X + 60, ReplayEditButtonBounds.Y + 31),
            4,
            new Color(238, 242, 255));
    }


    private void DrawLiveChartAutoUpdateCheckBox(GoAppSession session, Point mousePoint)
    {
        var hovered = ReviewChartPopupAutoUpdateBounds.Contains(mousePoint);
        FillRect(
            ReviewChartPopupAutoUpdateBounds,
            hovered ? new Color(47, 65, 91, 230) : new Color(31, 45, 70, 220));
        DrawRect(ReviewChartPopupAutoUpdateBounds, 2, new Color(137, 160, 205));
        var checkBounds = new Rectangle(
            ReviewChartPopupAutoUpdateBounds.X + 12,
            ReviewChartPopupAutoUpdateBounds.Y + 10,
            28,
            28);
        FillRect(checkBounds, new Color(17, 24, 48, 245));
        DrawRect(checkBounds, 2, new Color(176, 194, 242));
        if (session.IsLiveChartAutoUpdateEnabled)
        {
            DrawLine(
                new Vector2(checkBounds.X + 6, checkBounds.Y + 15),
                new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7),
                4,
                new Color(91, 218, 211));
            DrawLine(
                new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7),
                new Vector2(checkBounds.Right - 5, checkBounds.Y + 6),
                4,
                new Color(91, 218, 211));
        }
        DrawFittedText(
            "AUTO UPDATE",
            new Rectangle(
                checkBounds.Right + 10,
                ReviewChartPopupAutoUpdateBounds.Y + 6,
                ReviewChartPopupAutoUpdateBounds.Width - 60,
                ReviewChartPopupAutoUpdateBounds.Height - 12),
            Color.White,
            0.34f);
    }

    private readonly struct MovePrefixView(
        IReadOnlyList<GoGameMove> moves,
        int count) : IReadOnlyList<GoGameMove>
    {
        public int Count { get; } = Math.Clamp(count, 0, moves.Count);
        public GoGameMove this[int index] =>
            index >= 0 && index < Count
                ? moves[index]
                : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<GoGameMove> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                yield return moves[index];
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    private void DrawReviewChartPopupSeekBar(GoAppSession session)
    {
        DrawChartPopupSeekBar(session.ReviewMoveIndex, session.ReviewMoveCount);
    }

    private void DrawChartPopupSeekBar(int moveIndex, int moveCount)
    {
        FillRect(ReviewChartPopupSeekBounds, new Color(17, 24, 48, 225));
        DrawRect(ReviewChartPopupSeekBounds, 2, new Color(176, 194, 242));
        var ratio = moveCount == 0
            ? 0f
            : moveIndex / (float)moveCount;
        var x = ReviewChartPopupSeekBounds.Left + ratio * ReviewChartPopupSeekBounds.Width;
        FillRect(
            new Rectangle(
                ReviewChartPopupSeekBounds.Left,
                ReviewChartPopupSeekBounds.Y + 7,
                Math.Max(1, (int)(x - ReviewChartPopupSeekBounds.Left)),
                14),
            new Color(91, 218, 211, 220));
        DrawCircle(new Vector2(x, ReviewChartPopupSeekBounds.Center.Y), 13, new Color(255, 225, 128));
        DrawCenteredText(
            $"{moveIndex} / {moveCount}   CLICK OR DRAG TO SEEK",
            new Vector2(ReviewChartPopupSeekBounds.Center.X, ReviewChartPopupSeekBounds.Y - 25),
            new Color(238, 242, 255),
            0.48f);
    }

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawingContext.DrawLine(start, end, thickness, color);
    private void DrawCircle(Vector2 center, float radius, Color color) => _drawingContext.DrawCircle(center, radius, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);
    private void DrawCenteredText(string text, Vector2 center, Color color, float scale)
    {
        var size = _drawingContext.MeasureText(text) * scale;
        DrawText(text, center - size / 2f, color, scale);
    }
}
