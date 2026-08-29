using KifuwarabeGo2026.FormalAdapter.Sgf.Document;

var collection = SgfDocumentParser.Parse(
    "  (;FF[4]GM[1]XX[first][second]C[close\\] slash\\\\ line\\\r\njoined];B[aa](;W[bb]C[left])(;W[cc]C[right]))\r\n" +
    "(;GM[2]ZZ[unknown])  ");

Require(collection.GameTrees.Count == 2, "Multiple game trees must be retained.");
var firstTree = collection.GameTrees[0];
Require(firstTree.Sequence.Count == 2, "The main sequence must be retained.");
Require(firstTree.Variations.Count == 2, "Variations must be retained in order.");
Require(firstTree.Sequence[0].Properties.Select(property => property.Identifier).SequenceEqual(["FF", "GM", "XX", "C"]),
    "Unknown properties and property order must be retained.");
Require(firstTree.Sequence[0].Properties[2].Values.SequenceEqual(["first", "second"]),
    "Multiple property values must be retained in order.");
Require(firstTree.Sequence[0].Properties[3].Values.Single() == "close] slash\\ linejoined",
    "Escaped brackets, backslashes, and line continuation must be decoded.");
Require(firstTree.Variations[0].Sequence[0].Properties.Single(property => property.Identifier == "W").Values.Single() == "bb",
    "The first variation must be retained.");
Require(firstTree.Variations[1].Sequence[0].Properties.Single(property => property.Identifier == "W").Values.Single() == "cc",
    "The second variation must be retained.");

var canonical = SgfDocumentWriter.Write(collection);
var reparsed = SgfDocumentParser.Parse(canonical);
RequireEquivalent(collection, reparsed);
Require(canonical.Contains("XX[first][second]", StringComparison.Ordinal), "The writer must retain unknown multi-value properties.");
Require(canonical.Contains("C[close\\] slash\\\\ linejoined]", StringComparison.Ordinal), "The writer must escape SGF values.");

foreach (var vectorName in new[] { "sgf-baseline.sgf", "sgf-legacy-kfa.sgf" })
{
    var vector = SgfDocumentParser.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors", vectorName)));
    RequireEquivalent(vector, SgfDocumentParser.Parse(SgfDocumentWriter.Write(vector)));
}

RequireParseFailure("", 0);
RequireParseFailure("(;FF[4]", 7);
RequireParseFailure("(;ff[4])", 2);
RequireParseFailure("(;C[unterminated)", 17);
RequireParseFailure("()", 1);

var created = new SgfDocument();
var createdTree = new SgfGameTree();
var createdNode = new SgfNode();
createdNode.Properties.Add(new SgfProperty("KFW", ["{\"future\":true}"]));
createdNode.Properties.Add(new SgfProperty("C", ["first\r\nsecond"]));
createdTree.Sequence.Add(createdNode);
created.GameTrees.Add(createdTree);
Require(SgfDocumentWriter.Write(created) == "(;KFW[{\"future\":true}]C[first\nsecond])",
    "A document assembled through the public model must be writable with normalized line endings.");

Console.WriteLine("PASS: SGF documents retained collections, variations, unknown properties, multiple values, and escaped content across canonical round trips.");

static void RequireEquivalent(SgfDocument expected, SgfDocument actual)
{
    Require(expected.GameTrees.Count == actual.GameTrees.Count, "The game-tree count must round-trip.");
    for (var index = 0; index < expected.GameTrees.Count; index++)
        RequireEquivalentTree(expected.GameTrees[index], actual.GameTrees[index]);
}

static void RequireEquivalentTree(SgfGameTree expected, SgfGameTree actual)
{
    Require(expected.Sequence.Count == actual.Sequence.Count, "The sequence length must round-trip.");
    for (var nodeIndex = 0; nodeIndex < expected.Sequence.Count; nodeIndex++)
    {
        var expectedProperties = expected.Sequence[nodeIndex].Properties;
        var actualProperties = actual.Sequence[nodeIndex].Properties;
        Require(expectedProperties.Count == actualProperties.Count, "The property count must round-trip.");
        for (var propertyIndex = 0; propertyIndex < expectedProperties.Count; propertyIndex++)
        {
            Require(expectedProperties[propertyIndex].Identifier == actualProperties[propertyIndex].Identifier,
                "Property identifiers and order must round-trip.");
            Require(expectedProperties[propertyIndex].Values.SequenceEqual(actualProperties[propertyIndex].Values),
                "Property values and order must round-trip.");
        }
    }

    Require(expected.Variations.Count == actual.Variations.Count, "The variation count must round-trip.");
    for (var index = 0; index < expected.Variations.Count; index++)
        RequireEquivalentTree(expected.Variations[index], actual.Variations[index]);
}

static void RequireParseFailure(string text, int expectedOffset)
{
    try
    {
        _ = SgfDocumentParser.Parse(text);
        throw new InvalidOperationException("Malformed SGF must be rejected.");
    }
    catch (SgfParseException exception)
    {
        Require(exception.Offset == expectedOffset,
            $"Malformed SGF must report offset {expectedOffset}, but reported {exception.Offset}.");
    }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
