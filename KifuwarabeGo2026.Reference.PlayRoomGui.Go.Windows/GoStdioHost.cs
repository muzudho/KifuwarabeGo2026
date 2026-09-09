namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoomGui.JsonLines;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

public static class GoStdioHost
{
    public static int Run(bool headless)
    {
        Console.InputEncoding = new System.Text.UTF8Encoding(false);
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        var hostFailed = false;
        var sessionReady = new TaskCompletionSource<GoStdioSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        var windowReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource();
        GoStdioSession? session = null;
        var protocol = Task.Run(async () =>
        {
            using var input = new StreamReader(Console.OpenStandardInput(), new System.Text.UTF8Encoding(false));
            try
            {
                while (await input.ReadLineAsync().WaitAsync(stop.Token) is { } line)
                {
                    string id = "";
                    bool finish = false;
                    PlayRoomProcessResponse response;
                    try
                    {
                        var request = JsonSerializer.Deserialize<PlayRoomProcessRequest>(line, PlayRoomJsonLinesProtocol.JsonOptions)
                            ?? throw new InvalidDataException("Null request.");
                        id = request.RequestId;
                        if (request.ProtocolVersion != 1 || string.IsNullOrWhiteSpace(id))
                            throw new InvalidDataException("Unsupported version or missing requestId.");
                        T Read<T>() where T : class => request.Parameters?.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions)
                            ?? throw new InvalidDataException("Missing parameters.");
                        object result;
                        switch (request.Method)
                        {
                            case "describe":
                                result = new { protocolVersion = 1, roomTypes = new[] { "match" },
                                    capabilities = new[] { "go-display-state.v1", "poll-input-events.v1" }, headless };
                                break;
                            case "open":
                                if (session is not null) throw new InvalidDataException("Already open.");
                                var launch = Read<PlayRoomLaunchRequest>();
                                if (launch.RoomTypeId != "match" || !GoPlayRoomLaunchInterpreter.TryCreate(launch, out var plan, out _, out _) || plan is null)
                                    throw new InvalidDataException("Unsupported Match launch request.");
                                // External authority owns players and position updates in this mode.
                                if (plan.PlayerConnections.Count != 0 || launch.InitialPosition is not null)
                                    throw new InvalidDataException("Use configuration setupStones and updateState; local player connections and initialPosition are not supported in stdio mode.");
                                session = new GoStdioSession(plan);
                                sessionReady.TrySetResult(session);
                                await windowReady.Task.WaitAsync(TimeSpan.FromSeconds(30));
                                result = new PlayRoomReady(launch.RequestId, session.SessionId, "match");
                                break;
                            case "updateState":
                                result = Require().Update(Read<MatchStateUpdate>());
                                break;
                            case "readEvents":
                                var events = Require().ReadEvents(Read<PlayRoomSessionCommand>().SessionId);
                                result = events;
                                finish = events.Closed;
                                break;
                            case "complete":
                                result = Require().Complete(Read<MatchCompletionCommand>());
                                finish = true;
                                break;
                            case "goodbye":
                                result = Require().Close(Read<PlayRoomSessionCommand>().SessionId);
                                finish = true;
                                break;
                            default: throw new InvalidDataException("Unknown method.");
                        }
                        response = new(1, id, true, JsonSerializer.SerializeToElement(result, PlayRoomJsonLinesProtocol.JsonOptions), null);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                        response = new(1, id, false, null, new("invalid-request", ex.Message));
                        if (session is not null && !windowReady.Task.IsCompletedSuccessfully) finish = true;
                    }
                    Console.Out.WriteLine(JsonSerializer.Serialize(response, PlayRoomJsonLinesProtocol.JsonOptions));
                    Console.Out.Flush();
                    if (finish) break;
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (session is not null) session.Close(session.SessionId);
                sessionReady.TrySetCanceled();
            }
            GoStdioSession Require() => session ?? throw new InvalidDataException("Open a session first.");
        });
        try
        {
            var current = sessionReady.Task.GetAwaiter().GetResult();
            if (headless) windowReady.TrySetResult();
            else
            {
                using var game = new GoStdioGame(current, () => windowReady.TrySetResult());
                game.Run();
                current.Close(current.SessionId);
                // Allow a polling caller to receive the window-close event, then reap an abandoned host.
                stop.CancelAfter(TimeSpan.FromSeconds(10));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            hostFailed = true;
            windowReady.TrySetException(ex);
            if (session is not null) session.Close(session.SessionId);
            stop.CancelAfter(TimeSpan.FromSeconds(10));
            Console.Error.WriteLine(ex);
        }
        try { protocol.GetAwaiter().GetResult(); return hostFailed ? 5 : 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 5; }
    }
}
