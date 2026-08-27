namespace KifuwarabeGo2026.LauncherEngine.JsonLines;

using System.Diagnostics;
using System.Text.Json;
using KifuwarabeGo2026.LauncherEngine;

public sealed class JsonLinesLauncherEngine : ILauncherEngine, IDisposable
{
    private readonly ILauncherEngine _fallback;
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private readonly object _gate = new();
    private bool _disposed;

    public JsonLinesLauncherEngine(ProcessStartInfo hostStartInfo, ILauncherEngine fallback, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(hostStartInfo);
        ArgumentNullException.ThrowIfNull(fallback);
        _fallback = fallback;
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
        hostStartInfo.UseShellExecute = false;
        hostStartInfo.RedirectStandardInput = true;
        hostStartInfo.RedirectStandardOutput = true;
        hostStartInfo.RedirectStandardError = true;
        hostStartInfo.CreateNoWindow = true;
        _process = Process.Start(hostStartInfo)
            ?? throw new InvalidOperationException("ランチャーエンジンホストを起動できませんでした。");
        _process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data)) Trace.WriteLine($"LauncherEngineHost: {eventArgs.Data}");
        };
        _process.BeginErrorReadLine();
    }

    public LauncherState GetState() => Send<LauncherState>(LauncherEngineJsonLinesProtocol.GetStateMethod);

    public IReadOnlyList<InstalledVersion> GetInstalledVersions() =>
        Send<List<InstalledVersion>>(LauncherEngineJsonLinesProtocol.GetInstalledVersionsMethod);

    public Task<LauncherOperationResult<string>> UpdateAsync(
        LauncherProduct product,
        IProgress<LauncherProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        _fallback.UpdateAsync(product, progress, cancellationToken);

    public LauncherOperationResult Uninstall(InstalledVersion installedVersion) => _fallback.Uninstall(installedVersion);
    public LauncherOperationResult<LauncherLaunchDetails> StartGui() => _fallback.StartGui();
    public string? GetCurrentDirectory(LauncherProduct product) => _fallback.GetCurrentDirectory(product);
    public LauncherOperationResult<LauncherState> ChangeInstallationDirectory(string? directory) => _fallback.ChangeInstallationDirectory(directory);
    public LauncherOperationResult<LauncherState> ChangeScreenshotDirectory(string directory) => _fallback.ChangeScreenshotDirectory(directory);
    public LauncherOperationResult<LauncherState> ChangeCloseAfterStartingGui(bool value) => _fallback.ChangeCloseAfterStartingGui(value);

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _process.StandardInput.Close(); }
            catch (InvalidOperationException) { }
        }

        if (!_process.WaitForExit(2000))
        {
            try { _process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        }
        _process.Dispose();
    }

    private T Send<T>(string method)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process.HasExited) throw new IOException("ランチャーエンジンホストが終了しています。");

            var requestId = Guid.NewGuid().ToString("N");
            var request = new LauncherEngineRequest(LauncherEngineJsonLinesProtocol.Version, requestId, method);
            _process.StandardInput.WriteLine(JsonSerializer.Serialize(request, LauncherEngineJsonLinesProtocol.JsonOptions));
            _process.StandardInput.Flush();

            string? line;
            try
            {
                line = _process.StandardOutput.ReadLineAsync().WaitAsync(_timeout).GetAwaiter().GetResult();
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException($"ランチャーエンジンから {_timeout.TotalSeconds:0.#} 秒以内に応答がありませんでした。", exception);
            }

            if (line is null) throw new IOException("ランチャーエンジンホストが応答せずに終了しました。");
            LauncherEngineResponse response;
            try
            {
                response = JsonSerializer.Deserialize<LauncherEngineResponse>(line, LauncherEngineJsonLinesProtocol.JsonOptions)
                    ?? throw new JsonException("応答が null です。");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("ランチャーエンジンから不正な JSON 応答を受信しました。", exception);
            }

            if (response.ProtocolVersion != LauncherEngineJsonLinesProtocol.Version)
                throw new InvalidDataException($"未対応のプロトコルバージョンです: {response.ProtocolVersion}");
            if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
                throw new InvalidDataException("応答の要求識別番号が一致しません。");
            if (!response.Success) throw new InvalidOperationException(response.Error ?? "ランチャーエンジンの処理に失敗しました。");
            if (response.Result is null) throw new InvalidDataException("ランチャーエンジンの応答に結果がありません。");
            return response.Result.Value.Deserialize<T>(LauncherEngineJsonLinesProtocol.JsonOptions)
                ?? throw new InvalidDataException("ランチャーエンジンの応答結果を読み取れませんでした。");
        }
    }
}
