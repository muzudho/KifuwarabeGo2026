namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                string command = "kfw-options";
                var known = await client.SendCommandAsync("known_command " + command, deadline.Token);
                if (!known.IsSuccess || !known.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    command = "gui_options";
                    known = await client.SendCommandAsync("known_command " + command, deadline.Token);
                    if (!known.IsSuccess || !known.Payload.Equals("true", StringComparison.OrdinalIgnoreCase)) return null;
                }
                var response = await client.SendCommandAsync(command, deadline.Token);
                response.ThrowIfError(command);
                var document = GtpGuiOptionsDocument.Parse(response.Payload);
                if (document.Version != 1) throw new InvalidOperationException("Unsupported engine option schema.");
                if (document.Options.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() != document.Options.Count)
                    throw new InvalidOperationException("Duplicate engine option IDs.");
                return document.Options.Select(o => new GtpEngineGuiOptionSpec(o.Id, o.Label, o.Type,
                    o.Default, o.Min, o.Max, o.Vars)).ToArray();
            }).GetAwaiter().GetResult();
            profile.DiscoveredGuiOptions = specs;
            EngineOptionDiscoveryError = "";
        }
        catch (Exception error)
        {
            EngineOptionDiscoveryError = error.Message;
        }
    }

    public string EngineOptionDiscoveryError { get; private set; } = "";
}
