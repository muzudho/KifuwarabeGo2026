using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Casual.Ponnuki;

public sealed class PonnukiProviderGameSession : IAsyncDisposable
{
    private readonly GtpEngineClient _client;
    private bool _usesAppLifecycle;
    private bool _appStarted;

    public PonnukiProviderGameSession(GtpEngineProfile profile)
    {
        _client = new GtpEngineClient(PonnukiPositionProvider.CreateSettings(profile), TimeSpan.FromSeconds(10));
    }

    public int Seed { get; private set; }

    public async Task<GoGameRecord> StartAsync()
    {
        var app = CasualAppCatalog.Ponnuki;
        await _client.StartAsync();
        var startSupported = await IsCommandSupportedAsync("kfw-start-app");
        var endSupported = await IsCommandSupportedAsync("kfw-end-app");
        _usesAppLifecycle = startSupported && endSupported;
        var command = _usesAppLifecycle
            ? $"kfw-start-app {app.Id} provider"
            : $"kfw-make-position {app.Id} {app.Version} {app.BoardSize} {app.InitialRandomMoveCount}";
        var response = await _client.SendCommandAsync(command);
        response.ThrowIfError(command);
        _appStarted = _usesAppLifecycle;
        Seed = PonnukiPositionProvider.ParseSeed(response.Payload);
        return PonnukiPositionProvider.ParsePosition(response.Payload);
    }

    public async Task<PonnukiMoveResult> ListenMoveAsync(string vertex)
    {
        var command = $"kfw-listen-move {vertex}";
        var response = await _client.SendCommandAsync(command);
        response.ThrowIfError(command);
        return JsonSerializer.Deserialize<PonnukiMoveResult>(
            response.Payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The App Provider returned an empty move result.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_appStarted)
        {
            try
            {
                await _client.SendCommandAsync("kfw-end-app ponnuki provider");
            }
            catch (Exception)
            {
                // Disposal must continue even if the Provider has already exited.
            }
            _appStarted = false;
        }
        await _client.DisposeAsync();
    }

    private async Task<bool> IsCommandSupportedAsync(string command)
    {
        var response = await _client.SendCommandAsync($"known_command {command}");
        return response.IsSuccess && response.Payload.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PonnukiMoveResult
{
    public bool Accepted { get; set; }
    public bool GameOver { get; set; }
    public string Winner { get; set; } = "";
    public string Reason { get; set; } = "";
    public int BlackCaptures { get; set; }
    public int WhiteCaptures { get; set; }
    public string NextToPlay { get; set; } = "";

    public GoStone? WinnerStone => Winner.ToLowerInvariant() switch
    {
        "black" => GoStone.Black,
        "white" => GoStone.White,
        _ => null,
    };
}
