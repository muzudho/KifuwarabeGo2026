using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.PlaySpace.JsonLines;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki;

var implementation = ReadOption(args, "--play-space") ?? "go";
IPlaySpaceProtocol playSpace = implementation switch
{
    "go" => new GoPlaySpaceProtocol(),
    "ponnuki" => new PonnukiPlaySpaceProtocol(),
    _ => throw new ArgumentException($"Unknown play-space implementation: {implementation}"),
};
await PlaySpaceJsonLinesHost.RunAsync(playSpace, new(
    SupportsMultipleSessions: !args.Contains("--single-session", StringComparer.Ordinal),
    ExitAfterDescribe: args.Contains("--exit-after-describe", StringComparer.Ordinal)));

static string? ReadOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
