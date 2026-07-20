namespace KifuwarabeGo2026.Gui.Application.Cgos.Watching;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using System;
using System.Text.Json;

/// <summary>CGOSの着手行へ付加された解析JSONを読み取ります。</summary>
internal static class CgosMoveAnalysisParser
{
    public static GoMoveAnalysis? Parse(string? json, string playedVertex)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

            var root = document.RootElement;
            var source = FindPlayedMove(root, playedVertex) ?? root;
            var winrate = GetFiniteDouble(source, "winrate") ?? GetFiniteDouble(root, "winrate");
            if (winrate is < 0 or > 1) winrate = null;
            var score = GetFiniteDouble(source, "score") ?? GetFiniteDouble(root, "score");
            var visits = GetNonNegativeInt64(source, "visits") ?? GetNonNegativeInt64(root, "visits");
            var pv = GetString(source, "pv") ?? GetString(root, "pv") ?? "";
            if (pv.Length > 10_000) pv = pv[..9_997] + "...";

            return winrate is null && score is null && visits is null && pv.Length == 0
                ? null
                : new GoMoveAnalysis(winrate, pv, score, visits);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement? FindPlayedMove(JsonElement root, string playedVertex)
    {
        if (!root.TryGetProperty("moves", out var moves) || moves.ValueKind != JsonValueKind.Array) return null;

        JsonElement? first = null;
        foreach (var move in moves.EnumerateArray())
        {
            if (move.ValueKind != JsonValueKind.Object) continue;
            first ??= move;
            if (GetString(move, "move")?.Equals(playedVertex, StringComparison.OrdinalIgnoreCase) == true)
                return move;
        }

        return first;
    }

    private static double? GetFiniteDouble(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            return null;
        return number;
    }

    private static long? GetNonNegativeInt64(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || !value.TryGetInt64(out var number) || number < 0)
            return null;
        return number;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
