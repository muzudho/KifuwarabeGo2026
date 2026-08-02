namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Gtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class GtpEngineAppCompatibilityProbe
{
    private static readonly Regex AppIdPattern = new("^[a-z][A-Za-z0-9]*$", RegexOptions.CultureInvariant);

    public static async Task<GtpEngineAppCompatibility> CheckAsync(
        GtpEngineProfile profile,
        string appId,
        string role)
    {
        try
        {
            var settings = new GtpEngineSettings(
                profile.DisplayName,
                profile.ExecutablePath,
                profile.WorkingDirectoryModel,
                profile.Arguments,
                profile.EnableGtpLog,
                "app-discovery",
                new Dictionary<string, string>(),
                appId,
                role);
            await using var client = new GtpEngineClient(settings, TimeSpan.FromSeconds(5));
            await client.StartAsync();

            var known = await client.SendCommandAsync("known_command kfw-list-apps");
            known.ThrowIfError("known_command kfw-list-apps");
            if (!known.Payload.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return appId.Equals("play", StringComparison.Ordinal)
                    ? new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY GO PLAY")
                    : new(GtpEngineAppCompatibilityKind.Unsupported, $"{appId} NOT SUPPORTED");
            }

            var response = await client.SendCommandAsync("kfw-list-apps");
            response.ThrowIfError("kfw-list-apps");
            var appIds = response.Payload
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (appIds.Any(id => !AppIdPattern.IsMatch(id)) ||
                appIds.Distinct(StringComparer.Ordinal).Count() != appIds.Length)
                throw new InvalidOperationException("kfw-list-apps returned invalid app IDs.");
            return appIds.Contains(appId, StringComparer.Ordinal)
                ? new(GtpEngineAppCompatibilityKind.Supported, $"{appId} READY")
                : new(GtpEngineAppCompatibilityKind.Unsupported, $"{appId} NOT SUPPORTED");
        }
        catch (Exception ex)
        {
            return new(GtpEngineAppCompatibilityKind.CheckFailed, $"CHECK FAILED: {ex.Message}");
        }
    }
}
