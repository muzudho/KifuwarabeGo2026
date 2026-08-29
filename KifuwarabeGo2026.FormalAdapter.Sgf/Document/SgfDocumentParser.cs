namespace KifuwarabeGo2026.FormalAdapter.Sgf.Document;

/// <summary>Parses the complete SGF collection grammar without discarding variations or unknown properties.</summary>
public static class SgfDocumentParser
{
    public static SgfDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new Parser(text).ParseDocument();
    }

    private sealed class Parser(string text)
    {
        private int _offset;

        public SgfDocument ParseDocument()
        {
            var document = new SgfDocument();
            SkipWhitespace();
            while (!AtEnd)
            {
                if (Current != '(') throw Error("Expected an SGF game tree");
                document.GameTrees.Add(ParseGameTree());
                SkipWhitespace();
            }

            if (document.GameTrees.Count == 0) throw Error("An SGF collection must contain at least one game tree");
            return document;
        }

        private SgfGameTree ParseGameTree()
        {
            Expect('(');
            SkipWhitespace();
            var tree = new SgfGameTree();
            while (!AtEnd && Current == ';')
            {
                tree.Sequence.Add(ParseNode());
                SkipWhitespace();
            }

            if (tree.Sequence.Count == 0) throw Error("An SGF game tree must contain a node sequence");
            while (!AtEnd && Current == '(')
            {
                tree.Variations.Add(ParseGameTree());
                SkipWhitespace();
            }

            Expect(')');
            return tree;
        }

        private SgfNode ParseNode()
        {
            Expect(';');
            SkipWhitespace();
            var node = new SgfNode();
            while (!AtEnd && Current is >= 'A' and <= 'Z')
            {
                var identifier = ParseIdentifier();
                SkipWhitespace();
                var values = new List<string>();
                while (!AtEnd && Current == '[')
                {
                    values.Add(ParseValue());
                    SkipWhitespace();
                }

                if (values.Count == 0) throw Error($"Property {identifier} must contain a value");
                node.Properties.Add(new SgfProperty(identifier, values));
            }

            if (!AtEnd && char.IsAsciiLetter(Current) && Current is not (>= 'A' and <= 'Z'))
                throw Error("An SGF property identifier must use uppercase ASCII letters");
            return node;
        }

        private string ParseIdentifier()
        {
            var start = _offset;
            while (!AtEnd && Current is >= 'A' and <= 'Z') _offset++;
            return text[start.._offset];
        }

        private string ParseValue()
        {
            Expect('[');
            var value = new System.Text.StringBuilder();
            while (!AtEnd)
            {
                var character = text[_offset++];
                if (character == ']') return value.ToString();
                if (character == '\\')
                {
                    if (AtEnd) throw Error("An SGF property value cannot end with an escape");
                    character = text[_offset++];
                    if (character == '\r')
                    {
                        if (!AtEnd && Current == '\n') _offset++;
                        continue;
                    }
                    if (character == '\n') continue;
                    value.Append(character);
                    continue;
                }

                if (character == '\r')
                {
                    if (!AtEnd && Current == '\n') _offset++;
                    value.Append('\n');
                }
                else
                {
                    value.Append(character);
                }
            }

            throw Error("Unterminated SGF property value");
        }

        private void SkipWhitespace()
        {
            while (!AtEnd && char.IsWhiteSpace(Current)) _offset++;
        }

        private void Expect(char expected)
        {
            if (AtEnd || Current != expected) throw Error($"Expected '{expected}'");
            _offset++;
        }

        private bool AtEnd => _offset >= text.Length;
        private char Current => text[_offset];
        private SgfParseException Error(string message) => new(message, _offset);
    }
}
