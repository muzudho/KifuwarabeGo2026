namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;

/// <summary>大会ルール編集画面のコミ入力欄です。</summary>
public sealed class TournamentRuleKomiField
{
    private const int ControlX = 626;
    private static readonly Rectangle ValueBounds = new(ControlX + 132, 466, 176, 38);
    private readonly SinglelineTextUnderline _underline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 }, "EDIT");

    public static bool IsTextBoxHit(Point point) => ValueBounds.Contains(point);

    public void Draw(decimal komi, Point mousePoint, TournamentRuleKomiFieldDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawFieldLabel("KOMI", new Rectangle(ControlX, 460, 668, 56));
        draw.DrawFittedText(komi.ToString("0.0"), ValueBounds, Color.White, 0.52f);
        _underline.Bounds = ValueBounds;
        _underline.SetEditing(false);
        _underline.UpdatePointer(mousePoint);
        _underline.Draw(draw.UnderlineSurface, draw.ActionBadgeDrawing);
    }
}

public sealed record TournamentRuleKomiFieldDrawingCallbacks(
    Action<string, Rectangle> DrawFieldLabel,
    Action<string, Rectangle, Color, float> DrawFittedText,
    StationeryDrawingContext UnderlineSurface,
    ActionBadgeDrawingCallbacks ActionBadgeDrawing);
