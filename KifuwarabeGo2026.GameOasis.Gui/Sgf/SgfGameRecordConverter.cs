namespace KifuwarabeGo2026.GameOasis.Gui.Sgf;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

public static class SgfGameRecordConverter
{
    private static readonly string ApplicationPropertyValue = CreateApplicationPropertyValue();

    /// <summary>
    /// Reads a supported legacy SGF and writes it in the current interoperable form.
    /// Source files are never overwritten by this conversion.
    /// </summary>
    public static string UpgradeToCurrentFormat(string sgf) =>
        ToSgf(FromSgf(sgf));

    /// <summary>
    /// Renames the legacy KFA property to KFW without reformatting the SGF.
    /// Text inside property values, including comments and JSON strings, is left unchanged.
    /// </summary>
    public static string ConvertKfaToKfw(string sgf)
    {
        ArgumentNullException.ThrowIfNull(sgf);

        var builder = new StringBuilder(sgf.Length);
        var index = 0;
        while (index < sgf.Length)
        {
            if (sgf[index] == '[')
            {
                AppendPropertyValueVerbatim(builder, sgf, ref index);
                continue;
            }

            if (sgf[index] is >= 'A' and <= 'Z')
            {
                var nameStart = index;
                while (index < sgf.Length && sgf[index] is >= 'A' and <= 'Z')
                {
                    index++;
                }

                var name = sgf.AsSpan(nameStart, index - nameStart);
                if (name.SequenceEqual("KFA"))
                {
                    builder.Append("KFW");
                }
                else
                {
                    builder.Append(name);
                }
                continue;
            }

            builder.Append(sgf[index++]);
        }

        return builder.ToString();
    }

    public static string ToSgf(GoGameRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var builder = new StringBuilder();
        builder.Append("(;GM[1]FF[4]CA[UTF-8]");
        AppendProperty(builder, "AP", ApplicationPropertyValue);
        builder.Append('\n');
        if (!string.IsNullOrWhiteSpace(record.RuleName))
        {
            AppendProperty(builder, "RU", record.RuleName);
        }
        AppendProperty(builder, "SZ", record.BoardSize.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "KM", record.Komi.ToString(CultureInfo.InvariantCulture));
        if (record.TimeLimit > TimeSpan.Zero)
        {
            AppendProperty(
                builder,
                "TM",
                record.TimeLimit.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        }

        var hasGameInformation =
            !string.IsNullOrWhiteSpace(record.GameName) ||
            !string.IsNullOrWhiteSpace(record.BlackPlayerName) ||
            !string.IsNullOrWhiteSpace(record.WhitePlayerName) ||
            record.SetupStones.Count > 0;
        if (hasGameInformation)
        {
            builder.Append('\n');
        }
        if (!string.IsNullOrWhiteSpace(record.GameName))
        {
            AppendProperty(builder, "GN", record.GameName);
        }

        if (!string.IsNullOrWhiteSpace(record.BlackPlayerName))
        {
            AppendProperty(builder, "PB", record.BlackPlayerName);
        }

        if (!string.IsNullOrWhiteSpace(record.WhitePlayerName))
        {
            AppendProperty(builder, "PW", record.WhitePlayerName);
        }
        if (!string.IsNullOrWhiteSpace(record.BlackRank))
        {
            AppendProperty(builder, "BR", record.BlackRank);
        }
        if (!string.IsNullOrWhiteSpace(record.WhiteRank))
        {
            AppendProperty(builder, "WR", record.WhiteRank);
        }
        if (!string.IsNullOrWhiteSpace(record.PlayedDate))
        {
            AppendProperty(builder, "DT", record.PlayedDate);
        }
        if (!string.IsNullOrWhiteSpace(record.Place))
        {
            AppendProperty(builder, "PC", record.Place);
        }
        if (!string.IsNullOrWhiteSpace(record.Result))
        {
            AppendProperty(builder, "RE", record.Result);
        }

        AppendSetupStones(builder, record.SetupStones, GoStone.Black, "AB", record.BoardSize);
        AppendSetupStones(builder, record.SetupStones, GoStone.White, "AW", record.BoardSize);
        if (!string.IsNullOrWhiteSpace(record.RootComment))
        {
            AppendCommentProperty(builder, record.RootComment);
        }

        foreach (var move in record.Moves)
        {
            builder.Append('\n').Append(';');
            AppendProperty(builder, move.Stone == GoStone.Black ? "B" : "W", SgfCoordinate.FormatPoint(move.Point, record.BoardSize));
            if (move.TimeLeftAfterMove is { } timeLeft)
            {
                AppendProperty(
                    builder,
                    move.Stone == GoStone.Black ? "BL" : "WL",
                    Math.Max(0, timeLeft.TotalSeconds).ToString("0.###", CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrWhiteSpace(move.Comment))
            {
                AppendCommentProperty(builder, move.Comment);
            }
            if (move.CommonAnalysisJson is not null)
            {
                AppendProperty(builder, "CC", move.CommonAnalysisJson);
            }
            else if (move.Analysis is not null)
            {
                AppendProperty(builder, "CC", SerializeAnalysis(move, record.BoardSize));
            }
            else if (move.LegacyKifuwarabeAnalysisJson is not null)
            {
                AppendProperty(builder, "KFW", move.LegacyKifuwarabeAnalysisJson);
            }
        }

        builder.Append('\n').Append(')');
        return builder.ToString();
    }

    public static GoGameRecord FromSgf(string sgf)
    {
        ArgumentNullException.ThrowIfNull(sgf);

        List<Dictionary<string, List<string>>> nodes;
        try
        {
            var document = SgfDocumentParser.Parse(sgf);
            nodes = document.GameTrees[0].Sequence.Select(ToPropertyDictionary).ToList();
        }
        catch (KifuwarabeGo2026.FormalAdapter.Sgf.Document.SgfParseException exception)
        {
            throw new SgfParseException(exception.Message);
        }
        if (nodes.Count == 0)
        {
            throw new SgfParseException("SGF game tree has no nodes.");
        }

        var record = new GoGameRecord();
        ApplyRootProperties(record, nodes[0]);
        var readLegacyKifuwarabeAnalysis =
            !TryGetSingleValue(nodes[0], "KFAV", out var analysisVersion) ||
            analysisVersion == "1";

        var sawMove = false;
        foreach (var node in nodes)
        {
            if (node.ContainsKey("AB") || node.ContainsKey("AW"))
            {
                if (sawMove)
                {
                    throw new SgfParseException("SGF setup stones after moves are not supported by GoGameRecord.");
                }

                ApplySetupStones(record, node, "AB", GoStone.Black);
                ApplySetupStones(record, node, "AW", GoStone.White);
            }

            sawMove |= AppendMoveIfPresent(record, node, "B", GoStone.Black, readLegacyKifuwarabeAnalysis);
            sawMove |= AppendMoveIfPresent(record, node, "W", GoStone.White, readLegacyKifuwarabeAnalysis);
        }

        return record;
    }

    private static void ApplyRootProperties(GoGameRecord record, Dictionary<string, List<string>> root)
    {
        if (TryGetSingleValue(root, "GM", out var gameKind) && gameKind != "1")
        {
            throw new SgfParseException($"Unsupported SGF game kind GM[{gameKind}].");
        }

        if (TryGetSingleValue(root, "SZ", out var sizeText))
        {
            if (!int.TryParse(sizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var boardSize))
            {
                throw new SgfParseException($"Invalid SGF board size SZ[{sizeText}].");
            }

            record.BoardSize = boardSize;
        }

        if (TryGetSingleValue(root, "KM", out var komiText))
        {
            if (!decimal.TryParse(komiText, NumberStyles.Number, CultureInfo.InvariantCulture, out var komi))
            {
                throw new SgfParseException($"Invalid SGF komi KM[{komiText}].");
            }

            record.Komi = komi;
        }

        if (TryGetSingleValue(root, "TM", out var timeLimitText))
        {
            if (!double.TryParse(
                    timeLimitText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var timeLimitSeconds) ||
                !double.IsFinite(timeLimitSeconds) ||
                timeLimitSeconds < 0 ||
                timeLimitSeconds > TimeSpan.MaxValue.TotalSeconds)
            {
                throw new SgfParseException($"Invalid SGF time limit TM[{timeLimitText}].");
            }

            record.TimeLimit = TimeSpan.FromSeconds(timeLimitSeconds);
        }

        if (TryGetSingleValue(root, "RU", out var ruleName))
        {
            record.RuleName = ruleName;
        }

        if (TryGetSingleValue(root, "GN", out var gameName))
        {
            record.GameName = gameName;
        }

        if (TryGetSingleValue(root, "PB", out var blackPlayerName))
        {
            record.BlackPlayerName = blackPlayerName;
        }

        if (TryGetSingleValue(root, "PW", out var whitePlayerName))
        {
            record.WhitePlayerName = whitePlayerName;
        }

        if (TryGetSingleValue(root, "BR", out var blackRank))
        {
            record.BlackRank = blackRank;
        }

        if (TryGetSingleValue(root, "WR", out var whiteRank))
        {
            record.WhiteRank = whiteRank;
        }

        if (TryGetSingleValue(root, "DT", out var playedDate))
        {
            record.PlayedDate = playedDate;
        }

        if (TryGetSingleValue(root, "PC", out var place))
        {
            record.Place = place;
        }

        if (TryGetSingleValue(root, "RE", out var result))
        {
            record.Result = result;
        }
        if (TryGetSingleValue(root, "C", out var rootComment))
        {
            record.RootComment = NormalizeCommentLineEndings(rootComment);
        }
    }

    private static void ApplySetupStones(GoGameRecord record, Dictionary<string, List<string>> node, string propertyName, GoStone stone)
    {
        if (!node.TryGetValue(propertyName, out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            if (!SgfCoordinate.TryParsePoint(value, record.BoardSize, out var point) || point is null)
            {
                throw new SgfParseException($"Invalid SGF setup point {propertyName}[{value}].");
            }

            record.SetupStones.Add(new GoGameSetupStone(stone, point.Value));
        }
    }

    private static bool AppendMoveIfPresent(
        GoGameRecord record,
        Dictionary<string, List<string>> node,
        string propertyName,
        GoStone stone,
        bool readLegacyKifuwarabeAnalysis)
    {
        if (!node.TryGetValue(propertyName, out var values))
        {
            return false;
        }

        if (values.Count != 1)
        {
            throw new SgfParseException($"SGF move property {propertyName} must have one value.");
        }

        if (!SgfCoordinate.TryParsePoint(values[0], record.BoardSize, out var point))
        {
            throw new SgfParseException($"Invalid SGF move point {propertyName}[{values[0]}].");
        }

        var comment = TryGetSingleValue(node, "C", out var nodeComment) ? NormalizeCommentLineEndings(nodeComment) : "";
        var playedVertex = point is { } playedPoint ? GtpCoordinate.FormatVertex(playedPoint, record.BoardSize) : "pass";
        TimeSpan? timeLeftAfterMove = null;
        var timePropertyName = stone == GoStone.Black ? "BL" : "WL";
        if (TryGetSingleValue(node, timePropertyName, out var timeLeftText) &&
            double.TryParse(timeLeftText, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeLeftSeconds) &&
            double.IsFinite(timeLeftSeconds) && timeLeftSeconds >= 0 && timeLeftSeconds <= TimeSpan.MaxValue.TotalSeconds)
        {
            timeLeftAfterMove = TimeSpan.FromSeconds(timeLeftSeconds);
        }
        GoMoveAnalysis? analysis = null;
        string? commonAnalysisJson = null;
        string? legacyKifuwarabeAnalysisJson = null;
        if (TryGetSingleValue(node, "CC", out var commonJson))
        {
            analysis = CgosMoveAnalysisParser.Parse(commonJson, playedVertex);
            commonAnalysisJson = commonJson;
        }

        if (analysis is null &&
            (TryGetSingleValue(node, "KFW", out var legacyAnalysisJson) ||
             (readLegacyKifuwarabeAnalysis && TryGetSingleValue(node, "KFA", out legacyAnalysisJson))))
        {
            analysis = CgosMoveAnalysisParser.Parse(legacyAnalysisJson, playedVertex);
            if (analysis is null)
            {
                legacyKifuwarabeAnalysisJson = legacyAnalysisJson;
            }
        }
        record.Moves.Add(new GoGameMove(
            stone,
            point,
            comment,
            analysis,
            commonAnalysisJson,
            legacyKifuwarabeAnalysisJson,
            timeLeftAfterMove));
        return true;
    }

    /// <summary>zakki CGOS analysis JSON と同じキーで、着手に対応する解析を保存します。</summary>
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

    private static bool TryGetSingleValue(Dictionary<string, List<string>> node, string propertyName, out string value)
    {
        value = "";
        if (!node.TryGetValue(propertyName, out var values) || values.Count == 0)
        {
            return false;
        }

        value = values[0];
        return true;
    }

    private static string CreateApplicationPropertyValue()
    {
        var version = typeof(SgfGameRecordConverter).Assembly.GetName().Version;
        return version is null
            ? "KifuwarabeGo2026"
            : $"KifuwarabeGo2026:{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void AppendSetupStones(
        StringBuilder builder,
        IEnumerable<GoGameSetupStone> setupStones,
        GoStone stone,
        string propertyName,
        int boardSize)
    {
        var wroteProperty = false;
        foreach (var setupStone in setupStones)
        {
            if (setupStone.Stone != stone)
            {
                continue;
            }

            if (!wroteProperty)
            {
                builder.Append(propertyName);
                wroteProperty = true;
            }

            builder.Append('[').Append(EscapeValue(SgfCoordinate.FormatPoint(setupStone.Point, boardSize))).Append(']');
        }
    }

    private static void AppendProperty(StringBuilder builder, string name, string value)
    {
        builder.Append(name).Append('[').Append(EscapeValue(value)).Append(']');
    }

    private static void AppendCommentProperty(StringBuilder builder, string value) =>
        AppendProperty(builder, "C", NormalizeCommentLineEndings(value));

    private static string NormalizeCommentLineEndings(string? value) =>
        (value ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string EscapeValue(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);
    }

    private static void AppendPropertyValueVerbatim(StringBuilder builder, string sgf, ref int index)
    {
        builder.Append(sgf[index++]);
        while (index < sgf.Length)
        {
            var ch = sgf[index++];
            builder.Append(ch);
            if (ch == '\\' && index < sgf.Length)
            {
                builder.Append(sgf[index++]);
            }
            else if (ch == ']')
            {
                return;
            }
        }
    }

    private static Dictionary<string, List<string>> ToPropertyDictionary(SgfNode node)
    {
        var properties = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var property in node.Properties)
        {
            if (!properties.TryGetValue(property.Identifier, out var values))
            {
                values = [];
                properties.Add(property.Identifier, values);
            }
            values.AddRange(property.Values);
        }
        return properties;
    }

    [Obsolete("GUI SGF parsing is provided by FormalAdapter.Sgf.Document.")]
    private sealed class Parser
    {
        private readonly string _text;
        private int _index;

        public Parser(string text)
        {
            _text = text;
        }

        public List<Dictionary<string, List<string>>> ParseMainSequence()
        {
            SkipWhiteSpace();
            Expect('(');
            var nodes = ParseSequence();

            // Ignore variations after the main sequence; they can be preserved later
            // by extending GoGameRecord without changing this public API.
            while (Peek() == '(')
            {
                SkipGameTree();
                SkipWhiteSpace();
            }

            Expect(')');
            SkipWhiteSpace();
            if (_index != _text.Length)
            {
                throw Error("Unexpected content after SGF game tree.");
            }

            return nodes;
        }

        private List<Dictionary<string, List<string>>> ParseSequence()
        {
            var nodes = new List<Dictionary<string, List<string>>>();
            SkipWhiteSpace();
            while (Peek() == ';')
            {
                nodes.Add(ParseNode());
                SkipWhiteSpace();
            }

            return nodes;
        }

        private Dictionary<string, List<string>> ParseNode()
        {
            Expect(';');
            var properties = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            SkipWhiteSpace();

            while (IsPropertyNameStart(Peek()))
            {
                var name = ParsePropertyName();
                SkipWhiteSpace();
                if (Peek() != '[')
                {
                    throw Error($"SGF property {name} has no value.");
                }

                if (!properties.TryGetValue(name, out var values))
                {
                    values = new List<string>();
                    properties.Add(name, values);
                }

                while (Peek() == '[')
                {
                    values.Add(ParsePropertyValue());
                    SkipWhiteSpace();
                }
            }

            return properties;
        }

        private string ParsePropertyName()
        {
            var start = _index;
            while (IsPropertyNameStart(Peek()))
            {
                _index++;
            }

            return _text[start.._index];
        }

        private string ParsePropertyValue()
        {
            Expect('[');
            var builder = new StringBuilder();
            while (_index < _text.Length)
            {
                var ch = _text[_index++];
                if (ch == ']')
                {
                    return builder.ToString();
                }

                if (ch == '\\')
                {
                    if (_index >= _text.Length)
                    {
                        throw Error("SGF property value ends with an escape character.");
                    }

                    var escaped = _text[_index++];
                    if (escaped == '\r' && Peek() == '\n')
                    {
                        _index++;
                        continue;
                    }

                    if (escaped is '\r' or '\n')
                    {
                        continue;
                    }

                    builder.Append(escaped);
                    continue;
                }

                builder.Append(ch);
            }

            throw Error("SGF property value is not closed.");
        }

        private void SkipGameTree()
        {
            Expect('(');
            var depth = 1;
            while (_index < _text.Length && depth > 0)
            {
                var ch = _text[_index++];
                if (ch == '[')
                {
                    SkipPropertyValueBody();
                    continue;
                }

                if (ch == '(')
                {
                    depth++;
                    continue;
                }

                if (ch == ')')
                {
                    depth--;
                }
            }

            if (depth != 0)
            {
                throw Error("SGF game tree is not closed.");
            }
        }

        private void SkipPropertyValueBody()
        {
            while (_index < _text.Length)
            {
                var ch = _text[_index++];
                if (ch == ']')
                {
                    return;
                }

                if (ch == '\\' && _index < _text.Length)
                {
                    _index++;
                }
            }

            throw Error("SGF property value is not closed.");
        }

        private void Expect(char expected)
        {
            SkipWhiteSpace();
            if (Peek() != expected)
            {
                throw Error($"Expected '{expected}'.");
            }

            _index++;
        }

        private char Peek() => _index < _text.Length ? _text[_index] : '\0';

        private void SkipWhiteSpace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }

        private SgfParseException Error(string message) => new($"{message} Offset: {_index}.");

        private static bool IsPropertyNameStart(char ch) => ch is >= 'A' and <= 'Z';
    }
}
