namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Sgf;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System.Globalization;

/// <summary>
/// Builds a minimal SGF root node for an initial position without performing file I/O.
/// </summary>
public static class InitialPositionSgfBuilder
{
    public static InitialPositionDocument Build(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = new SgfDocument();
        var tree = new SgfGameTree();
        var root = new SgfNode();
        root.Properties.Add(new SgfProperty("GM", ["1"]));
        root.Properties.Add(new SgfProperty("FF", ["4"]));
        root.Properties.Add(new SgfProperty("CA", ["UTF-8"]));
        root.Properties.Add(new SgfProperty("SZ", [request.BoardSize.ToString(CultureInfo.InvariantCulture)]));
        root.Properties.Add(new SgfProperty("KM", [request.Komi.ToString(CultureInfo.InvariantCulture)]));
        root.Properties.Add(new SgfProperty("PL", [request.StartingTurn == GoStone.Black ? "B" : "W"]));
        AddSetupStones(root, request, GoStone.Black, "AB");
        AddSetupStones(root, request, GoStone.White, "AW");
        tree.Sequence.Add(root);
        document.GameTrees.Add(tree);
        return new InitialPositionDocument("initial-position.sgf", SgfDocumentWriter.Write(document) + "\n");
    }

    private static void AddSetupStones(
        SgfNode root,
        InitialPositionRequest request,
        GoStone stone,
        string propertyName)
    {
        var matchingStones = request.SetupStones.Where(setupStone => setupStone.Stone == stone).ToArray();
        if (matchingStones.Length > 0)
            root.Properties.Add(new SgfProperty(
                propertyName,
                matchingStones.Select(setupStone => SgfCoordinate.FormatPoint(setupStone.Point, request.BoardSize))));
    }
}
