namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Capabilities;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Strategies;

/// <summary>
/// Selects, executes, diagnoses, and safely continues initial-position setup methods.
/// </summary>
public sealed class InitialPositionConcierge
{
    private readonly TimeProvider _timeProvider;

    public InitialPositionConcierge(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<InitialPositionConciergeResult> ExecuteAsync(
        IInitialPositionExecutionHost host,
        InitialPositionRequest request,
        GtpCapabilitySet capabilities,
        IGtpEngineCompatibilityProfile? profile = null,
        InitialPositionConciergeCursor? cursor = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(capabilities);
        profile ??= GenericGtpProfile.Instance;

        var strategies = profile.Strategies;
        var startIndex = cursor?.NextStrategyIndex ?? 0;
        if (startIndex < 0 || startIndex > strategies.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(cursor), startIndex, "The concierge cursor is outside the strategy list.");
        }

        var attempts = new List<InitialPositionAttempt>();
        var diagnostics = new List<string>();
        if (cursor is { RecoveryRequired: true } &&
            !await TryRecoverAsync(host, profile, diagnostics, cancellationToken))
        {
            return new InitialPositionConciergeResult(attempts, cursor, diagnostics);
        }

        var classification = InitialPositionClassifier.Classify(request);
        for (var strategyIndex = startIndex; strategyIndex < strategies.Count; strategyIndex++)
        {
            var strategy = strategies[strategyIndex];
            var startedAt = _timeProvider.GetUtcNow();
            if (!strategy.CanApply(request, classification))
            {
                attempts.Add(CreateImmediateAttempt(
                    strategy,
                    InitialPositionAttemptStatus.NotApplicable,
                    startedAt,
                    "The method cannot represent this kind of initial position."));
                continue;
            }

            var unsupportedCommands = strategy.RequiredCommands
                .Where(command => capabilities.Get(command).Support == GtpCommandSupport.Unsupported)
                .ToArray();
            if (unsupportedCommands.Length > 0)
            {
                attempts.Add(CreateImmediateAttempt(
                    strategy,
                    InitialPositionAttemptStatus.Unsupported,
                    startedAt,
                    $"The engine reports unsupported command(s): {string.Join(", ", unsupportedCommands)}."));
                continue;
            }

            var execution = await ExecuteStrategyAsync(
                host,
                request,
                strategy,
                profile,
                startedAt,
                cancellationToken);
            attempts.Add(execution.Attempt);

            var nextIndex = strategyIndex + 1;
            var hasNext = nextIndex < strategies.Count;
            if (execution.Attempt.Status == InitialPositionAttemptStatus.CommandRejected)
            {
                if (!hasNext)
                {
                    return new InitialPositionConciergeResult(attempts, null, diagnostics);
                }

                if (!await TryRecoverAsync(host, profile, diagnostics, cancellationToken))
                {
                    return new InitialPositionConciergeResult(
                        attempts,
                        new InitialPositionConciergeCursor(nextIndex, RecoveryRequired: true),
                        diagnostics);
                }

                continue;
            }

            if (execution.Attempt.Status == InitialPositionAttemptStatus.VerifiedSuccess)
            {
                return new InitialPositionConciergeResult(attempts, null, diagnostics);
            }

            var canContinue = hasNext && execution.Attempt.Status is
                InitialPositionAttemptStatus.UnverifiedSuccess or
                InitialPositionAttemptStatus.PositionMismatch or
                InitialPositionAttemptStatus.InvalidResponse or
                InitialPositionAttemptStatus.TransportFailure;
            var continuation = canContinue
                ? new InitialPositionConciergeCursor(
                    nextIndex,
                    RecoveryRequired: execution.EngineMayBeDirty)
                : null;
            return new InitialPositionConciergeResult(attempts, continuation, diagnostics);
        }

        return new InitialPositionConciergeResult(attempts, null, diagnostics);
    }

    private async Task<StrategyExecution> ExecuteStrategyAsync(
        IInitialPositionExecutionHost host,
        InitialPositionRequest request,
        IInitialPositionStrategy strategy,
        IGtpEngineCompatibilityProfile profile,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var timestamp = _timeProvider.GetTimestamp();
        IInitialPositionDocumentLease? documentLease = null;
        try
        {
            InitialPositionStrategyContext? context = null;
            if (strategy is LoadSgfStrategy loadSgfStrategy)
            {
                documentLease = await host.MaterializeAsync(
                    loadSgfStrategy.CreateDocument(request),
                    cancellationToken);
                context = new InitialPositionStrategyContext(
                    documentLease.FilePath,
                    profile.LoadSgfPathStyle,
                    profile.LoadSgfMoveNumber);
            }

            IReadOnlyList<string> commands;
            try
            {
                commands = strategy.BuildCommands(request, context);
            }
            catch (Exception ex)
            {
                return new StrategyExecution(
                    CreateAttempt(
                        strategy,
                        InitialPositionAttemptStatus.InvalidResponse,
                        startedAt,
                        timestamp,
                        detail: $"Could not build setup commands: {DescribeException(ex)}"),
                    EngineMayBeDirty: false);
            }

            GtpCommandResult? lastResponse = null;
            var sentCommands = new List<string>();
            foreach (var command in commands)
            {
                sentCommands.Add(command);
                try
                {
                    lastResponse = await host.SendAsync(command, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new StrategyExecution(
                        CreateAttempt(
                            strategy,
                            InitialPositionAttemptStatus.TransportFailure,
                            startedAt,
                            timestamp,
                            sentCommands,
                            failedCommand: command,
                            detail: DescribeException(ex)),
                        EngineMayBeDirty: true);
                }

                if (!lastResponse.IsSuccess)
                {
                    return new StrategyExecution(
                        CreateAttempt(
                            strategy,
                            InitialPositionAttemptStatus.CommandRejected,
                            startedAt,
                            timestamp,
                            sentCommands,
                            failedCommand: command,
                            engineResponse: lastResponse.Payload,
                            detail: "The engine rejected the setup command."),
                        EngineMayBeDirty: true);
                }
            }

            return CreateSuccessfulExecution(
                strategy,
                request,
                startedAt,
                timestamp,
                sentCommands,
                lastResponse?.Payload);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new StrategyExecution(
                CreateAttempt(
                    strategy,
                    InitialPositionAttemptStatus.TransportFailure,
                    startedAt,
                    timestamp,
                    detail: $"Could not prepare the setup method: {DescribeException(ex)}"),
                EngineMayBeDirty: false);
        }
        finally
        {
            if (documentLease is not null)
            {
                await documentLease.DisposeAsync();
            }
        }
    }

    private StrategyExecution CreateSuccessfulExecution(
        IInitialPositionStrategy strategy,
        InitialPositionRequest request,
        DateTimeOffset startedAt,
        long timestamp,
        IReadOnlyList<string> commands,
        string? responsePayload)
    {
        if (strategy is FixedHandicapStrategy fixedHandicapStrategy)
        {
            var verification = fixedHandicapStrategy.VerifyResponse(request, responsePayload ?? string.Empty);
            var status = verification.Status switch
            {
                InitialPositionVerificationStatus.Verified => InitialPositionAttemptStatus.VerifiedSuccess,
                InitialPositionVerificationStatus.PositionMismatch => InitialPositionAttemptStatus.PositionMismatch,
                InitialPositionVerificationStatus.InvalidResponse => InitialPositionAttemptStatus.InvalidResponse,
                _ => InitialPositionAttemptStatus.UnverifiedSuccess,
            };
            return new StrategyExecution(
                CreateAttempt(
                    strategy,
                    status,
                    startedAt,
                    timestamp,
                    commands,
                    engineResponse: responsePayload,
                    detail: verification.Detail),
                EngineMayBeDirty: status != InitialPositionAttemptStatus.VerifiedSuccess);
        }

        if (strategy is KifuwarabeAtomicSetupStrategy atomicSetupStrategy)
        {
            var verification = atomicSetupStrategy.VerifySuccessfulResponse();
            return new StrategyExecution(
                CreateAttempt(
                    strategy,
                    InitialPositionAttemptStatus.VerifiedSuccess,
                    startedAt,
                    timestamp,
                    commands,
                    engineResponse: responsePayload,
                    detail: verification.Detail),
                EngineMayBeDirty: false);
        }

        var detail = strategy is LoadSgfStrategy loadSgfStrategy
            ? loadSgfStrategy.VerifySuccessfulResponse().Detail
            : "The engine accepted every command, but the resulting board could not be verified through portable GTP.";
        return new StrategyExecution(
            CreateAttempt(
                strategy,
                InitialPositionAttemptStatus.UnverifiedSuccess,
                startedAt,
                timestamp,
                commands,
                engineResponse: responsePayload,
                detail: detail),
            EngineMayBeDirty: true);
    }

    private async Task<bool> TryRecoverAsync(
        IInitialPositionExecutionHost host,
        IGtpEngineCompatibilityProfile profile,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            await host.RecoverAsync(profile.RecoveryAfterAttempt, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            diagnostics.Add($"Could not recover the engine before another setup method: {DescribeException(ex)}");
            return false;
        }
    }

    private InitialPositionAttempt CreateImmediateAttempt(
        IInitialPositionStrategy strategy,
        InitialPositionAttemptStatus status,
        DateTimeOffset startedAt,
        string detail) =>
        new(
            strategy.Method,
            strategy.DisplayName,
            status,
            startedAt,
            TimeSpan.Zero,
            detail: detail);

    private InitialPositionAttempt CreateAttempt(
        IInitialPositionStrategy strategy,
        InitialPositionAttemptStatus status,
        DateTimeOffset startedAt,
        long timestamp,
        IEnumerable<string>? commands = null,
        string? failedCommand = null,
        string? engineResponse = null,
        string? detail = null) =>
        new(
            strategy.Method,
            strategy.DisplayName,
            status,
            startedAt,
            _timeProvider.GetElapsedTime(timestamp),
            commands,
            failedCommand,
            engineResponse,
            detail);

    private static string DescribeException(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";

    private sealed record StrategyExecution(
        InitialPositionAttempt Attempt,
        bool EngineMayBeDirty);
}
