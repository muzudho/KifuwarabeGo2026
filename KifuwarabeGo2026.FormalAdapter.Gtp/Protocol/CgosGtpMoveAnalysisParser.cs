namespace KifuwarabeGo2026.FormalAdapter.Gtp.Protocol;

using System;
using System.Text.Json;

public sealed record CgosGtpMoveAnalysis(double? WinRate, string PrincipalVariation, double? Score, long? Visits);

/// <summary>CGOSの着手行へ付加された解析JSONを読み取ります。</summary>
public static class CgosGtpMoveAnalysisParser
{
    public static (string Vertex, string? AnalysisJson) ParseResponse(string payload)
    {
        var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var play = lines.LastOrDefault(line => line.StartsWith("play ", StringComparison.OrdinalIgnoreCase));
        if (play is null || play[5..].Trim() is not { Length: > 0 } vertex)
            throw new InvalidDataException("cgos-genmove_analyze response has no play command.");
        return (vertex, lines.FirstOrDefault(line => line.StartsWith('{')));
    }

    public static CgosGtpMoveAnalysis? Parse(string? json, string playedVertex)
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
                : new CgosGtpMoveAnalysis(winrate, pv, score, visits);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string ParseComment(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return "";
            var comment = GetString(document.RootElement, "comment") ?? "";
            return comment.Length <= 100_000 ? comment : comment[..99_997] + "...";
        }
        catch (JsonException)
        {
            return "";
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
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            return null;
        return number;
    }

    private static long? GetNonNegativeInt64(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number) || number < 0)
            return null;
        return number;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
