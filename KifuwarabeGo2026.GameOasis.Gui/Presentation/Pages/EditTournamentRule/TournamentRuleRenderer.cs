namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.GoApps.Formal.LocalMatch.Interval.TournamentRules;

using KifuwarabeGo2026.GameOasis.Gui.Presentation;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

public static class TournamentRuleRenderer
{
    public static int GetDisplayNameCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, string text) =>
        TournamentRulesPresenter.Default.GetAddPanelDisplayNameCaretIndex(drawingContext, point, text);
}
