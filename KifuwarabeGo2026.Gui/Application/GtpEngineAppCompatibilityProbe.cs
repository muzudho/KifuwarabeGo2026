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
        if (GtpEngineExecutableGuard.IsGuiApplication(profile))
            return new(GtpEngineAppCompatibilityKind.CheckFailed, GtpEngineExecutableGuard.GuiSelectedMessage);

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
                return appId.Equals("play", StringComparison.Ordinal) &&
                    role.Equals("player", StringComparison.Ordinal)
                    ? new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY GO PLAY")
                    : new(GtpEngineAppCompatibilityKind.Unsupported, $"{appId} NOT SUPPORTED");
            }

            if (role is not ("player" or "provider"))
            {
                throw new ArgumentException("role must be player or provider.", nameof(role));
            }

            var command = $"kfw-list-apps {role}";
            var response = await client.SendCommandAsync(command);
            var roleVerified = response.IsSuccess;
            if (!roleVerified)
            {
                command = "kfw-list-apps";
                response = await client.SendCommandAsync(command);
            }
            response.ThrowIfError(command);
            var appIds = response.Payload
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (appIds.Any(id => !AppIdPattern.IsMatch(id)) ||
                appIds.Distinct(StringComparer.Ordinal).Count() != appIds.Length)
                throw new InvalidOperationException("kfw-list-apps returned invalid app IDs.");
            if (appIds.Contains(appId, StringComparer.Ordinal))
                return new(
                    GtpEngineAppCompatibilityKind.Supported,
                    roleVerified
                        ? $"{appId} {role} READY"
                        : $"{appId} READY; ROLE UNVERIFIED");

            var runtimeSibling = GtpEngineExecutableGuard.FindRuntimeSpecificSibling(profile);
            return new(
                GtpEngineAppCompatibilityKind.Unsupported,
                runtimeSibling is null
                    ? $"{appId} NOT SUPPORTED"
                    : $"{appId} NOT SUPPORTED; SELECT {runtimeSibling}");
        }
        catch (Exception ex)
        {
            return new(GtpEngineAppCompatibilityKind.CheckFailed, $"CHECK FAILED: {ex.Message}");
        }
    }
}
