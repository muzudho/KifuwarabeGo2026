namespace KifuwarabeGo2026.Gui.Presentation.GoApps.Formal.LocalMatch.Interval.TournamentRules;

using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

public static class TournamentRuleRenderer
{
    public static int GetDisplayNameCaretIndex(StationeryDrawingContext drawingContext, Point point, string text) =>
        drawingContext.ScreenRenderer.GetTournamentRulesAddPanelDisplayNameCaretIndex(point, text);
}
