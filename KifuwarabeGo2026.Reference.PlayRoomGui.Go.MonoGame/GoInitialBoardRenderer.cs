namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Content資産に依存せず、専用HostのLocal Match盤面を描くRendererです。</summary>
public sealed class GoInitialBoardRenderer : IDisposable
{
    private readonly Texture2D _pixel;
    private readonly Texture2D _blackStone;
    private readonly Texture2D _whiteStone;

    public GoInitialBoardRenderer(GraphicsDevice graphicsDevice)
    {
        _pixel = CreateSolidTexture(graphicsDevice, Color.White);
        _blackStone = CreateStoneTexture(graphicsDevice, 96, new Color(24, 27, 31), new Color(92, 98, 108));
        _whiteStone = CreateStoneTexture(graphicsDevice, 96, new Color(220, 216, 196), Color.White);
    }

    public void Draw(SpriteBatch spriteBatch, GoPlayRoomLaunchPlan plan, Rectangle viewport)
    {
        var setup = plan.SetupStones.ToDictionary(value => value.Point, value => value.Stone);
        Draw(spriteBatch, GoPlayRoomViewState.Capture(
            plan.Activity, plan.BoardSize,
            (x, y) => setup.GetValueOrDefault(new Reference.PlayDomain.Go.GoPoint(x, y)),
            plan.StartingPlayer, 0, 0, 0, null, null, "", 0, 0), viewport);
    }

    public void Draw(SpriteBatch spriteBatch, GoPlayRoomViewState state, Rectangle viewport)
    {
        ArgumentNullException.ThrowIfNull(state);
        var board = CreateBoardBounds(viewport);
        spriteBatch.Draw(_pixel, new Rectangle(board.X + 12, board.Y + 16, board.Width, board.Height), new Color(0, 0, 0, 90));
        spriteBatch.Draw(_pixel, board, new Color(215, 158, 76));

        var margin = Math.Clamp(board.Width * 0.055f, 4f, board.Width / 3f);
        var cell = (board.Width - margin * 2) / (state.BoardSize - 1);
        var lineThickness = Math.Max(2, board.Width / 480);
        for (var index = 0; index < state.BoardSize; index++)
        {
            var offset = margin + index * cell;
            DrawLine(spriteBatch, board.X + margin, board.Y + offset, board.Right - margin, board.Y + offset, lineThickness);
            DrawLine(spriteBatch, board.X + offset, board.Y + margin, board.X + offset, board.Bottom - margin, lineThickness);
        }

        var stoneSize = Math.Max(20, (int)(cell * 0.88f));
        for (var y = 0; y < state.BoardSize; y++)
        for (var x = 0; x < state.BoardSize; x++)
        {
            var stone = state.GetStone(x, y);
            if (stone == Reference.PlayDomain.Go.GoStone.Empty) continue;
            var centerX = board.X + margin + x * cell;
            var centerY = board.Y + margin + y * cell;
            var destination = new Rectangle((int)(centerX - stoneSize / 2f), (int)(centerY - stoneSize / 2f), stoneSize, stoneSize);
            spriteBatch.Draw(stone == Reference.PlayDomain.Go.GoStone.Black ? _blackStone : _whiteStone, destination, Color.White);
        }

        if (state.LastMovePoint is { } lastMove)
        {
            var centerX = board.X + margin + lastMove.X * cell;
            var centerY = board.Y + margin + lastMove.Y * cell;
            var marker = Math.Max(5, stoneSize / 7);
            spriteBatch.Draw(_pixel, new Rectangle((int)centerX - marker / 2, (int)centerY - marker / 2, marker, marker), new Color(210, 64, 55));
        }
    }

    public static GoBoardGeometry CreateGeometry(int boardSize, Rectangle viewport)
    {
        var board = CreateBoardBounds(viewport);
        var margin = Math.Clamp(board.Width * 0.055f, 4f, board.Width / 3f);
        return new GoBoardGeometry(
            boardSize,
            new GoBoardViewport(board.X, board.Y, board.Width, board.Height),
            new GoBoardScreenPoint(board.X + margin, board.Y + margin),
            (board.Width - margin * 2) / (boardSize - 1));
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _blackStone.Dispose();
        _whiteStone.Dispose();
    }

    private void DrawLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, int thickness)
    {
        var vertical = Math.Abs(x2 - x1) < 1f;
        var destination = vertical
            ? new Rectangle((int)x1 - thickness / 2, (int)y1, thickness, Math.Max(1, (int)(y2 - y1)))
            : new Rectangle((int)x1, (int)y1 - thickness / 2, Math.Max(1, (int)(x2 - x1)), thickness);
        spriteBatch.Draw(_pixel, destination, new Color(49, 35, 25));
    }

    private static Rectangle CreateBoardBounds(Rectangle viewport)
    {
        var boardSide = Math.Max(32, Math.Min(viewport.Width, viewport.Height) - 96);
        return new Rectangle(
            viewport.X + (viewport.Width - boardSide) / 2,
            viewport.Y + (viewport.Height - boardSide) / 2,
            boardSide,
            boardSide);
    }

    private static Texture2D CreateSolidTexture(GraphicsDevice graphicsDevice, Color color)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData([color]);
        return texture;
    }

    private static Texture2D CreateStoneTexture(GraphicsDevice graphicsDevice, int size, Color edge, Color highlight)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colors = new Color[size * size];
        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var nx = (x - center) / center;
            var ny = (y - center) / center;
            var distance = MathF.Sqrt(nx * nx + ny * ny);
            if (distance > 0.98f)
            {
                colors[y * size + x] = Color.Transparent;
                continue;
            }
            var light = Math.Clamp(1f - MathF.Sqrt((nx + 0.35f) * (nx + 0.35f) + (ny + 0.4f) * (ny + 0.4f)), 0f, 1f);
            colors[y * size + x] = Color.Lerp(edge, highlight, light * 0.75f);
        }
        texture.SetData(colors);
        return texture;
    }
}
