namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KifuwarabeGo2026.FormalAdapter.Gtp.Client;
using KifuwarabeGo2026.FormalAdapter.Gtp.Options;

public sealed partial class GoAppSession
{
    // ラッパーかどうかは判別せず、同じプロトコルで項目を取得する。
    private void DiscoverEngineOptions(Dictionary<string, string> values)
    {
        var profile = IsEntryEngineOptionsDialogOpen && FindGtpEngineIndex(PlayerEditDraft.EngineProfileId) is var index && index >= 0
            ? _gtpEngineProfiles[index] : GtpEngineEditDraft;
        if (!File.Exists(profile.ExecutablePath)) return;
        try
        {
            var specs = Task.Run(async () =>
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                string directory = string.IsNullOrWhiteSpace(profile.WorkingDirectoryStr)
                    ? Path.GetDirectoryName(Path.GetFullPath(profile.ExecutablePath))! : profile.WorkingDirectoryStr;
                await using var client = new GtpEngineClient(new GtpEngineSettings(profile.DisplayName,
                    profile.ExecutablePath, directory, profile.Arguments, false, GuiOptions: values));
                await client.StartAsync(deadline.Token);
                var legacy = await ReadLegacyOptionsAsync(client, deadline.Token);
                var modern = await ReadPlayerOptionSchemaAsync(client, legacy is null, deadline.Token);
                var merged = legacy ?? modern;
                if (merged is null) return null;
                if (legacy is not null && modern is not null)
                {
                    // Keep the established display of editable legacy options.
                    foreach (var external in modern)
                    {
                        var existing = merged.FindIndex(option => option.Id == external.Id);
                        if (existing >= 0) merged[existing] = external;
                        else merged.Add(external);
                    }
                }
                if (merged.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() != merged.Count)
                    throw new InvalidOperationException("Duplicate engine option IDs.");
                return merged.ToArray();
            }).GetAwaiter().GetResult();
            profile.DiscoveredGuiOptions = specs;
            EngineOptionDiscoveryError = "";
        }
        catch (Exception error)
        {
            EngineOptionDiscoveryError = error.Message;
        }
    }

    private static async Task<List<GtpEngineGuiOptionSpec>?> ReadLegacyOptionsAsync(
        GtpEngineClient client, CancellationToken token)
    {
        foreach (var command in new[] { "kfw-options", "gui_options" })
        {
            var known = await client.SendCommandAsync("known_command " + command, token);
            if (!known.IsSuccess || !known.Payload.Equals("true", StringComparison.OrdinalIgnoreCase)) continue;
            var response = await client.SendCommandAsync(command, token);
            response.ThrowIfError(command);
            var document = GtpGuiOptionsDocument.Parse(response.Payload);
            if (document.Version != 1) throw new InvalidOperationException("Unsupported engine option schema.");
            return document.Options.Select(o => new GtpEngineGuiOptionSpec(o.Id, o.Label, o.Type,
                o.Default, o.Min, o.Max, o.Vars, ExternallyManaged: o.ExternallyManaged, ExternalValue: o.Value)).ToList();
        }
        return null;
    }

    private static async Task<List<GtpEngineGuiOptionSpec>?> ReadPlayerOptionSchemaAsync(
        GtpEngineClient client, bool includeEditable, CancellationToken token)
    {
        const string describe = "kfw-describe-options";
        var known = await client.SendCommandAsync($"known_command {describe}", token);
        if (!known.IsSuccess || !known.Payload.Equals("true", StringComparison.OrdinalIgnoreCase)) return null;
        var response = await client.SendCommandAsync($"{describe} play player", token);
        if (!response.IsSuccess) return null;
        var schema = GtpOptionSchemaDocument.Parse(response.Payload);
        if (schema.Version != 1 || schema.App != "play" || schema.Role != "player")
            throw new InvalidOperationException("Unexpected engine option schema.");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (schema.Options.Any(option => option.ExternallyManaged && option.Type != "action"))
        {
            var current = await client.SendCommandAsync("kfw-get-options play player", token);
            if (current.IsSuccess)
            {
                using var document = JsonDocument.Parse(current.Payload);
                var root = document.RootElement;
                if (root.GetProperty("version").GetInt32() == 1 && root.GetProperty("app").GetString() == "play" &&
                    root.GetProperty("role").GetString() == "player")
                    foreach (var property in root.GetProperty("values").EnumerateObject())
                        if (AsOptionText(property.Value) is { } value) values[property.Name] = value;
            }
        }

        return schema.Options.Where(option => includeEditable || option.ExternallyManaged).Select(option =>
            new GtpEngineGuiOptionSpec(option.Id, option.Label, option.Type switch
            {
                "boolean" => "check",
                "integer" => "spin",
                "enum" => "combo",
                "file" => "filename",
                "action" => "button",
                _ => "string",
            }, AsOptionText(option.Default) ?? "", option.Minimum, option.Maximum, option.Values, option.Binding,
                ExternallyManaged: option.ExternallyManaged, ExternalValue: values.GetValueOrDefault(option.Id))).ToList();
    }

    private static string? AsOptionText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True or JsonValueKind.False or JsonValueKind.Number => value.GetRawText(),
        _ => null,
    };

    public string EngineOptionDiscoveryError { get; private set; } = "";
}
