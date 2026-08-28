namespace KifuwarabeGo2026.LobbyEngine.JsonLines;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.LobbyEngine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

/// <summary>安全な読取操作だけを子プロセスへ送り、通信障害時は同一プロセス実装へ復旧します。</summary>
public sealed class JsonLinesLobbyEngine : ILobbyEngine
{
    private readonly Func<ProcessStartInfo> _hostStartInfoFactory;
    private readonly ILobbyEngine _fallback;
    private readonly TimeSpan _timeout;

    public JsonLinesLobbyEngine(Func<ProcessStartInfo> hostStartInfoFactory, ILobbyEngine fallback, TimeSpan? timeout = null)
    {
        _hostStartInfoFactory = hostStartInfoFactory ?? throw new ArgumentNullException(nameof(hostStartInfoFactory));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public string? CommunicationWarning { get; private set; }

    public LobbyState LoadState()
    {
        var fallbackState = _fallback.LoadState();
        try
        {
            var remote = Send<LobbyEntryList>(LobbyEngineJsonLinesProtocol.ListEntriesMethod);
            CommunicationWarning = null;
            return fallbackState with { Entries = remote.Entries.Select(entry => entry.Clone()).ToArray() };
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or TimeoutException or InvalidOperationException or Win32Exception)
        {
            CommunicationWarning = exception.Message;
            return fallbackState;
        }
    }

    public void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles) => _fallback.SaveGtpEngines(profiles);
    public void SaveEntries(IEnumerable<EntryProfile> profiles) => _fallback.SaveEntries(profiles);
    public void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles) => _fallback.SaveClientIdentities(profiles);
    public void SaveEntriesAndClientIdentities(IEnumerable<EntryProfile> entries, IEnumerable<ClientIdentityProfile> clientIdentities) =>
        _fallback.SaveEntriesAndClientIdentities(entries, clientIdentities);
    public void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles) => _fallback.SaveCgosConnections(profiles);

    private T Send<T>(string method)
    {
        var startInfo = _hostStartInfoFactory();
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        using var process = Process.Start(startInfo) ?? throw new IOException("ロビーエンジンホストを起動できませんでした。");
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var request = new LobbyEngineRequest(LobbyEngineJsonLinesProtocol.Version, requestId, method);
            process.StandardInput.WriteLine(JsonSerializer.Serialize(request, LobbyEngineJsonLinesProtocol.JsonOptions));
            process.StandardInput.Close();
            var line = process.StandardOutput.ReadLineAsync().WaitAsync(_timeout).GetAwaiter().GetResult();
            if (line is null) throw new IOException("ロビーエンジンホストが応答せずに終了しました。");

            LobbyEngineResponse response;
            try
            {
                response = JsonSerializer.Deserialize<LobbyEngineResponse>(line, LobbyEngineJsonLinesProtocol.JsonOptions)
                    ?? throw new JsonException("応答が null です。");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("ロビーエンジンから不正な JSON 応答を受信しました。", exception);
            }

            if (response.ProtocolVersion != LobbyEngineJsonLinesProtocol.Version)
                throw new InvalidDataException($"未対応のロビープロトコル版です: {response.ProtocolVersion}");
            if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
                throw new InvalidDataException("ロビーエンジン応答の要求識別番号が一致しません。");
            if (!response.Success)
                throw new InvalidOperationException(response.Error?.Message ?? "ロビーエンジンの読取操作に失敗しました。");
            if (response.Result is null) throw new InvalidDataException("ロビーエンジンの応答に結果がありません。");
            return response.Result.Value.Deserialize<T>(LobbyEngineJsonLinesProtocol.JsonOptions)
                ?? throw new InvalidDataException("ロビーエンジンの応答結果を読み取れませんでした。");
        }
        finally
        {
            if (!process.WaitForExit(1000))
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
            }
        }
    }
}
