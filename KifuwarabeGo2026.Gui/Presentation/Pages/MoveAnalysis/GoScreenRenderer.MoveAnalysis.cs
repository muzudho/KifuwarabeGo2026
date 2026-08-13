namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System.Globalization;

/// <summary>CGOS観戦画面と棋譜レビュー画面で共有する着手解析表示です。</summary>
public sealed partial class GoScreenRenderer
{
    private void DrawMoveAnalysisSection(GoGameMove? move, Rectangle bounds)
    {
        var color = new Color(76, 91, 126);
        var content = new Rectangle(bounds.X + 20, bounds.Y + 8, bounds.Width - 40, 52);
        DrawVerticalResultSection(bounds, "ANALYSIS", color);

        var analysis = move?.Analysis;
        var winrate = analysis?.Winrate is { } rate
            ? $"{(move!.Value.Stone == GoStone.Black ? "BLACK" : "WHITE")} {rate:P1}"
            : "-";
        DrawResultRow(content, "WINRATE", winrate, color, Color.White);

        var pvRow = new Rectangle(content.X, content.Y + 56, content.Width, 52);
        DrawResultLabel(pvRow, "PV", color);
        var pvValueBounds = GetMoveAnalysisPvValueBounds(bounds);
        var pv = analysis?.PrincipalVariation ?? "";
        DrawFittedText(pv.Length == 0 ? "-" : AbbreviateOptionValue(pv, 44), pvValueBounds, Color.White, 0.42f);

        if (analysis is not null)
        {
            var score = analysis.Score is { } scoreValue
                ? scoreValue.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)
                : "-";
            var visits = analysis.Visits?.ToString(CultureInfo.InvariantCulture) ?? "-";
            DrawFittedText(
                $"SCORE {score}   VISITS {visits}",
                new Rectangle(pvValueBounds.X, bounds.Bottom - 26, pvValueBounds.Width, 20),
                new Color(118, 139, 143),
                0.25f);
        }

    }

    private void DrawMoveAnalysisTooltip(GoGameMove? move, Rectangle sectionBounds, Point mousePoint, Rectangle bounds)
    {
        var pv = move?.Analysis?.PrincipalVariation ?? "";
        var pvValueBounds = GetMoveAnalysisPvValueBounds(sectionBounds);
        if (!pvValueBounds.Contains(mousePoint) || pv.Length <= 44) return;

        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(30, 36, 43, 252));
        DrawRect(bounds, 2, new Color(147, 244, 200));
        DrawText("PRINCIPAL VARIATION", new Vector2(bounds.X + 18, bounds.Y + 12), new Color(180, 195, 195), 0.3f);
        DrawFittedText(AbbreviateOptionValue(pv, 120), new Rectangle(bounds.X + 18, bounds.Y + 46, bounds.Width - 36, 42), Color.White, 0.36f);
    }

    private static Rectangle GetMoveAnalysisPvValueBounds(Rectangle sectionBounds) =>
        new(sectionBounds.X + 184, sectionBounds.Y + 70, sectionBounds.Width - 204, 40);
}
