namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal sealed class GoMatchEvaluationPanel : IDisposable
{
    private static readonly Color Muted = new(184, 199, 208);
    private static readonly Color WinRate = new(107, 225, 198);
    private static readonly Color Score = new(255, 205, 126);
    private readonly GoMatchText _text;
    private readonly Texture2D _pixel;

    public GoMatchEvaluationPanel(GraphicsDevice graphics)
    {
        _text = new GoMatchText(graphics);
        _pixel = new Texture2D(graphics, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public static Rectangle ToggleBounds(Rectangle viewport) =>
        new(Math.Max(viewport.X + 8, viewport.Right - 220), viewport.Y + 10, 208, 34);

    public static (Rectangle Board, Rectangle Panel) Layout(Rectangle viewport, bool open)
    {
        var content = new Rectangle(viewport.X, viewport.Y + 54, viewport.Width, Math.Max(1, viewport.Height - 54));
        if (!open) return (content, Rectangle.Empty);
        if (content.Width < 760 && content.Height >= 480)
        {
            var height = content.Height / 2;
            return (new(content.X, content.Y, content.Width, content.Height - height),
                new(content.X + 10, content.Bottom - height, content.Width - 20, height - 10));
        }
        var width = Math.Min(440, content.Width * 2 / 5);
        return (new(content.X, content.Y, content.Width - width, content.Height),
            new(content.Right - width, content.Y, width - 10, content.Height - 10));
    }

    public void DrawToggle(SpriteBatch batch, Rectangle bounds, bool open, bool hovered)
    {
        batch.Draw(_pixel, bounds, hovered ? new Color(63, 94, 105) : new Color(43, 66, 77));
        Text(batch, open ? "Hide analysis [Tab]" : "Show analysis [Tab]", bounds.X + 10, bounds.Y + 10,
            bounds.Width - 20, Color.White);
    }

    public void Draw(SpriteBatch batch, Rectangle bounds, int moveCount, IReadOnlyList<GoMatchEvaluation> evaluations)
    {
        if (bounds.Width < 80 || bounds.Height < 70) return;
        batch.Draw(_pixel, bounds, new Color(21, 30, 37));
        Text(batch, $"LIVE ANALYSIS   |   MOVE {moveCount}", bounds.X + 12, bounds.Y + 8, bounds.Width - 24, Color.White);
        if (evaluations.Count == 0)
        {
            Text(batch, "No engine evaluation available.", bounds.X + 12, bounds.Y + 43, bounds.Width - 24, Muted);
            return;
        }
        var chartHeight = (bounds.Height - 44) / 2;
        if (chartHeight < 62) return;
        var range = Math.Max(5d, evaluations.Max(value => Math.Abs(value.BlackScore ?? 0)));
        var chart = new Rectangle(bounds.X + 12, bounds.Y + 38, bounds.Width - 24, chartHeight);
        DrawChart(batch, chart, moveCount, evaluations, value => value.BlackWinRate,
            value => value, "Black win rate", value => FormattableString.Invariant($"{value * 100:0}%"), "100%", "0%", WinRate);
        chart.Offset(0, chartHeight);
        DrawChart(batch, chart, moveCount, evaluations, value => value.BlackScore,
            value => 0.5 + 0.5 * (value / range), "Black score lead",
            value => FormattableString.Invariant($"{value:+0.0;-0.0;0.0}"),
            FormattableString.Invariant($"+{range:0.#}"), FormattableString.Invariant($"-{range:0.#}"), Score);
    }

    private void DrawChart(SpriteBatch batch, Rectangle bounds, int moveCount, IReadOnlyList<GoMatchEvaluation> evaluations,
        Func<GoMatchEvaluation, double?> select, Func<double, double> normalize,
        string label, Func<double, string> format, string top, string bottom, Color color)
    {
        var latest = evaluations.LastOrDefault(value => select(value).HasValue);
        var title = latest is null ? $"{label}  -" : $"{label}  {format(select(latest)!.Value)}  #{latest.MoveNumber}";
        Text(batch, title, bounds.X, bounds.Y, bounds.Width, color);
        var plot = new Rectangle(bounds.X + 54, bounds.Y + 26, bounds.Width - 58, bounds.Height - 46);
        if (plot.Width < 8 || plot.Height < 8) return;
        batch.Draw(_pixel, plot, new Color(13, 20, 26));
        foreach (var y in new[] { plot.Y, plot.Center.Y, plot.Bottom })
            batch.Draw(_pixel, new Rectangle(plot.X, y, plot.Width, 1), new Color(76, 96, 107));
        Text(batch, top, bounds.X, plot.Y - 4, 49, Muted);
        Text(batch, bottom, bounds.X, plot.Bottom - 14, 49, Muted);
        Text(batch, "1", plot.X, plot.Bottom + 2, 38, Muted, 1);
        var last = moveCount.ToString();
        if (moveCount > 1)
            Text(batch, last, Math.Max(plot.X, plot.Right - GoMatchText.Measure(last, 1)),
                plot.Bottom + 2, plot.Width, Muted, 1);
        Vector2? previous = null;
        var previousMove = 0;
        foreach (var evaluation in evaluations)
        {
            if (select(evaluation) is not { } value) continue;
            var x = plot.X + (plot.Width - 1) * (evaluation.MoveNumber - 1f) / Math.Max(1, moveCount - 1);
            var y = plot.Bottom - (plot.Height - 1) * (float)normalize(value);
            var point = new Vector2(x, y);
            if (previous is { } start && evaluation.MoveNumber == previousMove + 1)
            {
                var segment = point - start;
                batch.Draw(_pixel, start, null, color, MathF.Atan2(segment.Y, segment.X), Vector2.Zero,
                    new Vector2(segment.Length(), 2), SpriteEffects.None, 0);
            }
            batch.Draw(_pixel, new Rectangle((int)x - 2, (int)y - 2, 5, 5), color);
            previous = point;
            previousMove = evaluation.MoveNumber;
        }
    }

    private void Text(SpriteBatch batch, string text, int x, int y, int width, Color color, int scale = 2) =>
        _text.Draw(batch, text, x, y, width, scale, color);

    public void Dispose()
    {
        _pixel.Dispose();
        _text.Dispose();
    }
}
