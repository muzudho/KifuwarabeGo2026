using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace KifuwarabeGo2026.Gui.Application.Local.Apps.Ponnuki;

public sealed class PonnukiProviderGameSession : IAsyncDisposable
{
    private readonly GtpEngineClient _client;

    public PonnukiProviderGameSession(GtpEngineProfile profile)
    {
        _client = new GtpEngineClient(PonnukiPositionProvider.CreateSettings(profile), TimeSpan.FromSeconds(10));
    }

    public int Seed { get; private set; }

    public async Task<GoGameRecord> StartAsync()
    {
        var app = LocalAppCatalog.Ponnuki;
        await _client.StartAsync();
        var command = $"kfw-make-position {app.Id} {app.Version} {app.BoardSize} {app.InitialRandomMoveCount}";
        var response = await _client.SendCommandAsync(command);
        response.ThrowIfError(command);
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

    public ValueTask DisposeAsync() => _client.DisposeAsync();
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
