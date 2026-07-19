namespace KifuwarabeGo2026.Sgf;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class SgfGameRecordConverter
{
    public static string ToSgf(GoGameRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var builder = new StringBuilder();
        builder.Append("(;FF[4]GM[1]CA[UTF-8]AP[KifuwarabeGo2026]");
        AppendProperty(builder, "SZ", record.BoardSize.ToString(CultureInfo.InvariantCulture));
        AppendProperty(builder, "KM", record.Komi.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(record.RuleName))
        {
            AppendProperty(builder, "RU", record.RuleName);
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

        AppendSetupStones(builder, record.SetupStones, GoStone.Black, "AB", record.BoardSize);
        AppendSetupStones(builder, record.SetupStones, GoStone.White, "AW", record.BoardSize);

        foreach (var move in record.Moves)
        {
            builder.Append(';');
            AppendProperty(builder, move.Stone == GoStone.Black ? "B" : "W", SgfCoordinate.FormatPoint(move.Point, record.BoardSize));
            if (!string.IsNullOrWhiteSpace(move.Comment))
            {
                AppendProperty(builder, "C", move.Comment);
            }
        }

        builder.Append(')');
        return builder.ToString();
    }

    public static GoGameRecord FromSgf(string sgf)
    {
        ArgumentNullException.ThrowIfNull(sgf);

        var nodes = new Parser(sgf).ParseMainSequence();
        if (nodes.Count == 0)
        {
            throw new SgfParseException("SGF game tree has no nodes.");
        }

        var record = new GoGameRecord();
        ApplyRootProperties(record, nodes[0]);

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

            sawMove |= AppendMoveIfPresent(record, node, "B", GoStone.Black);
            sawMove |= AppendMoveIfPresent(record, node, "W", GoStone.White);
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

    private static bool AppendMoveIfPresent(GoGameRecord record, Dictionary<string, List<string>> node, string propertyName, GoStone stone)
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

        var comment = TryGetSingleValue(node, "C", out var nodeComment) ? nodeComment : "";
        record.Moves.Add(new GoGameMove(stone, point, comment));
        return true;
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

    private static string EscapeValue(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);
    }

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
