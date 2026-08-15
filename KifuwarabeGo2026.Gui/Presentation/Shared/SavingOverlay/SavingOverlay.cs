namespace KifuwarabeGo2026.Gui.Presentation.Shared.SavingOverlay;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;

/// <summary>バックグラウンド保存中の入力遮断表示を所有します。</summary>
public sealed class SavingOverlay
{
    public static SavingOverlay Default { get; } = new();

    private SavingOverlay() { }

    public void Draw(KfwStationeryDrawingTools drawingContext, string message)
    {
        drawingContext.Begin();
        drawingContext.FillRectangle(new Rectangle(0, 0, drawingContext.ScreenWidth, drawingContext.ScreenHeight), new Color(0, 0, 0, 145));
        var panel = new Rectangle(690, 470, 540, 150);
        drawingContext.FillRectangle(panel, new Color(24, 29, 36, 250));
        drawingContext.DrawRectangle(panel, 2, new Color(147, 244, 200));
        drawingContext.DrawFittedText(string.IsNullOrWhiteSpace(message) ? "SAVING..." : message,
            new Rectangle(panel.X + 40, panel.Y + 34, panel.Width - 80, 34), Color.White, 0.46f);

        var center = new Vector2(panel.Center.X, panel.Y + 108);
        var phase = (float)(Environment.TickCount64 % 900L) / 900f * MathF.PI * 2f;
        for (var index = 0; index < 12; index++)
        {
            var angle = phase + index * MathF.PI * 2f / 12f;
            var opacity = (index + 1) / 12f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            drawingContext.DrawLine(center + direction * 15f, center + direction * 27f, 4,
                new Color(147, 244, 200) * opacity);
        }
        drawingContext.End();
    }
}
