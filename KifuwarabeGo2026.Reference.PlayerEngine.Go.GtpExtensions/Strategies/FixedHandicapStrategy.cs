namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Strategies;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Reproduces a conventional fixed-handicap placement and verifies returned vertices.
/// </summary>
public sealed class FixedHandicapStrategy : IInitialPositionStrategy
{
    public static FixedHandicapStrategy Instance { get; } = new();

    private FixedHandicapStrategy()
    {
    }

    public InitialPositionMethod Method => InitialPositionMethod.FixedHandicap;

    public string DisplayName => "標準置き碁（fixed_handicap）";

    public IReadOnlyList<string> RequiredCommands { get; } = ["fixed_handicap"];

    public bool CanApply(InitialPositionRequest request, InitialPositionClassification classification)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind == InitialPositionKind.StandardFixedHandicap &&
            classification.FixedHandicapStoneCount is >= 2 and <= 9 &&
            classification.WhiteStoneCount == 0 &&
            request.StartingTurn == GoStone.White;
    }

    public IReadOnlyList<string> BuildCommands(
        InitialPositionRequest request,
        InitialPositionStrategyContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var classification = InitialPositionClassifier.Classify(request);
        if (!CanApply(request, classification))
        {
            throw new InvalidOperationException("fixed_handicap cannot reproduce this initial position.");
        }

        var commands = InitialPositionCommandPreamble.Create(request);
        commands.Add($"fixed_handicap {classification.FixedHandicapStoneCount}");
        return commands;
    }

    public InitialPositionVerificationResult VerifyResponse(
        InitialPositionRequest request,
        string responsePayload)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(responsePayload);

        var expectedPoints = request.SetupStones
            .Where(setupStone => setupStone.Stone == GoStone.Black)
            .Select(setupStone => setupStone.Point)
            .ToHashSet();
        var expectedVertices = expectedPoints
            .Select(point => GtpCoordinate.FormatVertex(point, request.BoardSize))
            .OrderBy(vertex => vertex, StringComparer.Ordinal)
            .ToArray();
        var responseVertices = responsePayload.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var actualPoints = new HashSet<GoPoint>();
        var actualVertices = new List<string>();

        foreach (var vertex in responseVertices)
        {
            if (!GtpCoordinate.TryParseVertex(vertex, request.BoardSize, out var point))
            {
                return new InitialPositionVerificationResult(
                    InitialPositionVerificationStatus.InvalidResponse,
                    $"fixed_handicap returned invalid vertex '{vertex}'.",
                    expectedVertices,
                    actualVertices.Append(vertex));
            }

            if (!actualPoints.Add(point))
            {
                actualVertices.Add(GtpCoordinate.FormatVertex(point, request.BoardSize));
                return new InitialPositionVerificationResult(
                    InitialPositionVerificationStatus.InvalidResponse,
                    $"fixed_handicap returned duplicate vertex '{vertex}'.",
                    expectedVertices,
                    actualVertices);
            }

            actualVertices.Add(GtpCoordinate.FormatVertex(point, request.BoardSize));
        }

        actualVertices.Sort(StringComparer.Ordinal);
        if (!actualPoints.SetEquals(expectedPoints))
        {
            return new InitialPositionVerificationResult(
                InitialPositionVerificationStatus.PositionMismatch,
                $"fixed_handicap returned {actualPoints.Count} stones, but {expectedPoints.Count} expected stones were requested.",
                expectedVertices,
                actualVertices);
        }

        return new InitialPositionVerificationResult(
            InitialPositionVerificationStatus.Verified,
            "The fixed_handicap response matches the requested setup.",
            expectedVertices,
            actualVertices);
    }
}
