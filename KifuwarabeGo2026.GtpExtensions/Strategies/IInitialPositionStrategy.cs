namespace KifuwarabeGo2026.GtpExtensions.Strategies;

using KifuwarabeGo2026.GtpExtensions.InitialPosition;

/// <summary>
/// Describes one engine-independent way to reproduce an initial position.
/// </summary>
public interface IInitialPositionStrategy
{
    InitialPositionMethod Method { get; }

    string DisplayName { get; }

    IReadOnlyList<string> RequiredCommands { get; }

    bool CanApply(InitialPositionRequest request, InitialPositionClassification classification);

    IReadOnlyList<string> BuildCommands(
        InitialPositionRequest request,
        InitialPositionStrategyContext? context = null);
}
