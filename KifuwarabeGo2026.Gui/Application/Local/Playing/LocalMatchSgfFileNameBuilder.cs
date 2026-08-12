namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using System;
using System.Linq;

/// <summary>LocalMatch の棋譜保存時に使う、安全で識別しやすい初期ファイル名を作成する。</summary>
public static class LocalMatchSgfFileNameBuilder
{
    public static string Create(string blackPresentedName, string whitePresentedName, DateTime startedAt) =>
        $"kifuwarabe-go-{NormalizeName(blackPresentedName, "black")}-vs-{NormalizeName(whitePresentedName, "white")}-{startedAt:yyyyMMdd-HHmmss}.sgf";

    private static string NormalizeName(string value, string fallback)
    {
        var normalized = new string((value ?? "").Trim()
            .Select(character => character < ' ' || "<>:\"/\\|?*".Contains(character) ? '_' : character)
            .ToArray())
            .Trim(' ', '.');
        return string.IsNullOrWhiteSpace(normalized)
            ? fallback
            : normalized[..Math.Min(normalized.Length, 48)];
    }
}
