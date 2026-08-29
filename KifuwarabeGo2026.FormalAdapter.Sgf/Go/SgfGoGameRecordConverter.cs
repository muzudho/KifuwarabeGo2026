namespace KifuwarabeGo2026.FormalAdapter.Sgf.Go;

using System.Globalization;
using KifuwarabeGo2026.FormalAdapter.Sgf.Document;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Projects the main sequence of the first SGF game tree to and from a neutral Go record.</summary>
public static class SgfGoGameRecordConverter
{
    public static SgfGoGameRecord FromDocument(SgfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.GameTrees.Count == 0 || document.GameTrees[0].Sequence.Count == 0)
            throw new SgfGoConversionException("SGF game tree has no nodes.");

        var nodes = document.GameTrees[0].Sequence;
        var record = new SgfGoGameRecord();
        ApplyRoot(record, nodes[0]);
        var sawMove = false;
        foreach (var node in nodes)
        {
            var blackSetup = Find(node, "AB");
            var whiteSetup = Find(node, "AW");
            if (blackSetup is not null || whiteSetup is not null)
            {
                if (sawMove) throw new SgfGoConversionException("SGF setup stones after moves are not supported by the Go record projection.");
                AddSetup(record, blackSetup, GoStone.Black, "AB");
                AddSetup(record, whiteSetup, GoStone.White, "AW");
            }

            sawMove |= AddMove(record, node, "B", GoStone.Black);
            sawMove |= AddMove(record, node, "W", GoStone.White);
        }
        return record;
    }

    public static SgfDocument ToDocument(SgfGoGameRecord record, string? application = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        var document = new SgfDocument();
        var tree = new SgfGameTree();
        var root = new SgfNode();
        Add(root, "GM", "1");
        Add(root, "FF", "4");
        Add(root, "CA", "UTF-8");
        if (!string.IsNullOrWhiteSpace(application)) Add(root, "AP", application);
        AddIf(root, "RU", record.RuleName);
        Add(root, "SZ", record.BoardSize.ToString(CultureInfo.InvariantCulture));
        Add(root, "KM", record.Komi.ToString(CultureInfo.InvariantCulture));
        if (record.TimeLimit > TimeSpan.Zero)
            Add(root, "TM", record.TimeLimit.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        AddIf(root, "GN", record.GameName);
        AddIf(root, "PB", record.BlackPlayerName);
        AddIf(root, "PW", record.WhitePlayerName);
        AddIf(root, "BR", record.BlackRank);
        AddIf(root, "WR", record.WhiteRank);
        AddIf(root, "DT", record.PlayedDate);
        AddIf(root, "PC", record.Place);
        AddIf(root, "RE", record.Result);
        AddSetup(root, record, GoStone.Black, "AB");
        AddSetup(root, record, GoStone.White, "AW");
        AddIf(root, "C", record.RootComment);
        tree.Sequence.Add(root);

        foreach (var move in record.Moves)
        {
            if (move.Stone is not (GoStone.Black or GoStone.White))
                throw new ArgumentOutOfRangeException(nameof(record), move.Stone, "Move stone must be black or white.");
            var node = new SgfNode();
            var moveIdentifier = move.Stone == GoStone.Black ? "B" : "W";
            Add(node, moveIdentifier, SgfCoordinate.FormatPoint(move.Point, record.BoardSize));
            if (move.TimeLeftAfterMove is { } timeLeft)
                Add(node, move.Stone == GoStone.Black ? "BL" : "WL",
                    Math.Max(0, timeLeft.TotalSeconds).ToString("0.###", CultureInfo.InvariantCulture));
            AddIf(node, "C", move.Comment);
            if (move.AnalysisJson is not null)
            {
                if (move.AnalysisPropertyIdentifier is not ("CC" or "KFW" or "KFA"))
                    throw new ArgumentException("Analysis property must be CC, KFW, or KFA.", nameof(record));
                Add(node, move.AnalysisPropertyIdentifier, move.AnalysisJson);
            }
            tree.Sequence.Add(node);
        }

        document.GameTrees.Add(tree);
        return document;
    }

    public static SgfGoGameRecord Parse(string sgf) => FromDocument(SgfDocumentParser.Parse(sgf));
    public static string Write(SgfGoGameRecord record, string? application = null) =>
        SgfDocumentWriter.Write(ToDocument(record, application));

    private static void ApplyRoot(SgfGoGameRecord record, SgfNode root)
    {
        if (Value(root, "GM") is { } gameKind && gameKind != "1")
            throw new SgfGoConversionException($"Unsupported SGF game kind GM[{gameKind}].");
        if (Value(root, "SZ") is { } sizeText)
        {
            if (!int.TryParse(sizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
                throw new SgfGoConversionException($"Invalid SGF board size SZ[{sizeText}].");
            record.BoardSize = size;
        }
        if (Value(root, "KM") is { } komiText)
        {
            if (!decimal.TryParse(komiText, NumberStyles.Number, CultureInfo.InvariantCulture, out var komi))
                throw new SgfGoConversionException($"Invalid SGF komi KM[{komiText}].");
            record.Komi = komi;
        }
        if (Value(root, "TM") is { } timeText)
            record.TimeLimit = ParseSeconds(timeText, "TM");
        record.RuleName = Value(root, "RU") ?? "";
        record.GameName = Value(root, "GN") ?? "";
        record.BlackPlayerName = Value(root, "PB") ?? "";
        record.WhitePlayerName = Value(root, "PW") ?? "";
        record.BlackRank = Value(root, "BR") ?? "";
        record.WhiteRank = Value(root, "WR") ?? "";
        record.PlayedDate = Value(root, "DT") ?? "";
        record.Place = Value(root, "PC") ?? "";
        record.Result = Value(root, "RE") ?? "";
        record.RootComment = Value(root, "C") ?? "";
    }

    private static void AddSetup(SgfGoGameRecord record, SgfProperty? property, GoStone stone, string identifier)
    {
        if (property is null) return;
        foreach (var value in property.Values)
        {
            if (!SgfCoordinate.TryParsePoint(value, record.BoardSize, out var point) || point is null)
                throw new SgfGoConversionException($"Invalid SGF setup point {identifier}[{value}].");
            record.SetupStones.Add(new SgfGoSetupStone(stone, point.Value));
        }
    }

    private static bool AddMove(SgfGoGameRecord record, SgfNode node, string identifier, GoStone stone)
    {
        var property = Find(node, identifier);
        if (property is null) return false;
        if (property.Values.Count != 1)
            throw new SgfGoConversionException($"SGF move property {identifier} must have one value.");
        if (!SgfCoordinate.TryParsePoint(property.Values[0], record.BoardSize, out var point))
            throw new SgfGoConversionException($"Invalid SGF move point {identifier}[{property.Values[0]}].");
        TimeSpan? timeLeft = null;
        var timeIdentifier = stone == GoStone.Black ? "BL" : "WL";
        if (Value(node, timeIdentifier) is { } timeText) timeLeft = ParseSeconds(timeText, timeIdentifier);
        var analysis = Find(node, "CC") ?? Find(node, "KFW") ?? Find(node, "KFA");
        record.Moves.Add(new SgfGoMove(
            stone,
            point,
            Value(node, "C") ?? "",
            timeLeft,
            analysis?.Identifier,
            analysis?.Values.FirstOrDefault()));
        return true;
    }

    private static TimeSpan ParseSeconds(string text, string identifier)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            !double.IsFinite(seconds) || seconds < 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
            throw new SgfGoConversionException($"Invalid SGF time {identifier}[{text}].");
        return TimeSpan.FromSeconds(seconds);
    }

    private static SgfProperty? Find(SgfNode node, string identifier) =>
        node.Properties.FirstOrDefault(property => property.Identifier == identifier);
    private static string? Value(SgfNode node, string identifier) => Find(node, identifier)?.Values.FirstOrDefault();
    private static void Add(SgfNode node, string identifier, string value) =>
        node.Properties.Add(new SgfProperty(identifier, [value]));
    private static void AddIf(SgfNode node, string identifier, string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Add(node, identifier, value);
    }
    private static void AddSetup(SgfNode root, SgfGoGameRecord record, GoStone stone, string identifier)
    {
        var values = record.SetupStones.Where(item => item.Stone == stone)
            .Select(item => SgfCoordinate.FormatPoint(item.Point, record.BoardSize)).ToArray();
        if (values.Length > 0) root.Properties.Add(new SgfProperty(identifier, values));
    }
}
