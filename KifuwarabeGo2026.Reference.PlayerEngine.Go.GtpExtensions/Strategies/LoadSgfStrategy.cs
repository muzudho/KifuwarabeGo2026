namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Strategies;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Sgf;

/// <summary>
/// Reproduces an initial position by loading a host-materialized SGF document.
/// </summary>
public sealed class LoadSgfStrategy : IInitialPositionStrategy
{
    public static LoadSgfStrategy Instance { get; } = new();

    private LoadSgfStrategy()
    {
    }

    public InitialPositionMethod Method => InitialPositionMethod.LoadSgf;

    public string DisplayName => "SGF読込（loadsgf）";

    public IReadOnlyList<string> RequiredCommands { get; } = ["loadsgf"];

    public bool CanApply(InitialPositionRequest request, InitialPositionClassification classification)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind != InitialPositionKind.HistorySensitivePosition;
    }

    public InitialPositionDocument CreateDocument(InitialPositionRequest request) =>
        InitialPositionSgfBuilder.Build(request);

    public InitialPositionVerificationResult VerifySuccessfulResponse() =>
        new(
            InitialPositionVerificationStatus.Unverified,
            "loadsgf succeeded, but standard GTP does not provide a portable board-state verification response.");

    public IReadOnlyList<string> BuildCommands(
        InitialPositionRequest request,
        InitialPositionStrategyContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (context is null || string.IsNullOrWhiteSpace(context.SgfDocumentPath))
        {
            throw new InvalidOperationException("loadsgf requires a host-created SGF document path.");
        }

        if (context.LoadSgfMoveNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context),
                context.LoadSgfMoveNumber,
                "A loadsgf move number cannot be negative.");
        }

        var pathArgument = GtpCommandArgument.FormatFilePath(context.SgfDocumentPath, context.FilePathStyle);
        var moveNumberArgument = context.LoadSgfMoveNumber is { } moveNumber
            ? $" {moveNumber}"
            : string.Empty;
        return [$"loadsgf {pathArgument}{moveNumberArgument}"];
    }
}
