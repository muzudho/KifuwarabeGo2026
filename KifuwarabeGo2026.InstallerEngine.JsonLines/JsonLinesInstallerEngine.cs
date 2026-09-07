namespace KifuwarabeGo2026.InstallerEngine.JsonLines;

using System.Diagnostics;
using System.Text.Json;
using KifuwarabeGo2026.InstallerEngine;

public sealed class JsonLinesInstallerEngine : IInstallerEngine, IInstallerEngineCommunicationStatus, IDisposable
{
    private readonly IInstallerEngine _fallback;
    private readonly Process _process;
    private readonly TimeSpan _timeout;
    private readonly object _gate = new();
    private bool _disposed;
    private bool _useFallback;

    public string? CommunicationWarning { get; private set; }

    public JsonLinesInstallerEngine(ProcessStartInfo hostStartInfo, IInstallerEngine fallback, TimeSpan? timeout = null)
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
            ?? throw new InvalidOperationException("インストーラーエンジンホストを起動できませんでした。");
        _process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data)) Trace.WriteLine($"InstallerEngineHost: {eventArgs.Data}");
        };
        _process.BeginErrorReadLine();
    }

    public InstallerState GetState() => SendOrFallback(
        InstallerEngineJsonLinesProtocol.GetStateMethod,
        null,
        _fallback.GetState);

    public IReadOnlyList<InstalledVersion> GetInstalledVersions() =>
        SendOrFallback<List<InstalledVersion>>(
            InstallerEngineJsonLinesProtocol.GetInstalledVersionsMethod,
            null,
            () => [.. _fallback.GetInstalledVersions()]);

    public Task<InstallerOperationResult<string>> UpdateAsync(
        InstallerProduct product,
        IProgress<InstallerProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        _fallback.UpdateAsync(product, progress, cancellationToken);

    public InstallerOperationResult Uninstall(InstalledVersion installedVersion) => SendOrFallback(
        InstallerEngineJsonLinesProtocol.UninstallMethod,
        new UninstallParameters(installedVersion),
        () => _fallback.Uninstall(installedVersion));
    public InstallerOperationResult<InstallerLaunchDetails> StartGui() => _fallback.StartGui();
    public string? GetCurrentDirectory(InstallerProduct product) => SendOrFallback<string?>(
        InstallerEngineJsonLinesProtocol.GetCurrentDirectoryMethod,
        new InstallerProductParameters(product),
        () => _fallback.GetCurrentDirectory(product),
        allowNull: true);
    public InstallerOperationResult<InstallerState> ChangeInstallationDirectory(string? directory)
    {
        var result = SendOrFallback(
            InstallerEngineJsonLinesProtocol.ChangeInstallationDirectoryMethod,
            new InstallationDirectoryParameters(directory),
            () => _fallback.ChangeInstallationDirectory(directory));
        if (!_useFallback && result.IsSuccess) _ = _fallback.ChangeInstallationDirectory(directory);
        return result;
    }

    public InstallerOperationResult<InstallerState> ChangeScreenshotDirectory(string directory)
    {
        var result = SendOrFallback(
            InstallerEngineJsonLinesProtocol.ChangeScreenshotDirectoryMethod,
            new ScreenshotDirectoryParameters(directory),
            () => _fallback.ChangeScreenshotDirectory(directory));
        if (!_useFallback && result.IsSuccess) _ = _fallback.ChangeScreenshotDirectory(directory);
        return result;
    }

    public InstallerOperationResult<InstallerState> ChangeCloseAfterStartingGui(bool value)
    {
        var result = SendOrFallback(
            InstallerEngineJsonLinesProtocol.ChangeCloseAfterStartingGuiMethod,
            new CloseAfterStartingGuiParameters(value),
            () => _fallback.ChangeCloseAfterStartingGui(value));
        if (!_useFallback && result.IsSuccess) _ = _fallback.ChangeCloseAfterStartingGui(value);
        return result;
    }

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

    private T SendOrFallback<T>(string method, object? parameters, Func<T> fallback, bool allowNull = false)
    {
        if (_useFallback) return fallback();
        try { return Send<T>(method, parameters, allowNull); }
        catch (Exception exception) when (exception is IOException or InvalidDataException or TimeoutException or InvalidOperationException)
        {
            CommunicationWarning = exception.Message;
            _useFallback = true;
            StopFailedHost();
            return fallback();
        }
    }

    private T Send<T>(string method, object? parameters = null, bool allowNull = false)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process.HasExited) throw new IOException("インストーラーエンジンホストが終了しています。");

            var requestId = Guid.NewGuid().ToString("N");
            var request = new InstallerEngineRequest(
                InstallerEngineJsonLinesProtocol.Version,
                requestId,
                method,
                parameters is null ? null : JsonSerializer.SerializeToElement(parameters, InstallerEngineJsonLinesProtocol.JsonOptions));
            _process.StandardInput.WriteLine(JsonSerializer.Serialize(request, InstallerEngineJsonLinesProtocol.JsonOptions));
            _process.StandardInput.Flush();

            string? line;
            try
            {
                line = _process.StandardOutput.ReadLineAsync().WaitAsync(_timeout).GetAwaiter().GetResult();
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException($"インストーラーエンジンから {_timeout.TotalSeconds:0.#} 秒以内に応答がありませんでした。", exception);
            }

            if (line is null) throw new IOException("インストーラーエンジンホストが応答せずに終了しました。");
            InstallerEngineResponse response;
            try
            {
                response = JsonSerializer.Deserialize<InstallerEngineResponse>(line, InstallerEngineJsonLinesProtocol.JsonOptions)
                    ?? throw new JsonException("応答が null です。");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("インストーラーエンジンから不正な JSON 応答を受信しました。", exception);
            }

            if (response.ProtocolVersion != InstallerEngineJsonLinesProtocol.Version)
                throw new InvalidDataException($"未対応のプロトコルバージョンです: {response.ProtocolVersion}");
            if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
                throw new InvalidDataException("応答の要求識別番号が一致しません。");
            if (!response.Success) throw new InvalidOperationException(response.Error ?? "インストーラーエンジンの処理に失敗しました。");
            if (response.Result is null)
            {
                if (allowNull) return default!;
                throw new InvalidDataException("インストーラーエンジンの応答に結果がありません。");
            }
            var value = response.Result.Value.Deserialize<T>(InstallerEngineJsonLinesProtocol.JsonOptions);
            if (value is null && !allowNull) throw new InvalidDataException("インストーラーエンジンの応答結果を読み取れませんでした。");
            return value!;
        }
    }

    private void StopFailedHost()
    {
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
    }
}
