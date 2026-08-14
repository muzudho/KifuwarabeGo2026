namespace KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;
using System;

/// <summary>プレイヤー1人分の行を構成する共通コンポーネントです。</summary>
public sealed class PlayerRow
{
    private readonly PlayerTimeStatus _timeStatus = new();
    private readonly PlayerTurnIndicator _turnIndicator = new();
    private readonly AgehamaPlate _agehamaPlate = new();
    private readonly PlayerEngineErrorButton _engineErrorButton = new();
    private readonly PlayerTimeUsageBar _timeUsageBar = new();

    public void Draw(PlayerRowModel model, PlayerRowDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        if (!model.Minimal) draw.DrawDataRowFrame(model.Bounds);
        var layout = PlayerRowLayouts.Create(model.Bounds, model.Minimal, model.LiveElapsed is not null, model.PrimaryValueX);
        _turnIndicator.Draw(layout.ActiveIndicatorX, model.Bounds, model.IsActive, draw.FillRectangle);
        if (model.Minimal) draw.DrawIconStone(new Vector2(layout.StoneCenterX, model.Bounds.Y + 23), 16, model.IsBlack);
        else draw.DrawStone(new Vector2(layout.StoneCenterX, model.Bounds.Y + 23), 16, model.IsBlack);
        draw.DrawFittedText(model.PlayerName, layout.NameBounds, Color.White, 0.5f);
        if (model.Minimal)
        {
            _timeUsageBar.Bounds = new Rectangle(layout.StatusBounds.X, layout.StatusBounds.Y, layout.StatusBounds.Width, 40);
            _timeUsageBar.Used = model.Elapsed;
            _timeUsageBar.Now = model.LiveElapsed;
            _timeUsageBar.Limit = model.MainTime;
            _timeUsageBar.Draw(draw);
        }
        else
        {
            var status = _timeStatus.Build(model.Elapsed, model.MainTime, model.Agehama, model.Minimal, draw.FormatElapsedTime);
            draw.DrawFittedText(status, layout.StatusBounds, new Color(204, 211, 206), 0.34f);
            if (model.LiveElapsed is { } liveElapsed)
                draw.DrawFittedText(_timeStatus.BuildLive(liveElapsed, draw.FormatElapsedTime), layout.LiveStatusBounds,
                    model.IsActive ? new Color(147, 244, 200) : new Color(158, 178, 178), 0.30f);
        }
        if (model.Minimal)
        {
            _agehamaPlate.Draw(new Rectangle(model.Bounds.Right - 136, model.Bounds.Y + 43, 118, 38), model.Agehama,
                !model.IsBlack, new AgehamaPlateDrawingCallbacks(draw.DrawCircleSurface, draw.DrawStone, draw.DrawFittedText));
        }
        if (model.HasEngineError)
            _engineErrorButton.Draw(model.Bounds, model.MousePoint,
                new PlayerEngineErrorButtonDrawingCallbacks(draw.FillRectangle, draw.DrawRectangle, draw.DrawFittedText));
    }
}

public sealed record PlayerRowModel(Rectangle Bounds, string PlayerName, TimeSpan? Elapsed, TimeSpan? LiveElapsed,
    TimeSpan? MainTime, int Agehama, bool IsBlack, bool IsActive, bool HasEngineError, Point? MousePoint,
    bool Minimal, int PrimaryValueX);

public sealed record PlayerRowDrawingCallbacks(Action<Rectangle> DrawDataRowFrame,
    Action<Rectangle, Color> FillRectangle, Action<Rectangle, int, Color> DrawRectangle,
    Action<Vector2, float, bool> DrawStone, Action<Vector2, float, bool> DrawIconStone,
    Action<string, Rectangle, Color, float> DrawFittedText, Func<TimeSpan, string> FormatElapsedTime,
    Action<Rectangle, Color> DrawCircleSurface,
    Action<Vector2, Vector2, float, Color> DrawLine);
