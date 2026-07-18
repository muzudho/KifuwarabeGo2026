namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application.Cgos.Watching;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// CGOS 対局の観戦・結果画面を描画します。
/// </summary>
public sealed partial class GoScreenRenderer
{
    public void DrawCgosWatching(CgosGameObservation observation, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        var surface = DrawBoardSurface(observation.BoardSize);
        for (var y = 0; y < observation.BoardSize; y++)
        {
            for (var x = 0; x < observation.BoardSize; x++)
            {
                var stone = observation.GetStone(x, y);
                if (stone != GoStone.Empty)
                {
                    DrawStone(BoardPoint(surface.Start, surface.Cell, x, y), surface.Cell * 0.44f, stone == GoStone.Black);
                }
            }
        }

        DrawBoardFrameHighlights(surface.Outer);
        DrawCgosWatchingSidePanel(observation, mousePoint);
        _spriteBatch.End();
    }

    public static bool GetCgosWatchingBackButtonHit(Point point) => CgosWatchingBackButtonBounds.Contains(point);

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
        DrawInfoStrip(1144, 404, "WHITE", observation.WhitePlayerName);
        DrawInfoStrip(1144, 468, "BLACK", observation.BlackPlayerName);
        DrawInfoStrip(1144, 556, "MOVES", observation.MoveCount.ToString());
        DrawInfoStrip(1144, 620, "TURN", observation.CurrentTurn == GoStone.Black ? "BLACK" : "WHITE");

        if (observation.IsFinished)
        {
            DrawText("RESULT", new Vector2(1144, 716), new Color(180, 195, 195), 0.5f);
            DrawFittedText(observation.Result, new Rectangle(1144, 764, 668, 64), new Color(99, 223, 185), 0.56f);
            DrawCommandButton(CgosWatchingBackButtonBounds, "BACK TO CONNECTION", false, mousePoint, scale: 0.36f);
        }
        else
        {
            DrawText("WATCHING LIVE GAME", new Vector2(1144, 760), new Color(99, 223, 185), 0.52f);
        }
    }

    private static Rectangle CgosWatchingBackButtonBounds => new(1290, 900, 376, 64);
}
