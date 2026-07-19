namespace KifuwarabeGo2026.Infrastructure.FileSystem;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using System;
using System.IO;
using System.Text;

/// <summary>
/// CGOS 対局の SGF ファイル名を組み立てます。
/// </summary>
public static class CgosSgfFileNameBuilder
{
    /// <summary>
    /// CGOS 対局の内容から Windows で安全な SGF ファイル名を作成します。
    /// </summary>
    public static string Create(CgosConnectionProfile profile, CgosGameObservation observation)
    {
        const int maxBaseNameLength = 176;
        var dateTime = observation.StartedAt.ToString("yyyyMMdd-HHmmss");
        var black = SanitizePart(observation.BlackPlayerName, "BLACK");
        var white = SanitizePart(observation.WhitePlayerName, "WHITE");
        var eventName = SanitizePart(profile.Event, "EVENT");
        var role = SanitizePart(profile.Role, "ROLE");
        var baseName = $"{eventName}_{role}_{black}_{white}_{dateTime}";
        if (baseName.Length > maxBaseNameLength)
            baseName = $"{black}_{white}_{dateTime}";

        if (baseName.Length > maxBaseNameLength)
        {
            black = black[..Math.Min(70, black.Length)];
            white = white[..Math.Min(70, white.Length)];
            baseName = $"{black}_{white}_{dateTime}";
        }

        return baseName + ".sgf";
    }

    /// <summary>
    /// ファイル名に使用できない文字を構成要素から除去します。
    /// </summary>
    private static string SanitizePart(string text, string fallback)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(text.Length);
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedToken = NormalizeUppercaseToken(token);
            foreach (var character in normalizedToken)
            {
                if (character >= ' ' && Array.IndexOf(invalidCharacters, character) < 0)
                    builder.Append(character);
            }
        }

        var sanitized = builder.ToString().Trim(' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    /// <summary>
    /// すべて大文字の英単語を、先頭だけ大文字の表記へ変換します。
    /// </summary>
    private static string NormalizeUppercaseToken(string token)
    {
        var containsLetter = false;
        foreach (var character in token)
        {
            if (!char.IsAsciiLetter(character)) continue;
            containsLetter = true;
            if (char.IsLower(character)) return token;
        }

        if (!containsLetter) return token;
        var characters = token.ToLowerInvariant().ToCharArray();
        for (var index = 0; index < characters.Length; index++)
        {
            if (!char.IsAsciiLetter(characters[index])) continue;
            characters[index] = char.ToUpperInvariant(characters[index]);
            break;
        }

        return new string(characters);
    }
}
