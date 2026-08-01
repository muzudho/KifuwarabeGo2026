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
        var response = await client.SendCommandAsync("known_command kfw-make-position");
        response.ThrowIfError("known_command kfw-make-position");
        var supported = response.Payload.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
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

        var document = JsonSerializer.Deserialize<PonnukiPositionDocument>(
            response.Payload,
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

    private static GtpEngineSettings CreateSettings(GtpEngineProfile profile) =>
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
