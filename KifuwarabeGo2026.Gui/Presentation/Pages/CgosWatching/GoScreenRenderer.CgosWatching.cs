namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using KifuwarabeGo2026.Gui.Presentation.Pages.CgosWatching;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// CGOS 対局の観戦・結果画面を描画します。
/// </summary>
public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// CGOS 対局の観戦・結果画面を描画します。
    /// </summary>
    /// <param name="session"></param>
    /// <param name="observation"></param>
    /// <param name="mousePosition"></param>
    public void DrawCgosWatching(GoAppSession session, CgosGameObservation observation, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        var surface = DrawBoardSurface(observation.BoardSize);
        DrawBoardRenAnalysis(
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
            DrawLastMoveMarker(displayLastMove, surface.Start, surface.Cell);

        DrawBoardFrameHighlights(surface.Outer);
        if (!observation.IsFinished)
        {
            DrawBroadcastStatusBadge(
                observation.IsReplayMode ? "REPLAY" : "LIVE",
                session.IsReviewChartPopupOpen);
        }
        if (!session.IsReviewChartPopupOpen)
        {
            DrawCgosWatchingSidePanel(session, observation, mousePoint);
            if (observation.IsReplayMode)
            {
                DrawReplayNavigationControls(
                    observation.DisplayMoveIndex,
                    observation.MoveCount,
                    mousePoint,
                    showBackToLive: !observation.IsFinished,
                    backToLiveLabel: "BACK TO LIVE");
            }
            else if (observation.IsStarted)
            {
                DrawReplayEditIconButton(mousePoint);
            }
        }
        else
        {
            DrawCgosLiveChartPopup(session, observation, mousePoint);
        }
        _spriteBatch.End();
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
                    DrawStone(BoardPoint(start, cell, x, y), cell * 0.44f, stone == GoStone.Black);
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

        new Headline(observation.IsFinished ? "CGOS RESULT" : "CGOS WATCH", new Vector2(1144, 136), new Color(255, 230, 160), 0.72f).Draw(this);
        var screen = CgosWatchingScreen.Default;
        screen.LeaveViewButton.Draw(mousePoint, this);

        DrawVerticalResultSection(new Rectangle(1144, 204, 668, 58), "GAME INFO", new Color(66, 104, 116));
        DrawFittedText(
            $"GAME {observation.GameId}     BOARD {observation.BoardSize} x {observation.BoardSize}     KOMI {observation.Komi:0.0}     MOVES {observation.MoveCount}",
            new Rectangle(1164, 212, 628, 38),
            Color.White,
            0.34f);

        DrawVerticalResultSection(new Rectangle(1144, 276, 668, 200), "PLAYERS", new Color(76, 91, 126));
        DrawBothPlayersComponent(
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

        DrawCgosTrendChart(session, observation, mousePoint);

        if (observation.IsFinished)
        {
            DrawVerticalResultSection(new Rectangle(1144, 852, 668, 48), "RESULT", new Color(80, 48, 38));
            DrawCgosResultRow(new Rectangle(1164, 852, 628, 42), observation.Result);

            DrawVerticalResultSection(new Rectangle(1144, 912, 668, 68), "ACTION", new Color(91, 82, 105));
            screen.ReviewButton.Draw(mousePoint, this);
            if (session.IsSgfAutoSaveAvailable)
                DrawSgfAutoSaveCheckBox(screen.ExportSgfButton.Bounds, session, mousePoint);
            else
                screen.ExportSgfButton.Draw(mousePoint, this);
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
            DrawStoneValue(GameOverValueX, bounds.Center.Y, trimmed[2..], trimmed[0] is 'B' or 'b', new Color(99, 223, 185));
            return;
        }

        DrawFittedText(trimmed, new Rectangle(GameOverValueX, bounds.Y + 6, bounds.Right - GameOverValueX - 18, bounds.Height - 12), new Color(99, 223, 185), 0.58f);
    }

    private void DrawBroadcastStatusBadge(string label, bool chartPopup = false)
    {
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

}
