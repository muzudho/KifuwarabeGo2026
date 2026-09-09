namespace KifuwarabeGo2026.Reference.MatchRunner.Go;

using System.Diagnostics;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.PlayRoom.Launching;
using KifuwarabeGo2026.PlayRoomGui.JsonLines;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

/// <summary>Official Lobby's Go match runner: Protocol G authority and a public stdio window.</summary>
public sealed class GoStdioMatchLauncher(Func<PlayRoomLaunchRequest, ProcessStartInfo> startInfoFactory)
    : IPlayRoomProcessLauncher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlayRoomProcessCompletionResult> LaunchAsync(PlayRoomLaunchRequest request,
        IProgress<PlayRoomProcessReadyNotification>? readyProgress = null, CancellationToken cancellationToken = default)
    {
        // The Lobby calls this on its draw thread; process startup and progression belong on a worker.
        return await Task.Run(() => RunAsync(request, readyProgress, cancellationToken), CancellationToken.None);
    }

    private async Task<PlayRoomProcessCompletionResult> RunAsync(PlayRoomLaunchRequest request,
        IProgress<PlayRoomProcessReadyNotification>? progress, CancellationToken ct)
    {
        var wasReady = false;
        PollingMatchProcessSession? window = null;
        GoLocalMatchGtpController? players = null;
        Task<GoLocalMatchAction>? thinking = null;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var concierge = new GameOasisConcierge();
        IGuiProtocol protocol = new GameOasisGuiProtocol(concierge);
        GuiGameSnapshot? snapshot = null;
        PlayRoomProcessCompletionResult result;
        try
        {
            if (request.RoomTypeId != PlayRoomIds.Match || string.IsNullOrWhiteSpace(request.RequestId) ||
                !GoPlayRoomLaunchInterpreter.TryCreate(request, out var plan, out _, out _) || plan is null)
                throw new InvalidDataException("Unsupported Go Match launch request.");
            foreach (var participant in plan.Participants.Where(p => p.Kind.Equals("computer", StringComparison.OrdinalIgnoreCase)))
                if (!plan.PlayerConnections.Any(p => p.RoleId == participant.RoleId))
                    throw new InvalidDataException($"Missing computer connection for {participant.RoleId}.");
            Require(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol(), ct));
            snapshot = Require(await protocol.OpenSessionAsync(new(request.PlaySpaceTypeId, request.Configuration), ct)).InitialSnapshot;
            players = new GoLocalMatchGtpController(plan);
            await players.InitializeAsync(stop.Token);

            // Configuration contains the authoritative setup. The display has no engine connections or Lobby storage.
            var displayRequest = request with
            {
                InitialPosition = null,
                Participants = request.Participants.Select(p => p with { PlayerConnection = null, EngineOptions = null }).ToArray(),
            };
            var start = startInfoFactory(displayRequest);
            window = await PollingMatchProcessSession.OpenAsync(start, displayRequest, ct);
            long revision = 0, lastEvent = 0;
            await window.UpdateAsync(new(window.Ready.SessionId, revision, Display(snapshot)), ct);
            wasReady = true;
            progress?.Report(new(request.RequestId, "ready", "Go Match is ready through standard I/O."));
            MatchCompletion? completion = null;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var events = await window.ReadEventsAsync(ct);
                foreach (var input in events.Events)
                {
                    if (input.EventId <= lastEvent || input.Revision > revision)
                        throw new InvalidDataException("Invalid input event order or revision.");
                    lastEvent = input.EventId;
                    if (input.Type == "closed")
                    {
                        if (!events.Closed || input.Completion is not { Status: MatchCompletionStatus.Closed } closed ||
                            closed.SessionId != window.Ready.SessionId)
                            throw new InvalidDataException("Invalid window closure event.");
                        completion = closed;
                    }
                    else if (input.Type == "action" && input.Action is { } action)
                    {
                        if (action.SessionId != window.Ready.SessionId || string.IsNullOrWhiteSpace(action.ActionId))
                            throw new InvalidDataException("Invalid input action session or ID.");
                        if (events.Closed) continue;
                        var turn = Turn(snapshot);
                        if (input.Revision == revision && action.PlayerRoleId == turn &&
                            !players.HasEngine(Stone(turn)) && !snapshot.IsTerminal)
                        {
                            var applied = Require(await protocol.SubmitActionAsync(new(snapshot.SessionId,
                                Action(action), snapshot.Revision), ct));
                            snapshot = applied.Snapshot;
                            if (applied.IsAccepted)
                            {
                                if (action.Kind == MatchActionKind.PlayPoint)
                                    await players.PlayHumanAsync(Stone(turn), new(action.X!.Value, action.Y!.Value), stop.Token);
                                else if (action.Kind == MatchActionKind.Pass)
                                    await players.PassHumanAsync(Stone(turn), stop.Token);
                            }
                        }
                        // Rejections also release pending display input with a fresh projection revision.
                        await window.UpdateAsync(new(window.Ready.SessionId, ++revision, Display(snapshot)), ct);
                    }
                    else throw new InvalidDataException("Unsupported input event.");
                }
                if (events.Closed)
                {
                    if (completion is null) throw new InvalidDataException("Missing window closure result.");
                    break;
                }
                if (snapshot.IsTerminal)
                {
                    using var outcome = JsonDocument.Parse(snapshot.Outcome!.Content);
                    var winner = outcome.RootElement.GetProperty("winner").GetString();
                    var reason = outcome.RootElement.GetProperty("reason").GetString();
                    completion = await window.CompleteAsync(new(window.Ready.SessionId, Display(snapshot), winner, reason), ct);
                    break;
                }
                var current = Turn(snapshot);
                if (players.HasEngine(Stone(current)))
                {
                    thinking ??= players.GenerateMoveAsync(Stone(current), stop.Token);
                    if (thinking.IsCompleted)
                    {
                        var move = await thinking;
                        thinking = null;
                        var action = new MatchActionRequest(window.Ready.SessionId, Guid.NewGuid().ToString("N"), current,
                            move.Kind switch { GoLocalMatchActionKind.Play => MatchActionKind.PlayPoint,
                                GoLocalMatchActionKind.Pass => MatchActionKind.Pass, _ => MatchActionKind.Resign },
                            move.Point?.X, move.Point?.Y);
                        var applied = Require(await protocol.SubmitActionAsync(new(snapshot.SessionId,
                            Action(action), snapshot.Revision), ct));
                        if (!applied.IsAccepted) throw new InvalidDataException("Computer returned an illegal move: " + applied.Rejection?.Message);
                        snapshot = applied.Snapshot;
                        await window.UpdateAsync(new(window.Ready.SessionId, ++revision, Display(snapshot)), ct);
                    }
                }
                await Task.Delay(100, ct);
            }
            var exitCode = await window.WaitForExitAsync(ct);
            result = new(PlayRoomProcessCompletionStatus.ExitedNormally, request.RequestId, exitCode,
                WasReady: wasReady, Match: completion);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            result = new(PlayRoomProcessCompletionStatus.Cancelled, request.RequestId,
                ErrorCode: "match-cancelled", Message: "The match was cancelled.", WasReady: wasReady);
        }
        catch (Exception ex)
        {
            result = new(wasReady ? PlayRoomProcessCompletionStatus.ExitedAbnormally : PlayRoomProcessCompletionStatus.StartFailed,
                request.RequestId, ErrorCode: "stdio-match-failed", Message: ex.Message, WasReady: wasReady);
        }
        finally
        {
            stop.Cancel();
            if (thinking is not null) { try { await thinking; } catch { } }
            if (players is not null) { try { await players.DisposeAsync(); } catch { } }
            if (snapshot is not null) await protocol.CloseSessionAsync(new(snapshot.SessionId), CancellationToken.None);
        }
        if (window is not null)
        {
            try { await window.DisposeAsync(); }
            catch (Exception ex)
            {
                result = result with { Status = PlayRoomProcessCompletionStatus.ExitedAbnormally,
                    ErrorCode = "window-cleanup-failed", Message = ex.Message };
            }
            result = result with { Diagnostic = window.Diagnostic };
        }
        return result;
    }

    private static T Require<T>(ProtocolResponse<T> response) => response.IsSuccess && response.Value is not null
        ? response.Value : throw new InvalidOperationException(response.Error?.Message ?? "Protocol G/S operation failed.");

    private static GoStone Stone(string role) => role == "black" ? GoStone.Black : GoStone.White;
    private static string Turn(GuiGameSnapshot snapshot)
    {
        using var state = JsonDocument.Parse(snapshot.State.Content);
        return state.RootElement.GetProperty("nextToPlay").GetString()!;
    }

    private static ContractDocument Action(MatchActionRequest action) => new("application/json", GoSchemas.Action,
        JsonSerializer.Serialize(new { version = 1, type = action.Kind switch
            { MatchActionKind.PlayPoint => "play", MatchActionKind.Pass => "pass", MatchActionKind.Resign => "resign", _ => "invalid" },
            player = action.PlayerRoleId, x = action.X, y = action.Y }, JsonOptions));

    private static ContractDocument Display(GuiGameSnapshot snapshot)
    {
        using var state = JsonDocument.Parse(snapshot.State.Content);
        var root = state.RootElement;
        var stones = new List<object>();
        foreach (var color in new[] { "black", "white" })
            foreach (var point in root.GetProperty(color).EnumerateArray())
                stones.Add(new { x = point.GetProperty("x").GetInt32(), y = point.GetProperty("y").GetInt32(), color });
        return new("application/json", GameOasisOfficialNames.Go + ".display-state.v1",
            JsonSerializer.Serialize(new { version = 1, boardSize = root.GetProperty("boardSize").GetInt32(),
                currentTurn = root.GetProperty("nextToPlay").GetString(), stones, gameOver = snapshot.IsTerminal }, JsonOptions));
    }
}
