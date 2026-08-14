namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;
using System;

/// <summary>大会ルール編集画面の手数上限入力欄です。</summary>
public sealed class TournamentRuleMoveLimitField
{
    private const int ControlX = 626;
    private static readonly Rectangle ValueBounds = new(ControlX + 132, 612, 176, 40);
    private readonly SinglelineTextUnderline _underline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 }, "EDIT");

    public void Draw(int moveLimit, Point mousePoint, TournamentRuleMoveLimitFieldDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawFieldLabel("MOVES", new Rectangle(ControlX, 604, 668, 56));
        draw.DrawFittedText(moveLimit.ToString(), ValueBounds, Color.White, 0.52f);
        _underline.Bounds = ValueBounds;
        _underline.SetEditing(false);
        _underline.UpdatePointer(mousePoint);
        _underline.Draw(draw.UnderlineSurface);
    }
}

public sealed record TournamentRuleMoveLimitFieldDrawingCallbacks(
    Action<string, Rectangle> DrawFieldLabel,
    Action<string, Rectangle, Color, float> DrawFittedText,
    StationeryDrawingContext UnderlineSurface);
