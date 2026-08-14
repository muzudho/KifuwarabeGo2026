namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;
using System;

/// <summary>大会ルール編集画面の対局時間入力欄です。</summary>
public sealed class TournamentRuleTimeField
{
    private const int ControlX = 626;
    private static readonly Rectangle ValueBounds = new(ControlX + 132, 540, 308, 40);
    private readonly SinglelineTextUnderline _underline = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 }, "EDIT");

    public void Draw(TimeSpan time, Point mousePoint, TournamentRuleTimeFieldDrawingCallbacks draw)
    {
        draw.DrawFieldLabel("TIME", new Rectangle(ControlX, 532, 668, 56));
        draw.DrawFittedText($"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}", ValueBounds, Color.White, 0.52f);
        _underline.Bounds = ValueBounds;
        _underline.SetEditing(false);
        _underline.UpdatePointer(mousePoint);
        _underline.Draw(draw.UnderlineSurface, draw.ActionBadgeDrawing);
    }
}

public sealed record TournamentRuleTimeFieldDrawingCallbacks(Action<string, Rectangle> DrawFieldLabel,
    Action<string, Rectangle, Color, float> DrawFittedText, IUnderlineDrawingSurface UnderlineSurface,
    ActionBadgeDrawingCallbacks ActionBadgeDrawing);
