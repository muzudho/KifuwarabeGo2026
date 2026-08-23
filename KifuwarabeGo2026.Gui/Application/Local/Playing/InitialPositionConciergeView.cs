namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using System.Collections.Generic;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Provides a read-only GUI projection of two independent engine setup flows.
/// </summary>
public sealed record InitialPositionConciergeView(
    bool IsVisible,
    bool IsBusy,
    GoStone? SelectedStone,
    IReadOnlyList<InitialPositionEngineProgressView> Engines)
{
    public static InitialPositionConciergeView Hidden { get; } =
        new(false, false, null, []);
}

public sealed record InitialPositionEngineProgressView(
    GoStone Stone,
    string EngineName,
    bool IsAccepted,
    bool IsBusy,
    bool CanTryAnotherMethod,
    bool CanContinueAsIs,
    IReadOnlyList<InitialPositionAttempt> Attempts,
    IReadOnlyList<string> Diagnostics);
