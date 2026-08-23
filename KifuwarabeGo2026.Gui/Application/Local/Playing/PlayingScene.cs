namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.GtpExtensions.Capabilities;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.GameOasis;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using InitialPositionConciergePage = KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge.InitialPositionConcierge;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// ［対局中画面］の処理
/// </summary>
public sealed class PlayingScene : IDisposable
{
    private readonly GoAppSession _session;
    private readonly Action<float, float, float> _playPlaceStoneSound;
    private readonly Action _saveGtpEngineProfiles;
    private readonly Action _openGtpLog;
    private readonly Dictionary<GoStone, GtpEngineClient> _gtpEngines = new();
    private readonly Dictionary<GoStone, EngineInitialPositionState> _initialPositionStates = new();
    private readonly HashSet<GoStone> _analysisEngines = new();
    private readonly Queue<Func<CancellationToken, Task<EngineCommandResult>>> _engineCommandQueue = new();
    private CancellationTokenSource _engineCancellation = new();
    private Task<EngineCommandCompletion>? _pendingEngineCommand;
    private int _engineCommandGeneration;
    private bool _computerMoveAwaitingDraw;
    private bool _isInitialPositionConciergeActive;
    private GoStone? _selectedInitialPositionEngine;
    private GameOasisPlayerParticipationBridge? _gameOasisPlayerBridge;

    public PlayingScene(
        GoAppSession session,
        Action<float, float, float> playPlaceStoneSound,
        Action saveGtpEngineProfiles,
        Action openGtpLog)
    {
        _session = session;
        _playPlaceStoneSound = playPlaceStoneSound;
        _saveGtpEngineProfiles = saveGtpEngineProfiles;
        _openGtpLog = openGtpLog;
    }

    public bool IsInitialPositionConciergeVisible =>
        _isInitialPositionConciergeActive &&
        _session.CurrentMode.Kind == GoAppModeKind.Playing;

    public InitialPositionConciergeView InitialPositionConciergeView
    {
        get
        {
            if (!IsInitialPositionConciergeVisible)
            {
                return InitialPositionConciergeView.Hidden;
            }

            var isBusy = _pendingEngineCommand is not null;
            var engines = _initialPositionStates
                .OrderBy(pair => pair.Key == GoStone.Black ? 0 : 1)
                .Select(pair => pair.Value.CreateView(isBusy))
                .ToArray();
            return new InitialPositionConciergeView(
                true,
                isBusy,
                _selectedInitialPositionEngine,
                engines);
        }
    }

    public void Update()
    {
        _gameOasisPlayerBridge?.Update();
        CompletePendingEngineCommand();
        RequestComputerMoveIfReady();
    }

    /// <summary>
    /// GUI起動後に完成するGame Oasisのプレイヤー参加経路を後付けします。
    /// 接続しただけでは現行ローカル対局の進行経路を変更しません。
    /// </summary>
    public void AttachGameOasisPlayerBridge(GameOasisPlayerParticipationBridge bridge)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        if (_gameOasisPlayerBridge is not null && !ReferenceEquals(_gameOasisPlayerBridge, bridge))
            throw new InvalidOperationException("A different Game Oasis player bridge is already attached.");
        _gameOasisPlayerBridge = bridge;
    }

    /// <summary>
    /// 直前のコンピューター着手を盤面へ描画したことを通知します。
    /// 描画が間引かれる状況でも、次の着手へ進む前に局面を最低 1 フレーム表示します。
    /// </summary>
    public void MarkFrameDrawn() => _computerMoveAwaitingDraw = false;

    public void StartPlaying()
    {
        _computerMoveAwaitingDraw = false;
        _session.StartPlaying();
        StartGtpGameIfNeeded();
    }

    public bool TryHandleMouseClick(Point point)
    {
        if (IsInitialPositionConciergeVisible)
        {
            if (InitialPositionConciergePage.GetTryAnotherButtonHit(point) is { } tryStone)
            {
                SelectInitialPositionEngine(tryStone);
                TryAnotherInitialPositionMethod(tryStone);
            }
            else if (InitialPositionConciergePage.GetContinueButtonHit(point) is { } continueStone)
            {
                SelectInitialPositionEngine(continueStone);
                ContinueWithInitialPositionMethod(continueStone);
            }
            else if (InitialPositionConciergePage.IsLogButtonHit(point))
            {
                _openGtpLog();
            }
            else if (InitialPositionConciergePage.IsCancelButtonHit(point))
            {
                CancelGtpGame();
                _session.DeactivateModalWindow(ActiveWindowId.InitialPositionConcierge);
                _session.CancelPlaying();
            }
            else if (InitialPositionConciergePage.GetEngineCardHit(point) is { } selectedStone)
            {
                SelectInitialPositionEngine(selectedStone);
            }

            return true;
        }

        var rightSidePanel = LocalMatchPlayPage.Default.RightSidePanel;
        if (ShouldShowEnginePreparing() && rightSidePanel.CancelButton.IsHit(point))
        {
            CancelGtpGame();
            _session.CancelPlaying();
            return true;
        }

        if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        if (!_session.CanAcceptHumanMove)
        {
            // Engine turns and engine setup are handled from Update().
            return true;
        }

        if (rightSidePanel.PassButton.IsHit(point))
        {
            var passedBy = _session.CurrentTurn;
            if (_session.Pass())
            {
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
                SyncHumanPassIfNeeded(passedBy);
            }

            return true;
        }

        if (rightSidePanel.ResignButton.IsHit(point))
        {
            if (_session.Resign())
            {
                PlayPlaceStoneSound(0.45f, -0.25f, 0f);
                StopGtpGame();
            }

            return true;
        }

        if (BoardRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
        {
            var placedBy = _session.CurrentTurn;
            if (_session.TryPlaceStone(intersection.X, intersection.Y))
            {
                PlayPlaceStoneSound();
                SyncHumanMoveIfNeeded(placedBy, new GoPoint(intersection.X, intersection.Y));
            }

            return true;
        }

        return false;
    }

    public void SelectPreviousInitialPositionEngine() => SelectAdjacentInitialPositionEngine(-1);

    public void SelectNextInitialPositionEngine() => SelectAdjacentInitialPositionEngine(1);

    public void TryAnotherSelectedInitialPositionMethod()
    {
        if (_selectedInitialPositionEngine is { } stone)
        {
            TryAnotherInitialPositionMethod(stone);
        }
    }

    public void ContinueSelectedInitialPositionMethod()
    {
        if (_selectedInitialPositionEngine is { } stone)
        {
            ContinueWithInitialPositionMethod(stone);
        }
    }

    public void CancelInitialPositionConcierge()
    {
        if (!IsInitialPositionConciergeVisible)
        {
            return;
        }

        CancelGtpGame();
        _session.DeactivateModalWindow(ActiveWindowId.InitialPositionConcierge);
        _session.CancelPlaying();
    }

    public void OpenInitialPositionLog()
    {
        if (IsInitialPositionConciergeVisible)
        {
            _openGtpLog();
        }
    }

    public void Dispose()
    {
        _engineCancellation.Cancel();
        foreach (var engine in _gtpEngines.Values)
        {
            engine.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _gtpEngines.Clear();
        _engineCancellation.Dispose();
    }

    private bool ShouldShowEnginePreparing() =>
        _session.CurrentMode.Kind == GoAppModeKind.Playing &&
        !_session.CanAcceptHumanMove;

    private void StartGtpGameIfNeeded()
    {
        if (!HasComputerPlayer())
        {
            _session.SetEngineReady(true);
            return;
        }

        _session.SetEngineReady(false);
        EnsureGtpEngineForComputerPlayer(GoStone.Black);
        EnsureGtpEngineForComputerPlayer(GoStone.White);

        var enginesToStart = GetEngineSnapshot();
        if (_session.ConsumeQueuedGtpEngineButtonsForComputerPlayers())
            _saveGtpEngineProfiles();

        var matchSnapshot = _session.CurrentMatchSnapshot ??
            throw new InvalidOperationException("A local GTP game requires a Match snapshot.");
        if (matchSnapshot.SetupStones.Count > 0)
        {
            StartInitialPositionConcierge(enginesToStart, matchSnapshot);
            return;
        }

        BeginEngineCommand(async cancellationToken =>
        {
            foreach (var engine in enginesToStart)
            {
                try
                {
                    await engine.Client.StartAsync(cancellationToken);
                    await StartCasualPlayerAppIfNeededAsync(engine.Client, cancellationToken);
                    var knownAnalyze = await engine.Client.SendCommandAsync("known_command cgos-genmove_analyze", cancellationToken);
                    if (knownAnalyze.IsSuccess && knownAnalyze.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        _analysisEngines.Add(engine.Stone);
                    }
                    foreach (var command in GtpInitialPositionCommandBuilder.Build(matchSnapshot, _session.Komi))
                    {
                        await engine.Client.SendCommandExpectSuccessAsync(command, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    throw CreateEngineCommandException(engine.Stone, ex);
                }
            }

            return EngineCommandResult.EngineReady();
        });
    }

    private void StartInitialPositionConcierge(
        IReadOnlyList<EngineEntry> engines,
        KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch.MatchSnapshot matchSnapshot)
    {
        _isInitialPositionConciergeActive = true;
        _session.ActivateModalWindow(ActiveWindowId.InitialPositionConcierge);
        _initialPositionStates.Clear();
        foreach (var engine in engines)
        {
            _initialPositionStates[engine.Stone] = new EngineInitialPositionState(
                engine.Stone,
                _session.GetGtpEngineProfile(engine.Stone).DisplayName);
        }

        _selectedInitialPositionEngine = engines.FirstOrDefault()?.Stone;
        var request = InitialPositionRequest.FromSnapshot(matchSnapshot, _session.Komi);
        BeginEngineCommand(async cancellationToken =>
        {
            var updates = new List<EngineInitialPositionUpdate>();
            foreach (var engine in engines)
            {
                try
                {
                    await engine.Client.StartAsync(cancellationToken);
                    await StartCasualPlayerAppIfNeededAsync(engine.Client, cancellationToken);
                    var commandSession = new GtpEngineClientCommandSession(engine.Client);
                    var capabilities = await new GtpCapabilityProbe()
                        .ProbeInitialPositionAsync(commandSession, cancellationToken);
                    var compatibilityProfile = ResolveInitialPositionProfile(
                        _session.GetGtpEngineProfile(engine.Stone),
                        capabilities);
                    var host = new GtpInitialPositionExecutionHost(engine.Client);
                    var result = await new InitialPositionConcierge().ExecuteAsync(
                        host,
                        request,
                        capabilities,
                        compatibilityProfile,
                        cancellationToken: cancellationToken);
                    updates.Add(new EngineInitialPositionUpdate(
                        engine.Stone,
                        capabilities,
                        compatibilityProfile,
                        result));
                }
                catch (Exception ex)
                {
                    throw CreateEngineCommandException(engine.Stone, ex);
                }
            }

            return EngineCommandResult.InitialPositionProgress(updates);
        });
    }

    private async Task StartCasualPlayerAppIfNeededAsync(GtpEngineClient client, CancellationToken cancellationToken)
    {
        if (_session.UseKind != GoAppUseKind.LocalApps)
            return;

        var startSupported = await client.SendCommandAsync("known_command kfw-start-app", cancellationToken);
        if (!startSupported.IsSuccess || !startSupported.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
            return;

        const string command = "kfw-start-app ponnuki player";
        var response = await client.SendCommandAsync(command, cancellationToken);
        response.ThrowIfError(command);
    }

    private void SelectInitialPositionEngine(GoStone stone)
    {
        if (_initialPositionStates.ContainsKey(stone))
        {
            _selectedInitialPositionEngine = stone;
        }
    }

    private void SelectAdjacentInitialPositionEngine(int offset)
    {
        if (!IsInitialPositionConciergeVisible || _initialPositionStates.Count == 0)
        {
            return;
        }

        var stones = _initialPositionStates.Keys
            .OrderBy(stone => stone == GoStone.Black ? 0 : 1)
            .ToArray();
        var currentIndex = _selectedInitialPositionEngine is { } selected
            ? Array.IndexOf(stones, selected)
            : 0;
        var nextIndex = (currentIndex + offset + stones.Length) % stones.Length;
        _selectedInitialPositionEngine = stones[nextIndex];
    }

    private void TryAnotherInitialPositionMethod(GoStone stone)
    {
        if (!IsInitialPositionConciergeVisible ||
            _pendingEngineCommand is not null ||
            !_initialPositionStates.TryGetValue(stone, out var state) ||
            state.Capabilities is null ||
            state.CompatibilityProfile is null ||
            state.Cursor is null ||
            GetEngine(stone) is not { } engine ||
            _session.CurrentMatchSnapshot is not { } snapshot)
        {
            return;
        }

        state.IsBusy = true;
        var capabilities = state.Capabilities;
        var compatibilityProfile = state.CompatibilityProfile;
        var cursor = state.Cursor;
        var request = InitialPositionRequest.FromSnapshot(snapshot, _session.Komi);
        BeginEngineCommand(async cancellationToken =>
        {
            try
            {
                var result = await new InitialPositionConcierge().ExecuteAsync(
                    new GtpInitialPositionExecutionHost(engine),
                    request,
                    capabilities,
                    compatibilityProfile,
                    cursor,
                    cancellationToken);
                return EngineCommandResult.InitialPositionProgress(
                    [new EngineInitialPositionUpdate(stone, capabilities, compatibilityProfile, result)]);
            }
            catch (Exception ex)
            {
                throw CreateEngineCommandException(stone, ex);
            }
        });
    }

    private void ContinueWithInitialPositionMethod(GoStone stone)
    {
        if (_pendingEngineCommand is not null ||
            !_initialPositionStates.TryGetValue(stone, out var state) ||
            state.LastAttempt?.Status != InitialPositionAttemptStatus.UnverifiedSuccess)
        {
            return;
        }

        state.IsAccepted = true;
        state.IsBusy = false;
        RememberSuccessfulInitialPositionMethod(stone, state);
        SelectAdjacentInitialPositionEngine(1);
        TryFinalizeInitialPositionSetup();
    }

    private void ApplyInitialPositionUpdates(IReadOnlyList<EngineInitialPositionUpdate> updates)
    {
        foreach (var update in updates)
        {
            if (_initialPositionStates.TryGetValue(update.Stone, out var state))
            {
                var persistedProfile = _session.GetGtpEngineProfile(update.Stone);
                var shouldSave = persistedProfile.ClearStaleInitialPositionDetection(
                    update.Capabilities.EngineName,
                    update.Capabilities.EngineVersion);
                state.Apply(update);
                if (update.Result.IsVerified)
                {
                    shouldSave |= RememberSuccessfulInitialPositionMethod(update.Stone, state, saveNow: false);
                }

                if (shouldSave)
                {
                    _saveGtpEngineProfiles();
                }
            }
        }

        var next = _initialPositionStates.Values
            .OrderBy(state => state.Stone == GoStone.Black ? 0 : 1)
            .FirstOrDefault(state => !state.IsAccepted);
        _selectedInitialPositionEngine = next?.Stone ?? _selectedInitialPositionEngine;
    }

    private static IGtpEngineCompatibilityProfile ResolveInitialPositionProfile(
        GtpEngineProfile persistedProfile,
        GtpCapabilitySet capabilities)
    {
        var baseProfile = BuiltInGtpProfiles.ResolveBase(
            capabilities.EngineName,
            persistedProfile.InitialPositionProfileId);
        var preferredMethod = persistedProfile.InitialPositionManualPreferredMethod;
        if (preferredMethod is null &&
            persistedProfile.HasMatchingInitialPositionDetection(
                capabilities.EngineName,
                capabilities.EngineVersion) &&
            string.Equals(
                persistedProfile.InitialPositionDetectedProfileId,
                baseProfile.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            preferredMethod = persistedProfile.InitialPositionDetectedMethod;
        }

        return BuiltInGtpProfiles.Resolve(
            capabilities,
            baseProfile.Id,
            preferredMethod);
    }

    private bool RememberSuccessfulInitialPositionMethod(
        GoStone stone,
        EngineInitialPositionState state,
        bool saveNow = true)
    {
        if (state.LastAttempt is not { } attempt ||
            state.Capabilities is null ||
            state.CompatibilityProfile is null)
        {
            return false;
        }

        var persistedProfile = _session.GetGtpEngineProfile(stone);
        var changed = persistedProfile.InitialPositionDetectedMethod != attempt.Method ||
            !persistedProfile.HasMatchingInitialPositionDetection(
                state.Capabilities.EngineName,
                state.Capabilities.EngineVersion) ||
            !string.Equals(
                persistedProfile.InitialPositionDetectedProfileId,
                state.CompatibilityProfile.Id,
                StringComparison.OrdinalIgnoreCase);
        if (!changed)
        {
            return false;
        }

        persistedProfile.RememberInitialPositionDetection(
            attempt.Method,
            state.Capabilities.EngineName,
            state.Capabilities.EngineVersion,
            state.CompatibilityProfile.Id);
        if (saveNow)
        {
            _saveGtpEngineProfiles();
        }

        return true;
    }

    private void TryFinalizeInitialPositionSetup()
    {
        if (!IsInitialPositionConciergeVisible ||
            _pendingEngineCommand is not null ||
            _initialPositionStates.Count == 0 ||
            _initialPositionStates.Values.Any(state => !state.IsAccepted))
        {
            return;
        }

        var engines = GetEngineSnapshot();
        BeginEngineCommand(async cancellationToken =>
        {
            foreach (var engine in engines)
            {
                try
                {
                    var knownAnalyze = await engine.Client.SendCommandAsync(
                        "known_command cgos-genmove_analyze",
                        cancellationToken);
                    if (knownAnalyze.IsSuccess &&
                        knownAnalyze.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        _analysisEngines.Add(engine.Stone);
                    }
                }
                catch (Exception ex)
                {
                    throw CreateEngineCommandException(engine.Stone, ex);
                }
            }

            return EngineCommandResult.EngineReady();
        });
    }

    private void SyncHumanMoveIfNeeded(GoStone stone, GoPoint point)
    {
        var enginesToSync = GetEngineSnapshot();
        if (enginesToSync.Count == 0)
        {
            return;
        }

        var color = FormatColor(stone);
        var vertex = GtpCoordinate.FormatVertex(point, _session.BoardSize);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, vertex, cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void SyncHumanPassIfNeeded(GoStone stone)
    {
        var enginesToSync = GetEngineSnapshot();
        if (enginesToSync.Count == 0)
        {
            return;
        }

        var color = FormatColor(stone);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, "pass", cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void RequestComputerMoveIfReady()
    {
        var currentTurn = _session.CurrentTurn;
        if (_pendingEngineCommand is not null ||
            _computerMoveAwaitingDraw ||
            _session.CurrentMode.Kind != GoAppModeKind.Playing ||
            !_session.IsEngineReady ||
            _session.IsEngineThinking ||
            !string.IsNullOrWhiteSpace(_session.EngineErrorMessage) ||
            _session.GetPlayerKind(currentTurn) != GoPlayerKind.Computer)
        {
            return;
        }

        var engine = GetEngine(currentTurn);
        if (engine is null)
        {
            SetEngineError($"{FormatColor(currentTurn)} GTP engine is not ready.", currentTurn);
            return;
        }

        var color = FormatColor(currentTurn);
        BeginEngineCommand(async cancellationToken =>
        {
            if (_analysisEngines.Contains(currentTurn))
            {
                var analyzeResponse = await engine.SendCommandAsync($"cgos-genmove_analyze {color}", cancellationToken);
                analyzeResponse.ThrowIfError($"cgos-genmove_analyze {color}");
                return ParseAnalyzedMoveResponse(analyzeResponse.Payload, currentTurn);
            }

            var moveResponse = await engine.SendCommandAsync($"genmove {color}", cancellationToken);
            moveResponse.ThrowIfError($"genmove {color}");
            return EngineCommandResult.EngineMove(moveResponse.Payload, currentTurn);
        });
    }

    private void BeginEngineCommand(Func<CancellationToken, Task<EngineCommandResult>> command)
    {
        if (_pendingEngineCommand is not null)
        {
            _engineCommandQueue.Enqueue(command);
            return;
        }

        StartEngineCommand(command);
    }

    private void StartEngineCommand(Func<CancellationToken, Task<EngineCommandResult>> command)
    {
        _session.ClearEngineError();
        _session.SetEngineThinking(true);
        var generation = _engineCommandGeneration;
        var cancellationToken = _engineCancellation.Token;
        _pendingEngineCommand = Task.Run(async () =>
        {
            try
            {
                return new EngineCommandCompletion(await command(cancellationToken), generation);
            }
            catch (Exception ex)
            {
                var errorStone = ex is EngineCommandException engineException
                    ? engineException.Stone
                    : _session.CurrentTurn;
                return new EngineCommandCompletion(EngineCommandResult.Failure(ex, errorStone), generation);
            }
        });
    }

    private void CompletePendingEngineCommand()
    {
        if (_pendingEngineCommand is not { IsCompleted: true } completedCommand)
        {
            return;
        }

        _pendingEngineCommand = null;
        var completion = completedCommand.GetAwaiter().GetResult();
        if (completion.Generation != _engineCommandGeneration)
        {
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        var result = completion.Result;
        _session.SetEngineThinking(false);
        if (result.Error is not null)
        {
            if (_isInitialPositionConciergeActive &&
                result.ErrorStone is { } errorStone &&
                _initialPositionStates.TryGetValue(errorStone, out var state))
            {
                state.AddFailure(result.Error.Message);
                _selectedInitialPositionEngine = errorStone;
            }

            SetEngineError(result.Error.Message, result.ErrorStone ?? _session.CurrentTurn, result.Error);
            return;
        }

        if (result.InitialPositionUpdates is not null)
        {
            ApplyInitialPositionUpdates(result.InitialPositionUpdates);
            TryFinalizeInitialPositionSetup();
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (result.MakesEngineReady)
        {
            _isInitialPositionConciergeActive = false;
            _session.DeactivateModalWindow(ActiveWindowId.InitialPositionConcierge);
            _initialPositionStates.Clear();
            _selectedInitialPositionEngine = null;
            _session.SetEngineReady(true);
        }

        if (result.MoveText is null)
        {
            if (result.ClosesEngine)
            {
                StopGtpGame();
                return;
            }

            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (GtpCoordinate.IsPass(result.MoveText))
        {
            var forcedPassComment = result.PlayedBy is null ? "" : _session.GetOwnEyeForcedPassComment();
            var comment = string.IsNullOrWhiteSpace(result.Comment) ? forcedPassComment : result.Comment;
            if (_session.Pass(comment, result.Analysis, result.CommonAnalysisJson))
            {
                _computerMoveAwaitingDraw = true;
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
            }

            SyncComputerMoveToOtherEnginesIfNeeded(result.PlayedBy, "pass");
            StartQueuedEngineCommandIfNeeded();
            return;
        }

        if (!GtpCoordinate.TryParseVertex(result.MoveText, _session.BoardSize, out var point))
        {
            SetEngineError($"Invalid GTP vertex: {result.MoveText}", result.PlayedBy ?? _session.CurrentTurn);
            return;
        }

        if (!_session.TryPlaceStone(
                point.X,
                point.Y,
                result.Analysis,
                result.Comment,
                result.CommonAnalysisJson))
        {
            SetEngineError($"Illegal GTP move: {result.MoveText}", result.PlayedBy ?? _session.CurrentTurn);
            return;
        }

        PlayPlaceStoneSound();
        _computerMoveAwaitingDraw = true;
        SyncComputerMoveToOtherEnginesIfNeeded(result.PlayedBy, GtpCoordinate.FormatVertex(point, _session.BoardSize));
        StartQueuedEngineCommandIfNeeded();
    }

    private void PlayPlaceStoneSound(float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        _playPlaceStoneSound(volume, pitch, pan);
    }

    private void SyncComputerMoveToOtherEnginesIfNeeded(GoStone? playedBy, string vertex)
    {
        if (playedBy is null)
        {
            StopGtpGameIfGameOver();
            return;
        }

        var enginesToSync = GetEngineSnapshotExcept(playedBy.Value);
        if (enginesToSync.Count == 0)
        {
            StopGtpGameIfGameOver();
            return;
        }

        var color = FormatColor(playedBy.Value);
        var closeEngineAfterSync = _session.CurrentMode.Kind == GoAppModeKind.GameOver;
        BeginEngineCommand(async cancellationToken =>
        {
            await SyncPlayToEnginesAsync(enginesToSync, color, vertex, cancellationToken);
            return EngineCommandResult.Success(closeEngineAfterSync);
        });
    }

    private void StartQueuedEngineCommandIfNeeded()
    {
        if (_pendingEngineCommand is null && _engineCommandQueue.Count > 0)
        {
            StartEngineCommand(_engineCommandQueue.Dequeue());
        }
    }

    private bool HasComputerPlayer() =>
        _session.BlackPlayerKind == GoPlayerKind.Computer || _session.WhitePlayerKind == GoPlayerKind.Computer;

    private void EnsureGtpEngineForComputerPlayer(GoStone stone)
    {
        if (_session.GetPlayerKind(stone) != GoPlayerKind.Computer || _gtpEngines.ContainsKey(stone))
        {
            return;
        }

        _gtpEngines[stone] = new GtpEngineClient(CreateEngineSettings(stone), TimeSpan.FromSeconds(10));
    }

    private GtpEngineClient? GetEngine(GoStone stone) =>
        _gtpEngines.TryGetValue(stone, out var engine) ? engine : null;

    private List<EngineEntry> GetEngineSnapshot() =>
        _gtpEngines.Select(pair => new EngineEntry(pair.Key, pair.Value)).ToList();

    private List<EngineEntry> GetEngineSnapshotExcept(GoStone stone) =>
        _gtpEngines
            .Where(pair => pair.Key != stone)
            .Select(pair => new EngineEntry(pair.Key, pair.Value))
            .ToList();

    private static async Task SyncPlayToEnginesAsync(
        IReadOnlyList<EngineEntry> engines,
        string color,
        string vertex,
        CancellationToken cancellationToken)
    {
        foreach (var engine in engines)
        {
            try
            {
                await engine.Client.SendCommandExpectSuccessAsync($"play {color} {vertex}", cancellationToken);
            }
            catch (Exception ex)
            {
                throw new EngineCommandException(engine.Stone, ex.Message, ex);
            }
        }
    }

    private void CancelGtpGame()
    {
        StopGtpGame();
    }

    private void StopGtpGameIfGameOver()
    {
        if (_session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            StopGtpGame();
        }
    }

    private void StopGtpGame()
    {
        _computerMoveAwaitingDraw = false;
        _engineCommandGeneration++;
        _engineCommandQueue.Clear();
        _pendingEngineCommand = null;
        _engineCancellation.Cancel();
        _engineCancellation.Dispose();
        _engineCancellation = new CancellationTokenSource();

        var engines = GetEngineSnapshot();
        _gtpEngines.Clear();
        _analysisEngines.Clear();
        _initialPositionStates.Clear();
        _isInitialPositionConciergeActive = false;
        _session.DeactivateModalWindow(ActiveWindowId.InitialPositionConcierge);
        _selectedInitialPositionEngine = null;
        foreach (var engine in engines)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await engine.Client.DisposeAsync();
                }
                catch
                {
                    // Cancellation should return the GUI to setup even if the engine process is already gone.
                }
            });
        }
    }

    private static string FormatColor(GoStone stone) => stone == GoStone.Black ? "black" : "white";

    private static EngineCommandResult ParseAnalyzedMoveResponse(string payload, GoStone playedBy)
    {
        var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var playLine = lines.LastOrDefault(line => line.StartsWith("play ", StringComparison.OrdinalIgnoreCase));
        if (playLine is null || playLine[5..].Trim() is not { Length: > 0 } vertex)
        {
            throw new InvalidOperationException("cgos-genmove_analyze response has no play command.");
        }

        var json = lines.FirstOrDefault(line => line.StartsWith('{'));
        var analysis = CgosMoveAnalysisParser.Parse(json, vertex);
        var comment = CgosMoveAnalysisParser.ParseComment(json);
        return EngineCommandResult.EngineMove(vertex, playedBy, analysis, comment, json);
    }

    private EngineCommandException CreateEngineCommandException(GoStone stone, Exception exception)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        var message = $"{FormatColor(stone)} engine '{profile.DisplayName}' failed. Executable: {profile.ExecutablePath}";
        return new EngineCommandException(stone, message, exception);
    }

    private void SetEngineError(string message, GoStone stone, Exception? exception = null)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        ApplicationErrorLog.Write(
            "GTP ENGINE ERROR",
            $"Stone: {FormatColor(stone)}{Environment.NewLine}" +
            $"Engine: {profile.DisplayName}{Environment.NewLine}" +
            $"Executable: {profile.ExecutablePath}{Environment.NewLine}" +
            $"Message: {message}",
            exception);
        _session.SetEngineError(message, stone);
    }

    private GtpEngineSettings CreateEngineSettings(GoStone stone)
    {
        var profile = _session.GetGtpEngineProfile(stone);
        var logPrefix = stone == GoStone.Black ? "[black-engine]" : "[white-engine]";
        var appId = _session.UseKind == GoAppUseKind.LocalApps ? "ponnuki" : "play";
        return new GtpEngineSettings(
            profile.DisplayName,
            profile.ExecutablePath,
            profile.WorkingDirectoryModel,
            profile.Arguments,
            profile.EnableGtpLog,
            logPrefix,
            _session.UseKind == GoAppUseKind.LocalPlay
                ? _session.GetLocalMatchEngineGuiOptions(stone)
                : new Dictionary<string, string>(profile.GuiOptions),
            appId,
            "player");
    }

    private sealed record EngineCommandCompletion(EngineCommandResult Result, int Generation);

    private sealed record EngineCommandResult(
        string? MoveText,
        GoStone? PlayedBy,
        Exception? Error,
        GoStone? ErrorStone = null,
        bool MakesEngineReady = false,
        bool ClosesEngine = false,
        GoMoveAnalysis? Analysis = null,
        string Comment = "",
        string? CommonAnalysisJson = null,
        IReadOnlyList<EngineInitialPositionUpdate>? InitialPositionUpdates = null)
    {
        public static EngineCommandResult Success(bool closesEngine = false) => new(null, null, null, ClosesEngine: closesEngine);

        public static EngineCommandResult EngineReady() => new(null, null, null, MakesEngineReady: true);

        public static EngineCommandResult InitialPositionProgress(IReadOnlyList<EngineInitialPositionUpdate> updates) =>
            new(null, null, null, InitialPositionUpdates: updates);

        public static EngineCommandResult EngineMove(
            string moveText,
            GoStone playedBy,
            GoMoveAnalysis? analysis = null,
            string comment = "",
            string? commonAnalysisJson = null) =>
            new(
                moveText,
                playedBy,
                null,
                Analysis: analysis,
                Comment: comment,
                CommonAnalysisJson: commonAnalysisJson);

        public static EngineCommandResult Failure(Exception error, GoStone errorStone) => new(null, null, error, errorStone);
    }

    private sealed record EngineEntry(GoStone Stone, GtpEngineClient Client);

    private sealed record EngineInitialPositionUpdate(
        GoStone Stone,
        GtpCapabilitySet Capabilities,
        IGtpEngineCompatibilityProfile CompatibilityProfile,
        InitialPositionConciergeResult Result);

    private sealed class EngineInitialPositionState(GoStone stone, string engineName)
    {
        private readonly List<InitialPositionAttempt> _attempts = [];
        private readonly List<string> _diagnostics = [];

        public GoStone Stone { get; } = stone;

        public string EngineName { get; } = engineName;

        public GtpCapabilitySet? Capabilities { get; private set; }

        public IGtpEngineCompatibilityProfile? CompatibilityProfile { get; private set; }

        public InitialPositionConciergeCursor? Cursor { get; private set; }

        public bool IsAccepted { get; set; }

        public bool IsBusy { get; set; } = true;

        public InitialPositionAttempt? LastAttempt => _attempts.LastOrDefault();

        public void Apply(EngineInitialPositionUpdate update)
        {
            Capabilities = update.Capabilities;
            CompatibilityProfile = update.CompatibilityProfile;
            _attempts.AddRange(update.Result.Attempts);
            _diagnostics.AddRange(update.Result.Diagnostics);
            Cursor = update.Result.Continuation;
            IsAccepted = update.Result.IsVerified;
            IsBusy = false;
        }

        public void AddFailure(string diagnostic)
        {
            _diagnostics.Add(diagnostic);
            IsBusy = false;
        }

        public InitialPositionEngineProgressView CreateView(bool globalBusy) =>
            new(
                Stone,
                EngineName,
                IsAccepted,
                IsBusy || globalBusy,
                !globalBusy && !IsBusy && Cursor is not null,
                !globalBusy && !IsBusy && !IsAccepted &&
                    LastAttempt?.Status == InitialPositionAttemptStatus.UnverifiedSuccess,
                _attempts.ToArray(),
                _diagnostics.ToArray());
    }

    private sealed class EngineCommandException(GoStone stone, string message, Exception innerException)
        : Exception(message, innerException)
    {
        public GoStone Stone { get; } = stone;
    }
}
