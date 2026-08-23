namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.SeekButtonStrip;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using System;

/// <summary>棋譜の先頭・前後移動・末尾移動を行う8個のボタンを所有します。</summary>
public sealed class SeekButtonStripComponent
{
    private static readonly int[] StepValues =
        [int.MinValue, -50, -10, -1, 1, 10, 50, int.MaxValue];

    private readonly Button[] _buttons;

    public SeekButtonStripComponent(Func<int, Rectangle> getButtonBounds)
    {
        ArgumentNullException.ThrowIfNull(getButtonBounds);

        _buttons = new Button[StepValues.Length];
        for (var index = 0; index < StepValues.Length; index++)
            _buttons[index] = new Button(getButtonBounds(index), FormatStep(StepValues[index]), 0.31f);
    }

    public int? GetButtonHit(Point point)
    {
        for (var index = 0; index < _buttons.Length; index++)
            if (_buttons[index].IsHit(point))
                return StepValues[index];
        return null;
    }

    public Rectangle GetButtonBounds(int index) => _buttons[index].Bounds;

    public void Draw(KfwStationeryDrawingTools drawingContext, int currentIndex, int maximumIndex, Point mousePoint)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        for (var index = 0; index < _buttons.Length; index++)
        {
            var step = StepValues[index];
            var button = _buttons[index];
            button.IsEnabled = step < 0 ? currentIndex > 0 : currentIndex < maximumIndex;
            button.Draw(mousePoint, drawingContext);
        }
    }

    private static string FormatStep(int step) => step switch
    {
        int.MinValue => "|<",
        int.MaxValue => ">|",
        > 0 => $"+{step}",
        _ => step.ToString(),
    };
}
