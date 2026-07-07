namespace KifuwarabeGo2026;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Game1 : Game
{
    private const int VirtualWidth = 1920;
    private const int VirtualHeight = 1080;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private Texture2D? _softCircle;
    private Texture2D? _stoneLight;
    private Texture2D? _stoneDark;
    private int _boardSize = 19;
    private MouseState _previousMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = VirtualWidth;
        _graphics.PreferredBackBufferHeight = VirtualHeight;
        _graphics.SynchronizeWithVerticalRetrace = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Kifuwarabe Go 2026";
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Fonts/Ui");
        _pixel = CreateTexture(1, 1, (_, _) => Color.White);
        _softCircle = CreateCircleTexture(128, new Color(255, 255, 255, 255), softEdge: true);
        _stoneLight = CreateStoneTexture(128, lightStone: true);
        _stoneDark = CreateStoneTexture(128, lightStone: false);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1))
        {
            _boardSize = 9;
        }
        else if (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2))
        {
            _boardSize = 13;
        }
        else if (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3))
        {
            _boardSize = 19;
        }

        var mouse = Mouse.GetState();
        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            var point = ToVirtualPoint(mouse.Position);
            if (ButtonBounds(0).Contains(point))
            {
                _boardSize = 9;
            }
            else if (ButtonBounds(1).Contains(point))
            {
                _boardSize = 13;
            }
            else if (ButtonBounds(2).Contains(point))
            {
                _boardSize = 19;
            }
        }

        _previousMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));

        if (_spriteBatch is null || _font is null || _pixel is null || _softCircle is null || _stoneLight is null || _stoneDark is null)
        {
            return;
        }

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: GetVirtualTransform());
        DrawBackground(_spriteBatch);
        DrawBoard(_spriteBatch);
        DrawSidePanel(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Matrix GetVirtualTransform()
    {
        var viewport = GraphicsDevice.Viewport;
        var scale = Math.Min(viewport.Width / (float)VirtualWidth, viewport.Height / (float)VirtualHeight);
        var offsetX = (viewport.Width - VirtualWidth * scale) * 0.5f;
        var offsetY = (viewport.Height - VirtualHeight * scale) * 0.5f;
        return Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(offsetX, offsetY, 0f);
    }

    private Point ToVirtualPoint(Point screenPoint)
    {
        var viewport = GraphicsDevice.Viewport;
        var scale = Math.Min(viewport.Width / (float)VirtualWidth, viewport.Height / (float)VirtualHeight);
        var offsetX = (viewport.Width - VirtualWidth * scale) * 0.5f;
        var offsetY = (viewport.Height - VirtualHeight * scale) * 0.5f;
        return new Point((int)((screenPoint.X - offsetX) / scale), (int)((screenPoint.Y - offsetY) / scale));
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        FillRect(spriteBatch, new Rectangle(0, 0, VirtualWidth, VirtualHeight), new Color(11, 13, 18));
        FillRect(spriteBatch, new Rectangle(0, 0, VirtualWidth, 150), new Color(24, 30, 40));
        FillRect(spriteBatch, new Rectangle(0, 930, VirtualWidth, 150), new Color(9, 28, 31));

        for (var i = 0; i < 18; i++)
        {
            var alpha = (byte)(50 - i * 2);
            DrawLine(spriteBatch, new Vector2(-120, 180 + i * 42), new Vector2(2050, -40 + i * 64), 2, new Color((byte)56, (byte)86, (byte)96, alpha));
        }

        DrawGlow(spriteBatch, new Vector2(1030, 90), 520, new Color(39, 122, 104, 80));
        DrawGlow(spriteBatch, new Vector2(1700, 850), 360, new Color(144, 59, 48, 72));
    }

    private void DrawBoard(SpriteBatch spriteBatch)
    {
        var boardOuter = new Rectangle(54, 50, 980, 980);
        var board = new Rectangle(88, 84, 912, 912);

        FillRect(spriteBatch, new Rectangle(boardOuter.X + 18, boardOuter.Y + 22, boardOuter.Width, boardOuter.Height), new Color(0, 0, 0, 125));
        FillRect(spriteBatch, boardOuter, new Color(66, 42, 28));
        FillRect(spriteBatch, new Rectangle(boardOuter.X + 8, boardOuter.Y + 8, boardOuter.Width - 16, boardOuter.Height - 16), new Color(180, 126, 62));
        FillRect(spriteBatch, board, new Color(221, 166, 82));

        for (var i = 0; i < 24; i++)
        {
            var x = board.X + i * 38;
            DrawLine(spriteBatch, new Vector2(x, board.Y), new Vector2(x + 220, board.Bottom), 1, new Color(246, 196, 113, 42));
        }

        var margin = 38f;
        var playable = board.Width - margin * 2;
        var cell = playable / (_boardSize - 1);
        var start = new Vector2(board.X + margin, board.Y + margin);
        var end = new Vector2(board.Right - margin, board.Bottom - margin);

        for (var i = 0; i < _boardSize; i++)
        {
            var p = start.X + cell * i;
            DrawLine(spriteBatch, new Vector2(p, start.Y), new Vector2(p, end.Y), i == 0 || i == _boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
            p = start.Y + cell * i;
            DrawLine(spriteBatch, new Vector2(start.X, p), new Vector2(end.X, p), i == 0 || i == _boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
        }

        foreach (var star in GetStarPoints(_boardSize))
        {
            var center = BoardPoint(start, cell, star.X, star.Y);
            DrawCircle(spriteBatch, center, Math.Max(5, cell * 0.1f), new Color(55, 38, 25));
        }

        DrawSampleStones(spriteBatch, start, cell);
        DrawBoardFrameHighlights(spriteBatch, boardOuter);
    }

    private void DrawSidePanel(SpriteBatch spriteBatch)
    {
        var panel = new Rectangle(1102, 78, 760, 924);
        FillRect(spriteBatch, new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        FillRect(spriteBatch, panel, new Color(21, 25, 32, 236));
        DrawRect(spriteBatch, panel, 2, new Color(82, 111, 114));

        DrawText(spriteBatch, "KIFUWARABE GO 2026", new Vector2(1142, 116), new Color(244, 238, 218), 1.15f);
        DrawText(spriteBatch, $"BOARD {_boardSize} x {_boardSize}", new Vector2(1144, 178), new Color(99, 223, 185), 0.9f);

        var mousePoint = ToVirtualPoint(Mouse.GetState().Position);
        var labels = new[] { "9 x 9", "13 x 13", "19 x 19" };
        var sizes = new[] { 9, 13, 19 };
        for (var i = 0; i < labels.Length; i++)
        {
            var bounds = ButtonBounds(i);
            var selected = _boardSize == sizes[i];
            var hovered = bounds.Contains(mousePoint);
            FillRect(spriteBatch, bounds, selected ? new Color(39, 125, 97) : hovered ? new Color(50, 62, 72) : new Color(32, 38, 47));
            DrawRect(spriteBatch, bounds, 2, selected ? new Color(147, 244, 200) : new Color(88, 102, 112));
            var size = _font!.MeasureString(labels[i]) * 0.7f;
            DrawText(spriteBatch, labels[i], new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), Color.White, 0.7f);
        }

        DrawInfoStrip(spriteBatch, 1144, 344, "BLACK", "Kifuwarabe", new Color(26, 27, 30), Color.White);
        DrawInfoStrip(spriteBatch, 1144, 442, "WHITE", "Human", new Color(236, 229, 211), new Color(24, 24, 24));
        DrawInfoStrip(spriteBatch, 1144, 540, "KOMI", "6.5", new Color(148, 64, 53), Color.White);

        FillRect(spriteBatch, new Rectangle(1144, 668, 668, 238), new Color(14, 18, 23));
        DrawRect(spriteBatch, new Rectangle(1144, 668, 668, 238), 2, new Color(68, 83, 94));
        DrawText(spriteBatch, "LOCAL BOARD PREVIEW", new Vector2(1178, 700), new Color(180, 195, 195), 0.58f);
        DrawMiniBoard(spriteBatch, new Rectangle(1178, 754, 152, 152));
        DrawText(spriteBatch, "Mouse: board size buttons", new Vector2(1370, 768), new Color(227, 224, 210), 0.58f);
        DrawText(spriteBatch, "Keys: 1=9  2=13  3=19", new Vector2(1370, 816), new Color(227, 224, 210), 0.58f);
        DrawText(spriteBatch, "Esc: quit", new Vector2(1370, 864), new Color(227, 224, 210), 0.58f);
    }

    private Rectangle ButtonBounds(int index) => new(1144 + index * 224, 248, 188, 62);

    private void DrawInfoStrip(SpriteBatch spriteBatch, int x, int y, string label, string value, Color chipColor, Color chipTextColor)
    {
        FillRect(spriteBatch, new Rectangle(x, y, 668, 72), new Color(30, 36, 43));
        DrawRect(spriteBatch, new Rectangle(x, y, 668, 72), 1, new Color(70, 85, 94));
        FillRect(spriteBatch, new Rectangle(x + 18, y + 16, 132, 40), chipColor);
        DrawRect(spriteBatch, new Rectangle(x + 18, y + 16, 132, 40), 1, new Color(120, 130, 126));
        DrawText(spriteBatch, label, new Vector2(x + 38, y + 25), chipTextColor, 0.46f);
        DrawText(spriteBatch, value, new Vector2(x + 184, y + 20), Color.White, 0.62f);
    }

    private void DrawMiniBoard(SpriteBatch spriteBatch, Rectangle rect)
    {
        FillRect(spriteBatch, rect, new Color(202, 145, 68));
        var margin = 14f;
        var cell = (rect.Width - margin * 2) / 8f;
        for (var i = 0; i < 9; i++)
        {
            var x = rect.X + margin + cell * i;
            DrawLine(spriteBatch, new Vector2(x, rect.Y + margin), new Vector2(x, rect.Bottom - margin), 1, new Color(48, 34, 24));
            var y = rect.Y + margin + cell * i;
            DrawLine(spriteBatch, new Vector2(rect.X + margin, y), new Vector2(rect.Right - margin, y), 1, new Color(48, 34, 24));
        }

        DrawStone(spriteBatch, new Vector2(rect.X + margin + cell * 2, rect.Y + margin + cell * 2), 9, black: true);
        DrawStone(spriteBatch, new Vector2(rect.X + margin + cell * 5, rect.Y + margin + cell * 4), 9, black: false);
    }

    private void DrawSampleStones(SpriteBatch spriteBatch, Vector2 start, float cell)
    {
        var points = _boardSize switch
        {
            9 => new[] { (2, 2, true), (6, 2, false), (4, 4, true), (2, 6, false), (5, 6, true) },
            13 => new[] { (3, 3, true), (9, 3, false), (6, 6, true), (3, 9, false), (8, 9, true), (10, 7, false) },
            _ => new[] { (3, 3, true), (15, 3, false), (9, 9, true), (3, 15, false), (14, 14, true), (15, 10, false), (10, 15, true) },
        };

        foreach (var point in points)
        {
            DrawStone(spriteBatch, BoardPoint(start, cell, point.Item1, point.Item2), cell * 0.44f, point.Item3);
        }
    }

    private static Vector2 BoardPoint(Vector2 start, float cell, int x, int y) => new(start.X + cell * x, start.Y + cell * y);

    private static Point[] GetStarPoints(int boardSize)
    {
        return boardSize switch
        {
            9 => new[] { new Point(2, 2), new Point(6, 2), new Point(4, 4), new Point(2, 6), new Point(6, 6) },
            13 => new[] { new Point(3, 3), new Point(9, 3), new Point(6, 6), new Point(3, 9), new Point(9, 9) },
            _ => new[] { new Point(3, 3), new Point(9, 3), new Point(15, 3), new Point(3, 9), new Point(9, 9), new Point(15, 9), new Point(3, 15), new Point(9, 15), new Point(15, 15) },
        };
    }

    private void DrawBoardFrameHighlights(SpriteBatch spriteBatch, Rectangle boardOuter)
    {
        FillRect(spriteBatch, new Rectangle(boardOuter.X, boardOuter.Y, boardOuter.Width, 5), new Color(255, 220, 128, 90));
        FillRect(spriteBatch, new Rectangle(boardOuter.X, boardOuter.Y, 5, boardOuter.Height), new Color(255, 220, 128, 70));
        FillRect(spriteBatch, new Rectangle(boardOuter.Right - 7, boardOuter.Y, 7, boardOuter.Height), new Color(31, 20, 15, 120));
        FillRect(spriteBatch, new Rectangle(boardOuter.X, boardOuter.Bottom - 7, boardOuter.Width, 7), new Color(31, 20, 15, 120));
    }

    private void DrawGlow(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        spriteBatch.Draw(_softCircle!, destination, color);
    }

    private void DrawStone(SpriteBatch spriteBatch, Vector2 center, float radius, bool black)
    {
        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        spriteBatch.Draw(_softCircle!, new Rectangle(destination.X + 7, destination.Y + 10, destination.Width, destination.Height), new Color(0, 0, 0, 110));
        spriteBatch.Draw(black ? _stoneDark! : _stoneLight!, destination, Color.White);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        var size = (int)(radius * 2);
        spriteBatch.Draw(_softCircle!, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size), color);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, float thickness, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        var angle = MathF.Atan2(direction.Y, direction.X);
        spriteBatch.Draw(_pixel!, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private void FillRect(SpriteBatch spriteBatch, Rectangle rect, Color color) => spriteBatch.Draw(_pixel!, rect, color);

    private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        FillRect(spriteBatch, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        FillRect(spriteBatch, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(spriteBatch, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        FillRect(spriteBatch, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
    {
        spriteBatch.DrawString(_font!, text, position + new Vector2(2, 2), new Color(0, 0, 0, 125), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font!, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private Texture2D CreateTexture(int width, int height, Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(GraphicsDevice, width, height);
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                data[y * width + x] = colorFactory(x, y);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateCircleTexture(int size, Color color, bool softEdge)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var dx = x - center;
            var dy = y - center;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            var radius = size * 0.48f;
            if (distance > radius)
            {
                return Color.Transparent;
            }

            var alpha = softEdge ? MathHelper.Clamp((radius - distance) / (radius * 0.45f), 0f, 1f) : 1f;
            return color * alpha;
        });
    }

    private Texture2D CreateStoneTexture(int size, bool lightStone)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var nx = (x - center) / center;
            var ny = (y - center) / center;
            var distance = MathF.Sqrt(nx * nx + ny * ny);
            if (distance > 0.96f)
            {
                return Color.Transparent;
            }

            var highlight = MathF.Max(0f, 1f - MathF.Sqrt((nx + 0.32f) * (nx + 0.32f) + (ny + 0.38f) * (ny + 0.38f)) * 2.2f);
            var shade = 1f - MathHelper.Clamp(distance * 0.55f, 0f, 0.55f);
            if (lightStone)
            {
                var value = (byte)MathHelper.Clamp(214 + highlight * 38 - distance * 34, 170, 255);
                return new Color(value, value, (byte)MathHelper.Clamp(value - 12, 150, 245)) * shade;
            }

            var baseValue = 18 + highlight * 72 - distance * 12;
            return new Color((byte)MathHelper.Clamp(baseValue, 8, 92), (byte)MathHelper.Clamp(baseValue + 2, 9, 96), (byte)MathHelper.Clamp(baseValue + 7, 14, 105));
        });
    }
}
