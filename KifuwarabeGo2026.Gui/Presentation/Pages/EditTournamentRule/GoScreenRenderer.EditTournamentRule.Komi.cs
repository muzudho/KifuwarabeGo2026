namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;

/// <summary>大会ルール編集画面のコミ設定欄を描画します。</summary>
public sealed partial class GoScreenRenderer
{
    private readonly SinglelineTextUnderline _tournamentKomiUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    public static bool GetTournamentRulesKomiTextBoxHit(Point point) => TournamentRulesKomiTextBounds.Contains(point);

    private void DrawTournamentRulesKomiStrip(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(AddPanelControlX, 460, 668, 56);
        DrawDataRowFrame(bounds);
        DrawTournamentRulesFieldLabel("KOMI", bounds);
        var valueBounds = TournamentRulesKomiTextBounds;
        DrawFittedText(FormatKomi(session.Komi), valueBounds, Color.White, 0.52f);
        _tournamentKomiUnderline.Draw(valueBounds, false, valueBounds.Contains(mousePoint), this);
    }

    private static Rectangle TournamentRulesKomiTextBounds => new(AddPanelControlX + 132, 466, 176, 38);
}
