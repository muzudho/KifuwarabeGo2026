namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Gui.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        DrawBoardFrameHighlights(surface.Outer);
        DrawCgosWatchingSidePanel(observation, mousePoint);
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

    public static bool GetCgosWatchingBackButtonHit(Point point) => CgosWatchingBackButtonBounds.Contains(point);

    /// <summary>
    /// ［SGF OUTPUT］ボタンが押されたか判定します。
    /// </summary>
    public static bool GetCgosWatchingExportSgfButtonHit(Point point) => CgosWatchingExportSgfButtonBounds.Contains(point);

    private void DrawCgosWatchingSidePanel(CgosGameObservation observation, Point mousePoint)
    {
        var panel = new Rectangle(1102, 78, 760, 924);
        FillRect(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        FillRect(panel, new Color(21, 25, 32, 236));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText(observation.IsFinished ? "CGOS RESULT" : "CGOS WATCH", new Vector2(1144, 116), new Color(255, 230, 160), 0.78f);
        DrawInfoStrip(1144, 188, "GAME", observation.GameId.ToString());
        DrawInfoStrip(1144, 252, "BOARD", $"{observation.BoardSize} x {observation.BoardSize}");
        DrawInfoStrip(1144, 316, "KOMI", observation.Komi.ToString("0.0"));
        DrawBothPlayersComponent(
            1144,
            392,
            668,
            observation.BlackPlayerName,
            observation.WhitePlayerName,
            observation.BlackElapsedTime,
            observation.WhiteElapsedTime,
            observation.MainTime,
            observation.BlackAgehama,
            observation.WhiteAgehama,
            observation.CurrentTurn);
        DrawInfoStrip(1144, 596, "MOVES", observation.MoveCount.ToString());

        if (observation.IsFinished)
        {
            DrawText("RESULT", new Vector2(1144, 716), new Color(180, 195, 195), 0.5f);
            DrawFittedText(observation.Result, new Rectangle(1144, 764, 668, 64), new Color(99, 223, 185), 0.56f);
            DrawCommandButton(CgosWatchingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.4f);
            DrawCommandButton(CgosWatchingBackButtonBounds, "BACK TO CONNECTION", false, mousePoint, scale: 0.36f);
        }
        else
        {
            DrawText("WATCHING LIVE GAME", new Vector2(1144, 760), new Color(99, 223, 185), 0.52f);
        }
    }

    private static Rectangle CgosWatchingBackButtonBounds => new(1290, 900, 376, 64);

    /// <summary>
    /// ［SGF OUTPUT］ボタンの描画範囲
    /// </summary>
    private static Rectangle CgosWatchingExportSgfButtonBounds => new(1290, 830, 376, 56);
}
