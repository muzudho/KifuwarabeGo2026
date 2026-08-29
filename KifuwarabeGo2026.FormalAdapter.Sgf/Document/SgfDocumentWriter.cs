namespace KifuwarabeGo2026.FormalAdapter.Sgf.Document;

/// <summary>Writes a canonical SGF collection while retaining the complete document model.</summary>
public static class SgfDocumentWriter
{
    public static string Write(SgfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.GameTrees.Count == 0)
            throw new ArgumentException("An SGF collection must contain at least one game tree.", nameof(document));

        var output = new System.Text.StringBuilder();
        foreach (var tree in document.GameTrees) WriteGameTree(output, tree);
        return output.ToString();
    }

    private static void WriteGameTree(System.Text.StringBuilder output, SgfGameTree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        if (tree.Sequence.Count == 0)
            throw new ArgumentException("An SGF game tree must contain at least one node.", nameof(tree));

        output.Append('(');
        foreach (var node in tree.Sequence) WriteNode(output, node);
        foreach (var variation in tree.Variations) WriteGameTree(output, variation);
        output.Append(')');
    }

    private static void WriteNode(System.Text.StringBuilder output, SgfNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        output.Append(';');
        foreach (var property in node.Properties)
        {
            output.Append(property.Identifier);
            if (property.Values.Count == 0)
                throw new ArgumentException($"Property {property.Identifier} must contain at least one value.", nameof(node));
            foreach (var value in property.Values)
            {
                output.Append('[');
                AppendEscaped(output, value);
                output.Append(']');
            }
        }
    }

    private static void AppendEscaped(System.Text.StringBuilder output, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            switch (character)
            {
                case '\\': output.Append("\\\\"); break;
                case ']': output.Append("\\]"); break;
                case '\r':
                    if (index + 1 < value.Length && value[index + 1] == '\n') index++;
                    output.Append('\n');
                    break;
                default: output.Append(character); break;
            }
        }
    }
}
