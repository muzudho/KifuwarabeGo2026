namespace KifuwarabeGo2026.PlayRoomEngine.JsonLines;

using System.Diagnostics;
using System.Text.Json;

/// <summary>Conciergeが具象PlaySpaceアセンブリを参照せずにホストを起動するための記述です。</summary>
public sealed record PlayRoomEngineHostManifest(
    int Version,
    string PlaySpaceTypeId,
    string Command,
    IReadOnlyList<string> Arguments,
    bool SupportsMultipleSessions)
{
    public static PlayRoomEngineHostManifest Load(string manifestPath)
    {
        var manifest = JsonSerializer.Deserialize<PlayRoomEngineHostManifest>(
            File.ReadAllText(manifestPath), PlayRoomEngineJsonLinesProtocol.JsonOptions)
            ?? throw new InvalidDataException("PlaySpace Hostマニフェストを読み取れませんでした。");
        if (manifest.Version != 1 || string.IsNullOrWhiteSpace(manifest.PlaySpaceTypeId) ||
            string.IsNullOrWhiteSpace(manifest.Command))
            throw new InvalidDataException("PlaySpace Hostマニフェストが不正です。");
        return manifest;
    }

    public ProcessStartInfo CreateStartInfo(string manifestDirectory)
    {
        var start = new ProcessStartInfo(Command);
        foreach (var argument in Arguments)
        {
            var resolved = argument.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !Path.IsPathRooted(argument)
                ? Path.Combine(manifestDirectory, argument)
                : argument;
            start.ArgumentList.Add(resolved);
        }
        return start;
    }
}
