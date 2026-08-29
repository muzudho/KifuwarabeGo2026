using KifuwarabeGo2026.FormalAdapter.Sgf.Document;
using KifuwarabeGo2026.FormalAdapter.Sgf.Go;
using KifuwarabeGo2026.Shared.Domain;

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

var goRecord = SgfGoGameRecordConverter.Parse(
    "(;GM[1]FF[4]SZ[9]KM[6.5]TM[600]PB[Black]PW[White]AB[aa][bb]C[root]" +
    ";B[cc]BL[590]C[first]CC[{\"future\":true}];W[]WL[580](;B[dd])(;B[ee]))");
Require(goRecord.BoardSize == 9 && goRecord.Komi == 6.5m && goRecord.TimeLimit == TimeSpan.FromSeconds(600),
    "Go root settings must project from SGF.");
Require(goRecord.SetupStones.SequenceEqual([
    new SgfGoSetupStone(GoStone.Black, new GoPoint(0, 0)),
    new SgfGoSetupStone(GoStone.Black, new GoPoint(1, 1))]),
    "Go setup stones must project from SGF.");
Require(goRecord.Moves.Count == 2 && goRecord.Moves[0].Point == new GoPoint(2, 2) && goRecord.Moves[1].Point is null,
    "Moves and passes from the main sequence must project without following variations.");
Require(goRecord.Moves[0].AnalysisPropertyIdentifier == "CC" && goRecord.Moves[0].AnalysisJson == "{\"future\":true}",
    "Analysis JSON must remain opaque in the neutral Go projection.");
var goRoundTrip = SgfGoGameRecordConverter.Parse(SgfGoGameRecordConverter.Write(goRecord, "FormalAdapterSgfTest:1"));
Require(goRoundTrip.Moves.SequenceEqual(goRecord.Moves) && goRoundTrip.SetupStones.SequenceEqual(goRecord.SetupStones),
    "The neutral Go record must round-trip through the SGF document model.");
Require(SgfCoordinate.FormatPoint(new GoPoint(8, 8), 9) == "ii" &&
        SgfCoordinate.TryParsePoint("", 9, out var pass) && pass is null,
    "SGF Go coordinates and pass must use the FormalAdapter boundary.");

Console.WriteLine("PASS: SGF documents and neutral Go records retained collections, variations, properties, setup stones, moves, passes, time, and opaque analysis.");

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
