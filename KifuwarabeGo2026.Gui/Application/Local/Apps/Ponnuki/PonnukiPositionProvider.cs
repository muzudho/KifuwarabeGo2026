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
    public static async Task<GoGameRecord> MakePositionAsync(GtpEngineProfile profile)
    {
        var app = LocalAppCatalog.Ponnuki;
        var settings = new GtpEngineSettings(
            profile.DisplayName,
            profile.ExecutablePath,
            profile.WorkingDirectoryModel,
            profile.Arguments,
            profile.EnableGtpLog,
            "app-provider",
            new Dictionary<string, string>(profile.GuiOptions));

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
