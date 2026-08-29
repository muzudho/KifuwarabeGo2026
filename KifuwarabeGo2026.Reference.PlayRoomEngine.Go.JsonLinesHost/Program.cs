using KifuwarabeGo2026.PlayRoomEngine.JsonLines;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go;

await PlayRoomEngineJsonLinesHost.RunAsync(
    new GoPlaySpaceProtocol(),
    new(
        SupportsMultipleSessions: true,
        ExitAfterDescribe: args.Contains("--exit-after-describe", StringComparer.Ordinal)));
