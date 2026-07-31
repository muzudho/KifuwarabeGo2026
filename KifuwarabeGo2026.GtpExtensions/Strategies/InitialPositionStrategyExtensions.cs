namespace KifuwarabeGo2026.GtpExtensions.Strategies;

using KifuwarabeGo2026.GtpExtensions.Capabilities;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;

/// <summary>
/// Applies capability evidence consistently to every initial-position strategy.
/// </summary>
public static class InitialPositionStrategyExtensions
{
    public static bool CanAttempt(
        this IInitialPositionStrategy strategy,
        InitialPositionRequest request,
        InitialPositionClassification classification,
        GtpCapabilitySet capabilities)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(capabilities);
        return strategy.CanApply(request, classification) &&
            strategy.RequiredCommands.All(command =>
                capabilities.Get(command).Support != GtpCommandSupport.Unsupported);
    }
}
