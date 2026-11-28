using System.Text.Json.Nodes;

// GUIには一つのGTPエンジンとして見せる。子の設定内容をここへ複製しない。
internal sealed class SlotOptionProxy
{
    private static readonly string[] Choices = ["要求ファイルに従う", "１系", "２系", "３系", "４系", "５系", "６系", "７系", "８系", "９系"];
    private static readonly string[] OptionCommands = ["kfw-options", "kfw-get-option", "kfw-set-option"];
    private EngineLaunch? launch;
    private int selection;
    private bool gameStarted;
    private readonly List<string> acceptedOptions = new();

    public async Task<int> RunAsync()
    {
        try
        {
            while (await Console.In.ReadLineAsync() is { } line)
            {
                var words = line.Split('#')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (words.Count == 0) continue;
                string id = "";
                if (words[0].All(char.IsAsciiDigit)) { id = words[0]; words.RemoveAt(0); }
                string command = words.FirstOrDefault() ?? "";
                string[] args = words.Skip(1).ToArray();
                string[] response;
                if (command == "known_command" && args.Length == 1 && OptionCommands.Contains(args[0]))
                    response = [$"={id} true"];
                // このプロキシで合成するオプションはversion 1。別のスキーマ経路へ迂回しない。
                else if (command == "known_command" && args is ["kfw-describe-options"])
                    response = [$"={id} false"];
                else if (command == "kfw-options" && args.Length == 0)
                    response = [$"={id} {await DescribeAsync()}"];
                else if (command == "kfw-get-option" && args is ["EngineSlot"])
                    response = [$"={id} {Choices[selection]}"];
                else if (command == "kfw-set-option" && args.Length > 0 && args[0] == "EngineSlot")
                {
                    int requested = args.Length == 2 ? Array.IndexOf(Choices, args[1]) : -1;
                    if (requested < 0) response = [$"?{id} invalid EngineSlot value"];
                    else if (gameStarted) response = [$"?{id} EngineSlot applies only before the game starts"];
                    else
                    {
                        selection = requested;
                        acceptedOptions.Clear();
                        await StopAsync();
                        response = [$"={id}"];
                    }
                }
                else if (command == "quit" && args.Length == 0 && launch is null)
                    response = [$"={id}"];
                else
                {
                    bool metadata = command is "known_command" or "list_commands" or "name" or "version" or "protocol_version"
                        or "quit" or "kfw-get-option" or "kfw-set-option";
                    if (!metadata && !gameStarted)
                    {
                        await StopAsync();
                        launch = SwitchControl.Start(selection == 0 ? null : selection);
                        foreach (string option in acceptedOptions)
                        {
                            var applied = await ForwardAsync(option);
                            if (!applied[0].StartsWith('=')) throw new IOException("Selected engine rejected saved option: " + applied[0]);
                        }
                        gameStarted = true;
                    }
                    EnsurePreview();
                    response = await ForwardAsync(line);
                    if (command == "kfw-set-option" && response[0].StartsWith('=') && !gameStarted)
                        acceptedOptions.Add(string.Join(' ', words));
                    if (command == "list_commands" && response[0].StartsWith('='))
                        response = response.Concat(OptionCommands).Distinct().ToArray();
                }
                await Console.Out.WriteAsync(string.Join('\n', response) + "\n\n");
                await Console.Out.FlushAsync();
                if (command == "quit" && args.Length == 0) break;
            }
            return 0;
        }
        finally { await StopAsync(); }
    }

    private void EnsurePreview() => launch ??= SwitchControl.Start(selection == 0 ? null : selection, consumeRequest: false);

    private async Task<string> DescribeAsync()
    {
        EnsurePreview();
        var options = new JsonArray { new JsonObject {
            ["id"] = "EngineSlot", ["label"] = "Wrapper: ｎ系", ["type"] = "combo",
            ["default"] = Choices[0], ["value"] = Choices[selection],
            ["vars"] = new JsonArray(Choices.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()) } };
        var known = await ForwardAsync("known_command kfw-options");
        if (known[0].StartsWith('=') && Payload(known).Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            var response = await ForwardAsync("kfw-options");
            if (!response[0].StartsWith('=')) throw new IOException("Selected engine rejected kfw-options.");
            var document = JsonNode.Parse(Payload(response));
            if (document?["version"]?.GetValue<int>() != 1 || document["options"] is not JsonArray children)
                throw new InvalidDataException("Selected engine returned an unsupported option document.");
            var ids = new HashSet<string>(StringComparer.Ordinal) { "EngineSlot" };
            foreach (var child in children)
            {
                string optionId = child?["id"]?.GetValue<string>() ?? "";
                if (optionId.Length == 0 || !ids.Add(optionId))
                    throw new InvalidDataException("Duplicate or reserved option ID: " + optionId);
                options.Add(child!.DeepClone());
            }
        }
        return new JsonObject { ["version"] = 1, ["options"] = options }.ToJsonString();
    }

    private static string Payload(string[] response) => string.Join('\n', response)[1..].TrimStart();

    private async Task<string[]> ForwardAsync(string command)
    {
        var child = launch!.Process;
        await child.StandardInput.WriteLineAsync(command);
        await child.StandardInput.FlushAsync();
        var response = new List<string>();
        while (true)
        {
            string? part = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(9));
            if (part is null) throw new IOException("Selected engine closed its GTP output.");
            if (part.Length == 0) { if (response.Count > 0) break; continue; }
            response.Add(part);
        }
        return response.ToArray();
    }

    private async Task StopAsync()
    {
        if (launch is not { } old) return;
        launch = null;
        using var child = old.Process;
        if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
        old.WriteStatus("stopped", child.ExitCode);
    }
}
