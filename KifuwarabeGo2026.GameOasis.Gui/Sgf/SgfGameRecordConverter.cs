namespace KifuwarabeGo2026.GameOasis.Gui.Sgf;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;

/// <summary>Compatibility entry point between the GUI record and the formal SGF adapter.</summary>
public static class SgfGameRecordConverter
{
    private static readonly string ApplicationPropertyValue = CreateApplicationPropertyValue();

    public static string UpgradeToCurrentFormat(string sgf) => ToSgf(FromSgf(sgf));

    /// <summary>Renames legacy KFA properties without changing values or formatting.</summary>
    public static string ConvertKfaToKfw(string sgf)
    {
        ArgumentNullException.ThrowIfNull(sgf);
        var output = new StringBuilder(sgf.Length);
        var index = 0;
        while (index < sgf.Length)
        {
            if (sgf[index] == '[')
            {
                AppendPropertyValueVerbatim(output, sgf, ref index);
                continue;
            }
            if (sgf[index] is >= 'A' and <= 'Z')
            {
                var start = index;
                while (index < sgf.Length && sgf[index] is >= 'A' and <= 'Z') index++;
                var identifier = sgf.AsSpan(start, index - start);
                output.Append(identifier.SequenceEqual("KFA") ? "KFW" : identifier);
                continue;
            }
            output.Append(sgf[index++]);
        }
        return output.ToString();
    }

    public static string ToSgf(GoGameRecord record) =>
        SgfGoGameRecordConverter.Write(ToNeutralRecord(record), ApplicationPropertyValue);

    public static GoGameRecord FromSgf(string sgf)
    {
        try
        {
            return FromNeutralRecord(SgfGoGameRecordConverter.Parse(sgf));
        }
        catch (KifuwarabeGo2026.FormalAdapter.Sgf.Document.SgfParseException exception)
        {
            throw new SgfParseException(exception.Message);
        }
        catch (SgfGoConversionException exception)
        {
            throw new SgfParseException(exception.Message);
        }
    }

    private static SgfGoGameRecord ToNeutralRecord(GoGameRecord source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var target = new SgfGoGameRecord
        {
            GameName = source.GameName,
            RuleName = source.RuleName,
            BlackPlayerName = source.BlackPlayerName,
            WhitePlayerName = source.WhitePlayerName,
            BlackRank = source.BlackRank,
            WhiteRank = source.WhiteRank,
            PlayedDate = source.PlayedDate,
            Result = source.Result,
            Place = source.Place,
            RootComment = source.RootComment,
            BoardSize = source.BoardSize,
            Komi = source.Komi,
            TimeLimit = source.TimeLimit,
        };
        foreach (var setupStone in source.SetupStones)
            target.SetupStones.Add(new SgfGoSetupStone(setupStone.Stone, setupStone.Point));
        foreach (var move in source.Moves)
        {
            var (analysisIdentifier, analysisJson) = GetAnalysisDocument(move, source.BoardSize);
            target.Moves.Add(new SgfGoMove(
                move.Stone,
                move.Point,
                move.Comment,
                move.TimeLeftAfterMove,
                analysisIdentifier,
                analysisJson));
        }
        return target;
    }

    private static GoGameRecord FromNeutralRecord(SgfGoGameRecord source)
    {
        var target = new GoGameRecord
        {
            GameName = source.GameName,
            RuleName = source.RuleName,
            BlackPlayerName = source.BlackPlayerName,
            WhitePlayerName = source.WhitePlayerName,
            BlackRank = source.BlackRank,
            WhiteRank = source.WhiteRank,
            PlayedDate = source.PlayedDate,
            Result = source.Result,
            Place = source.Place,
            RootComment = source.RootComment,
            BoardSize = source.BoardSize,
            Komi = source.Komi,
            TimeLimit = source.TimeLimit,
        };
        foreach (var setupStone in source.SetupStones)
            target.SetupStones.Add(new GoGameSetupStone(setupStone.Stone, setupStone.Point));
        foreach (var move in source.Moves)
        {
            var playedVertex = move.Point is { } point ? GtpCoordinate.FormatVertex(point, source.BoardSize) : "pass";
            GoMoveAnalysis? analysis = null;
            string? commonAnalysisJson = null;
            string? legacyAnalysisJson = null;
            if (move.AnalysisJson is { } json)
            {
                analysis = CgosMoveAnalysisParser.Parse(json, playedVertex);
                if (move.AnalysisPropertyIdentifier == "CC") commonAnalysisJson = json;
                else if (analysis is null) legacyAnalysisJson = json;
            }
            target.Moves.Add(new GoGameMove(
                move.Stone,
                move.Point,
                move.Comment,
                analysis,
                commonAnalysisJson,
                legacyAnalysisJson,
                move.TimeLeftAfterMove));
        }
        return target;
    }

    private static (string? Identifier, string? Json) GetAnalysisDocument(GoGameMove move, int boardSize)
    {
        if (move.CommonAnalysisJson is not null) return ("CC", move.CommonAnalysisJson);
        if (move.Analysis is not null) return ("CC", SerializeAnalysis(move, boardSize));
        if (move.LegacyKifuwarabeAnalysisJson is not null) return ("KFW", move.LegacyKifuwarabeAnalysisJson);
        return (null, null);
    }

    private static string SerializeAnalysis(GoGameMove move, int boardSize)
    {
        var analysis = move.Analysis ?? throw new InvalidOperationException("Move analysis is required.");
        var playedVertex = move.Point is { } point ? GtpCoordinate.FormatVertex(point, boardSize) : "pass";
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("moves");
            writer.WriteStartObject();
            writer.WriteString("move", playedVertex);
            if (analysis.Winrate is { } winrate) writer.WriteNumber("winrate", winrate);
            if (analysis.Score is { } score) writer.WriteNumber("score", score);
            if (!string.IsNullOrWhiteSpace(analysis.PrincipalVariation)) writer.WriteString("pv", analysis.PrincipalVariation);
            if (analysis.Visits is { } visits) writer.WriteNumber("visits", visits);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateApplicationPropertyValue()
    {
        var version = typeof(SgfGameRecordConverter).Assembly.GetName().Version;
        return version is null ? "KifuwarabeGo2026" : $"KifuwarabeGo2026:{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void AppendPropertyValueVerbatim(StringBuilder output, string sgf, ref int index)
    {
        output.Append(sgf[index++]);
        while (index < sgf.Length)
        {
            var character = sgf[index++];
            output.Append(character);
            if (character == '\\' && index < sgf.Length) output.Append(sgf[index++]);
            else if (character == ']') return;
        }
    }
}
