namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;
using Microsoft.Xna.Framework.Graphics;
using System;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>
/// CGOS 対局の観戦・結果画面を描画します。
/// </summary>
public sealed class CgosWatchingRenderer
{
    private readonly BoardRenderer _boardRenderer;
    private readonly MoveTrendChartRenderer _moveTrendChartRenderer;
    private readonly PopupTrendChartRenderer _popupTrendChartRenderer;
    private readonly Action<RenParseDisplayMode, int, Func<int, int, GoStone>, Func<GoRenParseResult>, Action, Vector2, float> _drawRenAnalysis;
    private KfwStationeryDrawingTools _drawingContext = null!;

    public CgosWatchingRenderer(BoardRenderer boardRenderer, MoveTrendChartRenderer moveTrendChartRenderer,
        PopupTrendChartRenderer popupTrendChartRenderer,
        Action<RenParseDisplayMode, int, Func<int, int, GoStone>, Func<GoRenParseResult>, Action, Vector2, float> drawRenAnalysis)
    {
        _boardRenderer = boardRenderer;
        _moveTrendChartRenderer = moveTrendChartRenderer;
        _popupTrendChartRenderer = popupTrendChartRenderer;
        _drawRenAnalysis = drawRenAnalysis;
    }
    /// <summary>
    /// CGOS 対局の観戦・結果画面を描画します。
    /// </summary>
    /// <param name="session"></param>
    /// <param name="observation"></param>
    /// <param name="mousePosition"></param>
    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, CgosGameObservation observation, Point mousePosition)
    {
        _drawingContext = drawingContext;
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();

        drawingContext.DrawBackground();
        var surface = _boardRenderer.DrawBoardSurface(observation.BoardSize);
        _drawRenAnalysis(
            session.RenParseDisplayMode,
            observation.BoardSize,
            observation.GetStone,
            observation.ParseRens,
            () => DrawCgosWatchingStones(observation, surface.Start, surface.Cell),
            surface.Start,
            surface.Cell);
        var displayMoveIndex = observation.DisplayMoveIndex;
        GoGameMove? displayLastMove = displayMoveIndex > 0 && displayMoveIndex <= observation.Moves.Count
            ? observation.Moves[displayMoveIndex - 1]
            : null;
        if (!session.IsRenParseDisplayEnabled)
            _boardRenderer.DrawLastMoveMarker(displayLastMove, surface.Start, surface.Cell);

        _boardRenderer.DrawBoardFrameHighlights(surface.Outer);
        if (!observation.IsFinished)
        {
            DrawBroadcastStatusBadge(_drawingContext,
                observation.IsReplayMode ? "REPLAY" : "LIVE",
                session.IsReviewChartPopupOpen);
        }
        if (!session.IsReviewChartPopupOpen)
        {
            DrawCgosWatchingSidePanel(session, observation, mousePoint);
            if (observation.IsReplayMode)
            {
                _popupTrendChartRenderer.DrawReplayNavigationControls(
                    _drawingContext,
                    observation.DisplayMoveIndex,
                    observation.MoveCount,
                    mousePoint,
                    showBackToLive: !observation.IsFinished,
                    backToLiveLabel: "BACK TO LIVE");
            }
            else if (observation.IsStarted)
            {
                _popupTrendChartRenderer.DrawReplayEditIconButton(_drawingContext, mousePoint);
            }
        }
        else
        {
            _popupTrendChartRenderer.DrawCgos(_drawingContext, session, observation, mousePoint);
        }
        drawingContext.End();
    }

    /// <summary>
    /// CGOS 観戦盤面の石を描画します。
    /// </summary>
    private void DrawCgosWatchingStones(CgosGameObservation observation, Vector2 start, float cell)
    {
        for (var y = 0; y < observation.BoardSize; y++)
        {
            for (var x = 0; x < observation.BoardSize; x++)
            {
                var stone = observation.GetStone(x, y);
                if (stone != GoStone.Empty)
                {
                    _boardRenderer.DrawStone(BoardRenderer.BoardPoint(start, cell, x, y), cell * 0.44f, stone == GoStone.Black);
                }
            }
        }
    }

    private void DrawCgosWatchingSidePanel(GoAppSession session, CgosGameObservation observation, Point mousePoint)
    {
        var panel = new Rectangle(1102, 78, 760, 924);
        FillRect(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        FillRect(panel, new Color(21, 25, 32, 236));
        DrawRect(panel, 2, new Color(82, 111, 114));

        new Headline(observation.IsFinished ? "CGOS RESULT" : "CGOS WATCH", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f).Draw(_drawingContext);
        var screen = CgosWatchPage.Default;
        screen.LeaveViewButton.Draw(mousePoint, _drawingContext);

        DrawVerticalResultSection(new Rectangle(1144, 204, 668, 58), "GAME INFO", new Color(66, 104, 116));
        DrawFittedText(
            $"GAME {observation.GameId}     BOARD {observation.BoardSize} x {observation.BoardSize}     KOMI {observation.Komi:0.0}     MOVES {observation.MoveCount}",
            new Rectangle(1164, 212, 628, 38),
            Color.White,
            0.34f);

        DrawVerticalResultSection(new Rectangle(1144, 276, 668, 200), "PLAYERS", new Color(76, 91, 126));
        PlayersComponent.Default.DrawBothPlayers(_drawingContext,
            1144,
            284,
            668,
            observation.BlackPlayerName,
            observation.WhitePlayerName,
            observation.BlackElapsedTime,
            observation.WhiteElapsedTime,
            observation.MainTime,
            observation.BlackAgehama,
            observation.WhiteAgehama,
            observation.CurrentTurn,
            minimal: true,
            blackLiveElapsed: observation.BlackLiveElapsedTime,
            whiteLiveElapsed: observation.WhiteLiveElapsedTime);

        _moveTrendChartRenderer.DrawCgos(_drawingContext, session, observation, mousePoint);

        if (observation.IsFinished)
        {
            DrawVerticalResultSection(new Rectangle(1144, 852, 668, 48), "RESULT", new Color(80, 48, 38));
            DrawCgosResultRow(new Rectangle(1164, 852, 628, 42), observation.Result);

            DrawVerticalResultSection(new Rectangle(1144, 912, 668, 68), "ACTION", new Color(91, 82, 105));
            screen.ReviewButton.Draw(mousePoint, _drawingContext);
            if (session.IsSgfAutoSaveAvailable)
                screen.SgfAutoSaveCheckBox.Draw(
                    screen.ExportSgfButton.Bounds, session, mousePoint, _drawingContext);
            else
                screen.ExportSgfButton.Draw(mousePoint, _drawingContext);
        }
        else
        {
            DrawVerticalResultSection(new Rectangle(1144, 852, 668, 64), "STATUS", new Color(62, 112, 105));
            DrawResultRow(
                new Rectangle(1164, 858, 628, 48),
                "STATE",
                observation.IsReplayMode ? "WATCHING REPLAY" : "WATCHING LIVE GAME",
                new Color(62, 112, 105),
                observation.IsReplayMode ? new Color(137, 201, 255) : new Color(99, 223, 185));
        }

    }

    /// <summary>
    /// CGOS の勝敗表現を、共通の結果行と石アイコンで描画します。
    /// </summary>
    private void DrawCgosResultRow(Rectangle bounds, string result)
    {
        DrawResultLabel(bounds, "RESULT", new Color(80, 48, 38));

        var trimmed = result.Trim();
        if (trimmed.Length >= 2 && trimmed[1] == '+' &&
            (trimmed[0] is 'B' or 'b' or 'W' or 'w'))
        {
            _drawingContext.DrawStoneValue(RightSidePanelLayout.PrimaryValueX, bounds.Center.Y, trimmed[2..], trimmed[0] is 'B' or 'b', new Color(99, 223, 185));
            return;
        }

        DrawFittedText(trimmed, new Rectangle(RightSidePanelLayout.PrimaryValueX, bounds.Y + 6, bounds.Right - RightSidePanelLayout.PrimaryValueX - 18, bounds.Height - 12), new Color(99, 223, 185), 0.58f);
    }

    public void DrawBroadcastStatusBadge(KfwStationeryDrawingTools drawingContext, string label, bool chartPopup = false)
    {
        _drawingContext = drawingContext;
        var replay = label == "REPLAY";
        var bounds = chartPopup
            ? new Rectangle(850, 55, 164, 48)
            : replay
                ? new Rectangle(660, 72, 164, 54)
                : new Rectangle(842, 72, 164, 54);
        FillRect(bounds, replay ? new Color(43, 83, 126, 238) : new Color(137, 34, 41, 238));
        DrawRect(bounds, 2, replay ? new Color(137, 201, 255) : new Color(255, 145, 151));
        if (!replay)
        {
            DrawCircle(new Vector2(bounds.X + 25, bounds.Center.Y), 7, new Color(255, 225, 225));
        }
        DrawCenteredText(
            label,
            new Vector2(bounds.Center.X + (replay ? 0 : 10), bounds.Center.Y),
            Color.White,
            0.48f);
    }

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawCircle(Vector2 center, float radius, Color color) => _drawingContext.DrawCircle(center, radius, color);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);
    private void DrawVerticalResultSection(Rectangle bounds, string label, Color color) => _drawingContext.DrawVerticalResultSection(bounds, label, color);
    private void DrawResultLabel(Rectangle bounds, string label, Color color) => _drawingContext.DrawResultLabel(bounds, label, color);
    private void DrawResultRow(Rectangle bounds, string label, string value, Color chipColor, Color valueColor)
    {
        _drawingContext.DrawDataRowFrame(bounds);
        _drawingContext.FillRectangle(new Rectangle(bounds.X, bounds.Y, 5, bounds.Height), chipColor);
        _drawingContext.DrawFittedText(label, new Rectangle(bounds.X + 18, bounds.Y + 7, 180, bounds.Height - 14), new Color(180, 195, 195), 0.38f);
        _drawingContext.DrawFittedText(value, new Rectangle(bounds.X + 212, bounds.Y + 7, bounds.Width - 230, bounds.Height - 14), valueColor, 0.48f);
    }
    private void DrawCenteredText(string text, Vector2 center, Color color, float scale)
    {
        var size = _drawingContext.MeasureText(text) * scale;
        _drawingContext.DrawText(text, center - size / 2f, color, scale);
    }

}
