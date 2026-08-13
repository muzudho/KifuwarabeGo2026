namespace KifuwarabeGo2026.Gui.Presentation.GoApps.Formal.LocalMatch.Interval.TournamentRules;

using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class TournamentRuleRenderer
{
    public static int GetDisplayNameCaretIndex(GoScreenRenderer renderer, Point point, string text) =>
        renderer.GetTournamentRulesAddPanelDisplayNameCaretIndex(point, text);
}
