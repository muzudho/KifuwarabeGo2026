using KifuwarabeGo2026.PlayRoomEngine.JsonLines;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki;

await PlayRoomEngineJsonLinesHost.RunAsync(
    new PonnukiPlaySpaceProtocol(),
    new(
        SupportsMultipleSessions: false,
        ExitAfterDescribe: args.Contains("--exit-after-describe", StringComparer.Ordinal)));
