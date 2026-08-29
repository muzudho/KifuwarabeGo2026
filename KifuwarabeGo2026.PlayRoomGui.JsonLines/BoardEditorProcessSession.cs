namespace KifuwarabeGo2026.PlayRoomGui.JsonLines;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using System.Diagnostics;
using System.Text.Json;

/// <summary>独立Board Editorプロセスの起動から終了までを所有するクライアントです。</summary>
public sealed class BoardEditorProcessSession : IDisposable
{
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private bool _disposed;
    private bool _completed;

    private BoardEditorProcessSession(Process process, TimeSpan timeout, PlayRoomReady ready)
    {
        _process = process;
        _timeout = timeout;
        Ready = ready;
    }

    public PlayRoomReady Ready { get; }

    public static BoardEditorProcessSession Open(ProcessStartInfo startInfo, PlayRoomLaunchRequest request, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(request);
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(5);
        Configure(startInfo);
        var process = Process.Start(startInfo) ?? throw new IOException("Board Editor Play Roomを起動できませんでした。");
        try
        {
            var ready = Send<PlayRoomReady>(process, actualTimeout, PlayRoomJsonLinesProtocol.OpenMethod, request);
            return new BoardEditorProcessSession(process, actualTimeout, ready);
        }
        catch
        {
            Stop(process);
            process.Dispose();
            throw;
        }
    }

    public void ReplacePosition(BoardEditorPositionUpdate update)
    {
        EnsureSession(update.SessionId);
        _ = Send<PlayRoomReady>(_process, _timeout, PlayRoomJsonLinesProtocol.ReplacePositionMethod, update);
    }

    public BoardEditorCompletion Adopt() => Complete(PlayRoomJsonLinesProtocol.AdoptMethod);
    public BoardEditorCompletion Discard() => Complete(PlayRoomJsonLinesProtocol.DiscardMethod);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (!_completed && !_process.HasExited)
                _ = Send<BoardEditorCompletion>(_process, _timeout, PlayRoomJsonLinesProtocol.GoodbyeMethod,
                    new PlayRoomSessionCommand(Ready.SessionId));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or TimeoutException or InvalidOperationException) { }
        finally
        {
            Stop(_process);
            _process.Dispose();
        }
    }

    private BoardEditorCompletion Complete(string method)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = Send<BoardEditorCompletion>(_process, _timeout, method, new PlayRoomSessionCommand(Ready.SessionId));
        _completed = true;
        return result;
    }

    private void EnsureSession(string sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!string.Equals(sessionId, Ready.SessionId, StringComparison.Ordinal))
            throw new InvalidOperationException("Board Editor session ID does not match.");
    }

    private static T Send<T>(Process process, TimeSpan timeout, string method, object parameters)
    {
        if (process.HasExited) throw new IOException("Board Editor Play Roomが終了しています。");
        var requestId = Guid.NewGuid().ToString("N");
        var request = new PlayRoomProcessRequest(PlayRoomJsonLinesProtocol.Version, requestId, method,
            JsonSerializer.SerializeToElement(parameters, PlayRoomJsonLinesProtocol.JsonOptions));
        process.StandardInput.WriteLine(JsonSerializer.Serialize(request, PlayRoomJsonLinesProtocol.JsonOptions));
        process.StandardInput.Flush();
        var line = process.StandardOutput.ReadLineAsync().WaitAsync(timeout).GetAwaiter().GetResult();
        if (line is null) throw new IOException("Board Editor Play Roomが応答せずに終了しました。");
        PlayRoomProcessResponse response;
        try
        {
            response = JsonSerializer.Deserialize<PlayRoomProcessResponse>(line, PlayRoomJsonLinesProtocol.JsonOptions)
                ?? throw new JsonException("応答が null です。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Board Editor Play Roomから不正なJSON応答を受信しました。", exception);
        }
        if (response.ProtocolVersion != PlayRoomJsonLinesProtocol.Version)
            throw new InvalidDataException($"未対応のPlay Roomプロトコル版です: {response.ProtocolVersion}");
        if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
            throw new InvalidDataException("Play Room応答の要求識別番号が一致しません。");
        if (!response.Success) throw new InvalidOperationException(response.Error?.Message ?? "Play Room操作に失敗しました。");
        if (response.Result is null) throw new InvalidDataException("Play Room応答に結果がありません。");
        return response.Result.Value.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions)
            ?? throw new InvalidDataException("Play Room応答結果を読み取れませんでした。");
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
