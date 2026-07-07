namespace KifuwarabeGo2026;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GoAppSession _session = new();
    private GoScreenRenderer? _renderer;
    private SoundEffect? _placeStoneSound;
    private MouseState _previousMouse;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = VirtualScreen.Width;
        _graphics.PreferredBackBufferHeight = VirtualScreen.Height;
        _graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Kifuwarabe Go 2026";
        Window.AllowUserResizing = true;
    }

    protected override void LoadContent()
    {
        _renderer = new GoScreenRenderer(GraphicsDevice, Content);
        _placeStoneSound = CreatePlaceStoneSound();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
        }

        UpdateBoardSizeByKeyboard(keyboard);
        UpdateMouseInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));
        _renderer?.Draw(_session, Mouse.GetState().Position);

        base.Draw(gameTime);
    }

    private void UpdateBoardSizeByKeyboard(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1))
        {
            _session.ChangeBoardSize(9);
        }
        else if (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2))
        {
            _session.ChangeBoardSize(13);
        }
        else if (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3))
        {
            _session.ChangeBoardSize(19);
        }
    }

    private void UpdateMouseInput()
    {
        var mouse = Mouse.GetState();
        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
            var boardSize = GoScreenRenderer.GetBoardSizeButtonHit(point);
            if (boardSize.HasValue)
            {
                _session.ChangeBoardSize(boardSize.Value);
            }
            else if (GoScreenRenderer.GetStartPlayingButtonHit(point))
            {
                _session.StartPlaying();
            }
            else if (GoScreenRenderer.GetPassButtonHit(point))
            {
                if (_session.Pass())
                {
                    _placeStoneSound?.Play(0.45f, 0.25f, 0f);
                }
            }
            else if (GoScreenRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
            {
                if (_session.TryPlaceStone(intersection.X, intersection.Y))
                {
                    _placeStoneSound?.Play();
                }
            }
        }

        _previousMouse = mouse;
    }

    private static SoundEffect CreatePlaceStoneSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.09f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-42f * t);
            var wave = MathF.Sin(MathF.Tau * 520f * t) * 0.55f + MathF.Sin(MathF.Tau * 210f * t) * 0.45f;
            var sample = (short)(wave * envelope * short.MaxValue * 0.55f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }
}
