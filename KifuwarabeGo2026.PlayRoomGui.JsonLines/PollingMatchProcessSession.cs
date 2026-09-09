namespace KifuwarabeGo2026.PlayRoomGui.JsonLines;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

/// <summary>Single-reader polling window client. All requests must be awaited sequentially.</summary>
public sealed class PollingMatchProcessSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task _stderr;
    private readonly StringBuilder _diagnostic = new();
    private readonly TimeSpan _timeout;
    public PlayRoomReady Ready { get; private set; } = null!;
    public string Diagnostic { get { lock (_diagnostic) return _diagnostic.ToString(); } }

    private PollingMatchProcessSession(Process process, TimeSpan timeout)
    {
        _process = process;
        _timeout = timeout;
        _stderr = Task.Run(async () =>
        {
            var buffer = new char[1024];
            int count;
            while ((count = await process.StandardError.ReadAsync(buffer)) > 0)
                lock (_diagnostic)
                    _diagnostic.Append(buffer, 0, Math.Min(count, 4000 - _diagnostic.Length));
        });
    }

    public static async Task<PollingMatchProcessSession> OpenAsync(ProcessStartInfo start,
        PlayRoomLaunchRequest request, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = start.RedirectStandardOutput = start.RedirectStandardError = true;
        start.StandardInputEncoding = start.StandardOutputEncoding = start.StandardErrorEncoding = new UTF8Encoding(false);
        var process = Process.Start(start) ?? throw new IOException("Could not start Play Room.");
        var client = new PollingMatchProcessSession(process, timeout ?? TimeSpan.FromSeconds(35));
        try
        {
            var description = await client.SendAsync<JsonElement>("describe", new { }, cancellationToken);
            var capabilities = description.GetProperty("capabilities").EnumerateArray().Select(c => c.GetString()).ToArray();
            if (!capabilities.Contains("poll-input-events.v1") || !capabilities.Contains("go-display-state.v1") ||
                !description.GetProperty("roomTypes").EnumerateArray().Any(r => r.GetString() == "match"))
                throw new InvalidDataException("Play Room does not support the required Match capabilities.");
            client.Ready = await client.SendAsync<PlayRoomReady>("open", request, cancellationToken);
            if (client.Ready.RequestId != request.RequestId || client.Ready.RoomTypeId != request.RoomTypeId ||
                string.IsNullOrWhiteSpace(client.Ready.SessionId))
                throw new InvalidDataException("Mismatched Play Room readiness response.");
            return client;
        }
        catch { await client.DisposeAsync(); throw; }
    }

    public async Task UpdateAsync(MatchStateUpdate state, CancellationToken ct)
    {
        var result = await SendAsync<MatchViewState>("updateState", state, ct);
        if (result.SessionId != Ready.SessionId || result.Revision != state.Revision || result.State != state.State)
            throw new InvalidDataException("Mismatched display acknowledgement.");
    }

    public async Task<MatchInputEvents> ReadEventsAsync(CancellationToken ct)
    {
        var result = await SendAsync<MatchInputEvents>("readEvents", new PlayRoomSessionCommand(Ready.SessionId), ct);
        if (result.SessionId != Ready.SessionId || result.Events is null)
            throw new InvalidDataException("Mismatched input session.");
        return result;
    }

    public async Task<MatchCompletion> CompleteAsync(MatchCompletionCommand command, CancellationToken ct)
    {
        var result = await SendAsync<MatchCompletion>("complete", command, ct);
        if (result.SessionId != Ready.SessionId || result.Status != MatchCompletionStatus.Finished ||
            result.FinalState != command.FinalState || result.WinnerRoleId != command.WinnerRoleId || result.Reason != command.Reason)
            throw new InvalidDataException("Mismatched Match completion.");
        return result;
    }

    public async Task<int> WaitForExitAsync(CancellationToken ct)
    {
        await _process.WaitForExitAsync(ct).WaitAsync(TimeSpan.FromSeconds(5), ct);
        await _stderr.WaitAsync(TimeSpan.FromSeconds(5), ct);
        if (_process.ExitCode != 0) throw new IOException($"Play Room exited with code {_process.ExitCode}. {Diagnostic}");
        return _process.ExitCode;
    }

    private async Task<T> SendAsync<T>(string method, object parameters, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_timeout);
        var id = Guid.NewGuid().ToString("N");
        var request = new PlayRoomProcessRequest(1, id, method,
            JsonSerializer.SerializeToElement(parameters, PlayRoomJsonLinesProtocol.JsonOptions));
        try
        {
            await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request,
                PlayRoomJsonLinesProtocol.JsonOptions).AsMemory(), timeout.Token);
            await _process.StandardInput.FlushAsync(timeout.Token);
            var line = await _process.StandardOutput.ReadLineAsync(timeout.Token)
                ?? throw new IOException("Play Room exited without a response.");
            var response = JsonSerializer.Deserialize<PlayRoomProcessResponse>(line, PlayRoomJsonLinesProtocol.JsonOptions)
                ?? throw new InvalidDataException("Null response.");
            if (response.ProtocolVersion != 1 || response.RequestId != id)
                throw new InvalidDataException("Mismatched response version or request ID.");
            if (!response.Success) throw new InvalidDataException(response.Error?.Message ?? "Play Room rejected the request.");
            return response.Result is { } value
                ? value.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions) ?? throw new InvalidDataException("Null result.")
                : throw new InvalidDataException("Missing result.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new TimeoutException($"Play Room timed out during {method}."); }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            try { _process.StandardInput.Close(); }
            catch (IOException) { }
            catch (InvalidOperationException) { }
            try { await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException)
            {
                if (!_process.HasExited) _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            await _stderr.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally { _process.Dispose(); }
    }
}
