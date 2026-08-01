using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace KifuwarabeGo2026.Gui.Application.Local.Apps.Ponnuki;

public static class PonnukiPositionProvider
{
    public static async Task<(bool IsSupported, string Message)> CheckCapabilityAsync(GtpEngineProfile profile)
    {
        var settings = CreateSettings(profile);
        await using var client = new GtpEngineClient(settings, TimeSpan.FromSeconds(10));
        await client.StartAsync();
        var makePositionResponse = await client.SendCommandAsync("known_command kfw-make-position");
        makePositionResponse.ThrowIfError("known_command kfw-make-position");
        var listenMoveResponse = await client.SendCommandAsync("known_command kfw-listen-move");
        listenMoveResponse.ThrowIfError("known_command kfw-listen-move");
        var supported = makePositionResponse.Payload.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) &&
                        listenMoveResponse.Payload.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        return supported
            ? (true, "PONNUKI v1 READY")
            : (false, "PONNUKI v1 NOT SUPPORTED");
    }

    public static async Task<GoGameRecord> MakePositionAsync(GtpEngineProfile profile)
    {
        var app = LocalAppCatalog.Ponnuki;
        var settings = CreateSettings(profile);

        await using var client = new GtpEngineClient(settings, TimeSpan.FromSeconds(10));
        await client.StartAsync();
        var command = $"kfw-make-position {app.Id} {app.Version} {app.BoardSize} {app.InitialRandomMoveCount}";
        var response = await client.SendCommandAsync(command);
        response.ThrowIfError(command);

        return ParsePosition(response.Payload);
    }

    internal static GoGameRecord ParsePosition(string payload)
    {
        var app = LocalAppCatalog.Ponnuki;
        var document = JsonSerializer.Deserialize<PonnukiPositionDocument>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The App Provider returned an empty position.");
        if (!string.Equals(document.App, app.Id, StringComparison.OrdinalIgnoreCase) ||
            document.Version != app.Version || document.BoardSize != app.BoardSize)
            throw new InvalidOperationException("The App Provider returned an incompatible Ponnuki position.");

        var record = new GoGameRecord
        {
            GameName = "Ponnuki",
            RuleName = "Ponnuki v1",
            BoardSize = document.BoardSize,
            Komi = 0m,
        };
        AddStones(record, document.Black, GoStone.Black);
        AddStones(record, document.White, GoStone.White);
        return record;
    }

    internal static int ParseSeed(string payload)
    {
        using var json = JsonDocument.Parse(payload);
        return json.RootElement.TryGetProperty("seed", out var seedElement) && seedElement.TryGetInt32(out var seed)
            ? seed
            : throw new InvalidOperationException("The App Provider did not return the random seed.");
    }

    internal static GtpEngineSettings CreateSettings(GtpEngineProfile profile) =>
        new(
            profile.DisplayName,
            profile.ExecutablePath,
            profile.WorkingDirectoryModel,
            profile.Arguments,
            profile.EnableGtpLog,
            "app-provider",
            new Dictionary<string, string>(profile.GuiOptions));

    private static void AddStones(GoGameRecord record, IReadOnlyList<string>? vertices, GoStone stone)
    {
        if (vertices is null) return;
        foreach (var vertex in vertices)
        {
            if (!GtpCoordinate.TryParseVertex(vertex, record.BoardSize, out var point))
                throw new InvalidOperationException($"The App Provider returned an invalid vertex: {vertex}");
            record.SetupStones.Add(new GoGameSetupStone(stone, point));
        }
    }

    private sealed class PonnukiPositionDocument
    {
        public string App { get; set; } = "";
        public int Version { get; set; }
        public int BoardSize { get; set; }
        public List<string> Black { get; set; } = [];
        public List<string> White { get; set; } = [];
        public string ToPlay { get; set; } = "black";
        public int Seed { get; set; }
    }
}
