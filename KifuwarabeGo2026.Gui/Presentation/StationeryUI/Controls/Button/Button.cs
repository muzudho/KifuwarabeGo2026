namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;

using Microsoft.Xna.Framework;
using System;

/// <summary>位置、ラベル、有効状態、ヒット判定を所有する文房具 UI のボタンです。</summary>
public sealed class Button
{
    public Button(Rectangle bounds, string label, float labelScale)
    {
        Bounds = bounds;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        LabelScale = labelScale;
    }

    public Rectangle Bounds { get; set; }
    public string Label { get; set; }
    public float LabelScale { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool IsHit(Point point) => IsEnabled && Bounds.Contains(point);

    public void Draw(Point mousePoint, Action<Rectangle, string, bool, Point, bool, float> drawButton) =>
        (drawButton ?? throw new ArgumentNullException(nameof(drawButton)))(Bounds, Label, false, mousePoint, IsEnabled, LabelScale);
}
