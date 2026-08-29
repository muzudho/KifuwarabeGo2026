namespace KifuwarabeGo2026.PlayRoomGui.JsonLines;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using System.Diagnostics;
using System.Text.Json;

/// <summary>独立 Match Play Room プロセスとの読み取り専用セッションです。</summary>
public sealed class MatchProcessSession : IDisposable
{
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private bool _disposed;
    private bool _completed;

    private MatchProcessSession(Process process, TimeSpan timeout, PlayRoomReady ready)
    {
        _process = process;
        _timeout = timeout;
        Ready = ready;
    }

    public PlayRoomReady Ready { get; }

    public static MatchProcessSession Open(ProcessStartInfo startInfo, PlayRoomLaunchRequest request, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(request);
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(5);
        Configure(startInfo);
        var process = Process.Start(startInfo) ?? throw new IOException("Match Play Room を起動できませんでした。");
        try
        {
            var ready = Send<PlayRoomReady>(process, actualTimeout, PlayRoomJsonLinesProtocol.OpenMethod, request);
            return new MatchProcessSession(process, actualTimeout, ready);
        }
        catch
        {
            Stop(process);
            process.Dispose();
            throw;
        }
    }

    public MatchViewState UpdateState(MatchStateUpdate update)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Send<MatchViewState>(_process, _timeout, PlayRoomJsonLinesProtocol.UpdateStateMethod, update);
    }

    public MatchActionAccepted SubmitAction(MatchActionRequest action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Send<MatchActionAccepted>(_process, _timeout, PlayRoomJsonLinesProtocol.SubmitActionMethod, action);
    }

    public MatchCompletion Complete(MatchCompletionCommand command)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = Send<MatchCompletion>(_process, _timeout, PlayRoomJsonLinesProtocol.CompleteMethod, command);
        _completed = true;
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (!_completed && !_process.HasExited)
                _ = Send<MatchCompletion>(_process, _timeout, PlayRoomJsonLinesProtocol.GoodbyeMethod,
                    new PlayRoomSessionCommand(Ready.SessionId));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or TimeoutException or InvalidOperationException) { }
        finally
        {
            Stop(_process);
            _process.Dispose();
        }
    }

    private static T Send<T>(Process process, TimeSpan timeout, string method, object parameters)
    {
        if (process.HasExited) throw new IOException("Match Play Room が終了しています。");
        var requestId = Guid.NewGuid().ToString("N");
        var request = new PlayRoomProcessRequest(PlayRoomJsonLinesProtocol.Version, requestId, method,
            JsonSerializer.SerializeToElement(parameters, PlayRoomJsonLinesProtocol.JsonOptions));
        process.StandardInput.WriteLine(JsonSerializer.Serialize(request, PlayRoomJsonLinesProtocol.JsonOptions));
        process.StandardInput.Flush();
        var line = process.StandardOutput.ReadLineAsync().WaitAsync(timeout).GetAwaiter().GetResult();
        if (line is null) throw new IOException("Match Play Room が応答せずに終了しました。");
        PlayRoomProcessResponse response;
        try
        {
            response = JsonSerializer.Deserialize<PlayRoomProcessResponse>(line, PlayRoomJsonLinesProtocol.JsonOptions)
                ?? throw new JsonException("応答が null です。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Match Play Room から不正な JSON 応答を受信しました。", exception);
        }
        if (response.ProtocolVersion != PlayRoomJsonLinesProtocol.Version)
            throw new InvalidDataException($"未対応の Play Room プロトコル版です: {response.ProtocolVersion}");
        if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
            throw new InvalidDataException("Play Room 応答の要求識別番号が一致しません。");
        if (!response.Success) throw new InvalidOperationException(response.Error?.Message ?? "Match 操作に失敗しました。");
        if (response.Result is null) throw new InvalidDataException("Play Room 応答に結果がありません。");
        return response.Result.Value.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions)
            ?? throw new InvalidDataException("Play Room 応答結果を読み取れませんでした。");
    }

    private static void Configure(ProcessStartInfo startInfo)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
    }

    private static void Stop(Process process)
    {
        try { process.StandardInput.Close(); }
        catch (InvalidOperationException) { }
        if (!process.WaitForExit(1000))
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }
    }
}
