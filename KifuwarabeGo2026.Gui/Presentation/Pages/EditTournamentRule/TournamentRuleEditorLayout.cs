namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>大会ルール編集画面だけで使う配置とヒットテストです。</summary>
public static class TournamentRuleEditorLayout
{
    public const int ControlX = 626;
    public const int BoardSizeButtonY = 391;

    public static int? GetBoardSizeButtonHit(Point point, GoAppModeKind modeKind)
    {
        if (modeKind == GoAppModeKind.GameOver) return null;
        if (BoardSizeButtonBounds(0).Contains(point)) return 9;
        if (BoardSizeButtonBounds(1).Contains(point)) return 13;
        return BoardSizeButtonBounds(2).Contains(point) ? 19 : null;
    }

    public static GoRuleKind? GetRuleKindButtonHit(Point point)
    {
        if (RuleKindButtonBounds(0).Contains(point)) return GoRuleKind.Japanese;
        if (RuleKindButtonBounds(1).Contains(point)) return GoRuleKind.PureGo;
        return RuleKindButtonBounds(2).Contains(point) ? GoRuleKind.Chinese : null;
    }

    public static bool IsTimeTextBoxHit(Point point) => TimeTextBounds.Contains(point);

    public static bool IsMoveLimitTextBoxHit(Point point) => MoveLimitTextBounds.Contains(point);

    public static int GetNumericCaretIndex(Point point, TournamentRulesNumericField field, string text,
        Func<int, string, Rectangle, float, int> getCaretIndex)
    {
        ArgumentNullException.ThrowIfNull(getCaretIndex);
        var bounds = field switch
        {
            TournamentRulesNumericField.MainTimeHours or TournamentRulesNumericField.MainTimeMinutes or TournamentRulesNumericField.MainTimeSeconds => TimeTextBounds,
            _ => MoveLimitTextBounds,
        };
        return getCaretIndex(point.X, text, new Rectangle(bounds.X + 8, bounds.Y + 4, bounds.Width - 16, bounds.Height - 8), 0.42f);
    }

    public static Rectangle BoardSizeButtonBounds(int index) => new(ControlX + 132 + index * 180, BoardSizeButtonY, 164, 50);
    public static Rectangle RuleKindButtonBounds(int index) => new(ControlX + 132 + index * 180, 319, 164, 50);
    public static Rectangle TimeTextBounds => new(ControlX + 132, 540, 308, 40);
    public static Rectangle MoveLimitTextBounds => new(ControlX + 132, 612, 176, 40);
}
