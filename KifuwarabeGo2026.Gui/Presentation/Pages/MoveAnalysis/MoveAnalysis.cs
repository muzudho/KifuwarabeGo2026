namespace KifuwarabeGo2026.Gui.Presentation.Pages.MoveAnalysis;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;

/// <summary>1 手の勝率、主変化、評価値を表示する解析コンポーネントです。</summary>
public sealed class MoveAnalysis
{
    private static readonly Color AccentColor = new(76, 91, 126);

    /// <summary>解析の要約欄を描画します。</summary>
    public void DrawSection(GoGameMove? move, Rectangle bounds, MoveAnalysisDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var content = new Rectangle(bounds.X + 20, bounds.Y + 8, bounds.Width - 40, 52);
        draw.DrawVerticalSection(bounds, "ANALYSIS", AccentColor);

        var analysis = move?.Analysis;
        var winrate = analysis?.Winrate is { } rate
            ? $"{(move!.Value.Stone == GoStone.Black ? "BLACK" : "WHITE")} {rate:P1}"
            : "-";
        draw.DrawResultRow(content, "WINRATE", winrate, AccentColor, Color.White);

        var pvRow = new Rectangle(content.X, content.Y + 56, content.Width, 52);
        draw.DrawResultLabel(pvRow, "PV", AccentColor);
        var pvValueBounds = GetPrincipalVariationValueBounds(bounds);
        var principalVariation = analysis?.PrincipalVariation ?? string.Empty;
        draw.DrawFittedText(principalVariation.Length == 0 ? "-" : draw.Abbreviate(principalVariation, 44), pvValueBounds, Color.White, 0.42f);

        if (analysis is null) return;
        var score = analysis.Score is { } scoreValue ? scoreValue.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) : "-";
        var visits = analysis.Visits?.ToString(CultureInfo.InvariantCulture) ?? "-";
        draw.DrawFittedText($"SCORE {score}   VISITS {visits}", new Rectangle(pvValueBounds.X, bounds.Bottom - 26, pvValueBounds.Width, 20), new Color(118, 139, 143), 0.25f);
    }

    /// <summary>省略されている主変化にマウスが重なった場合に詳細を描画します。</summary>
    public void DrawTooltip(GoGameMove? move, Rectangle sectionBounds, Point mousePoint, Rectangle bounds, MoveAnalysisDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var principalVariation = move?.Analysis?.PrincipalVariation ?? string.Empty;
        if (!GetPrincipalVariationValueBounds(sectionBounds).Contains(mousePoint) || principalVariation.Length <= 44) return;

        draw.FillRectangle(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 150));
        draw.FillRectangle(bounds, new Color(30, 36, 43, 252));
        draw.DrawRectangle(bounds, 2, new Color(147, 244, 200));
        draw.DrawText("PRINCIPAL VARIATION", new Vector2(bounds.X + 18, bounds.Y + 12), new Color(180, 195, 195), 0.3f);
        draw.DrawFittedText(draw.Abbreviate(principalVariation, 120), new Rectangle(bounds.X + 18, bounds.Y + 46, bounds.Width - 36, 42), Color.White, 0.36f);
    }

    /// <summary>主変化の値を表示する領域です。</summary>
    public static Rectangle GetPrincipalVariationValueBounds(Rectangle sectionBounds) =>
        new(sectionBounds.X + 184, sectionBounds.Y + 70, sectionBounds.Width - 204, 40);
}

/// <summary>MoveAnalysis に渡す描画機能です。</summary>
public sealed record MoveAnalysisDrawingCallbacks(
    Action<Rectangle, string, Color> DrawVerticalSection,
    Action<Rectangle, string, string, Color, Color> DrawResultRow,
    Action<Rectangle, string, Color> DrawResultLabel,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Func<string, int, string> Abbreviate);
