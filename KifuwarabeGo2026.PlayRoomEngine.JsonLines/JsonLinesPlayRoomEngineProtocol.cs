namespace KifuwarabeGo2026.PlayRoomEngine.JsonLines;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using System.Diagnostics;
using System.Text.Json;

/// <summary>Protocol Sを標準入出力JSON Lines越しに利用するクライアントです。</summary>
public sealed class JsonLinesPlayRoomEngineProtocol : IPlaySpaceProtocol, IDisposable
{
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private bool _disposed;

    public JsonLinesPlayRoomEngineProtocol(ProcessStartInfo startInfo, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        _process = Process.Start(startInfo) ?? throw new IOException("PlaySpace Hostを起動できませんでした。");
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public ValueTask<ProtocolResponse<PlaySpaceDescriptor>> DescribeAsync(CancellationToken cancellationToken = default) =>
        Send<PlaySpaceDescriptor>(PlayRoomEngineJsonLinesProtocol.DescribeMethod, null, cancellationToken);

    public ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(CancellationToken cancellationToken = default) =>
        Send<ContractDocument>(PlayRoomEngineJsonLinesProtocol.GetConfigurationSchemaMethod, null, cancellationToken);

    public ValueTask<ProtocolResponse<PlaySpaceConfigurationValidation>> ValidateConfigurationAsync(
        ValidatePlaySpaceConfigurationRequest request, CancellationToken cancellationToken = default) =>
        Send<PlaySpaceConfigurationValidation>(PlayRoomEngineJsonLinesProtocol.ValidateConfigurationMethod, request, cancellationToken);

    public ValueTask<ProtocolResponse<PlaySpaceSessionCreated>> CreateSessionAsync(
        CreatePlaySpaceSessionRequest request, CancellationToken cancellationToken = default) =>
        Send<PlaySpaceSessionCreated>(PlayRoomEngineJsonLinesProtocol.CreateSessionMethod, request, cancellationToken);

    public ValueTask<ProtocolResponse<PlaySpaceSnapshot>> GetSnapshotAsync(
        GetPlaySpaceSnapshotRequest request, CancellationToken cancellationToken = default) =>
        Send<PlaySpaceSnapshot>(PlayRoomEngineJsonLinesProtocol.GetSnapshotMethod, request, cancellationToken);

    public ValueTask<ProtocolResponse<PlaySpaceActionApplied>> ApplyActionAsync(
        ApplyPlaySpaceActionRequest request, CancellationToken cancellationToken = default) =>
        Send<PlaySpaceActionApplied>(PlayRoomEngineJsonLinesProtocol.ApplyActionMethod, request, cancellationToken);

    public ValueTask<ProtocolResponse<PlaySpaceSessionClosed>> CloseSessionAsync(
        ClosePlaySpaceSessionRequest request, CancellationToken cancellationToken = default) =>
        Send<PlaySpaceSessionClosed>(PlayRoomEngineJsonLinesProtocol.CloseSessionMethod, request, cancellationToken);

    private ValueTask<ProtocolResponse<T>> Send<T>(string method, object? parameters, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (_process.HasExited) throw new IOException("PlaySpace Hostが終了しています。");
        var requestId = Guid.NewGuid().ToString("N");
        var element = parameters is null ? (JsonElement?)null :
            JsonSerializer.SerializeToElement(parameters, PlayRoomEngineJsonLinesProtocol.JsonOptions);
        var request = new PlayRoomEngineProcessRequest(PlayRoomEngineJsonLinesProtocol.Version, requestId, method, element);
        _process.StandardInput.WriteLine(JsonSerializer.Serialize(request, PlayRoomEngineJsonLinesProtocol.JsonOptions));
        _process.StandardInput.Flush();
        var line = _process.StandardOutput.ReadLineAsync(cancellationToken).AsTask()
            .WaitAsync(_timeout, cancellationToken).GetAwaiter().GetResult();
        if (line is null)
        {
            var diagnostic = _process.StandardError.ReadToEnd();
            throw new IOException(string.IsNullOrWhiteSpace(diagnostic)
                ? "PlaySpace Hostが応答せずに終了しました。"
                : $"PlaySpace Hostが応答せずに終了しました。{Environment.NewLine}{diagnostic.Trim()}");
        }
        PlayRoomEngineProcessResponse response;
        try
        {
            response = JsonSerializer.Deserialize<PlayRoomEngineProcessResponse>(line, PlayRoomEngineJsonLinesProtocol.JsonOptions)
                ?? throw new JsonException("応答がnullです。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("PlaySpace Hostから不正なJSON応答を受信しました。", exception);
        }
        if (response.ProtocolVersion != PlayRoomEngineJsonLinesProtocol.Version)
            throw new InvalidDataException($"未対応のPlaySpaceプロトコル版です: {response.ProtocolVersion}");
        if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
            throw new InvalidDataException("PlaySpace応答の要求識別番号が一致しません。");
        if (!response.Success) throw new InvalidOperationException(response.Error?.Message ?? "PlaySpace通信に失敗しました。");
        if (response.Result is null) throw new InvalidDataException("PlaySpace応答に結果がありません。");
        var result = response.Result.Value.Deserialize<PlayRoomEngineProtocolResult<T>>(PlayRoomEngineJsonLinesProtocol.JsonOptions)
            ?? throw new InvalidDataException("Protocol S応答を読み取れませんでした。");
        return ValueTask.FromResult(result.Error is null && result.Value is not null
            ? ProtocolResponse<T>.Success(result.Value)
            : ProtocolResponse<T>.Failure(result.Error ?? new ProtocolError("empty-response", "Protocol S応答に値がありません。")));
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            if (!_process.HasExited)
            {
                var request = new PlayRoomEngineProcessRequest(PlayRoomEngineJsonLinesProtocol.Version, Guid.NewGuid().ToString("N"),
                    PlayRoomEngineJsonLinesProtocol.GoodbyeMethod);
                _process.StandardInput.WriteLine(JsonSerializer.Serialize(request, PlayRoomEngineJsonLinesProtocol.JsonOptions));
                _process.StandardInput.Flush();
            }
        }
        catch (InvalidOperationException) { }
        finally
        {
            _disposed = true;
            try { _process.StandardInput.Close(); } catch (InvalidOperationException) { }
            if (!_process.WaitForExit(1000))
                try { _process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            _process.Dispose();
        }
    }
}
