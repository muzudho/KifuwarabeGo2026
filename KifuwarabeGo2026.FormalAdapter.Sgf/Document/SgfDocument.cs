namespace KifuwarabeGo2026.FormalAdapter.Sgf.Document;

/// <summary>Represents one SGF collection without applying game-specific meaning.</summary>
public sealed class SgfDocument
{
    public IList<SgfGameTree> GameTrees { get; } = new List<SgfGameTree>();
}

/// <summary>Represents a sequence and the variations branching after that sequence.</summary>
public sealed class SgfGameTree
{
    public IList<SgfNode> Sequence { get; } = new List<SgfNode>();

    public IList<SgfGameTree> Variations { get; } = new List<SgfGameTree>();
}

/// <summary>Represents one SGF node while retaining property order.</summary>
public sealed class SgfNode
{
    public IList<SgfProperty> Properties { get; } = new List<SgfProperty>();
}

/// <summary>Represents one property while retaining all values and their order.</summary>
public sealed class SgfProperty
{
    public SgfProperty(string identifier, IEnumerable<string> values)
    {
        if (string.IsNullOrEmpty(identifier) || identifier.Any(character => character is < 'A' or > 'Z'))
            throw new ArgumentException("An SGF property identifier must contain uppercase ASCII letters.", nameof(identifier));

        Identifier = identifier;
        foreach (var value in values ?? throw new ArgumentNullException(nameof(values)))
            Values.Add(value ?? throw new ArgumentException("An SGF property value cannot be null.", nameof(values)));
        if (Values.Count == 0)
            throw new ArgumentException("An SGF property must contain at least one value.", nameof(values));
    }

    public string Identifier { get; }

    public IList<string> Values { get; } = new List<string>();
}
