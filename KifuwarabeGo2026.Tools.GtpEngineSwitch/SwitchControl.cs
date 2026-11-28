using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

internal sealed record EngineLaunch(Process Process, int Primary, string Executable, string StatusPath)
{
    public void WriteStatus(string state, int? exitCode = null) => SwitchControl.TryWrite(StatusPath, new
    {
        schemaVersion = 1, state, runningPrimary = Primary, wrapperPid = Environment.ProcessId,
        enginePid = Process.Id, executable = Executable, updatedUtc = DateTimeOffset.UtcNow, exitCode,
    });
}

internal static class SwitchControl
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly string Root = AppContext.BaseDirectory;
    private static readonly string ConfigurationPath = Path.Combine(Root, "engine-switch.json");
    private static readonly string RequestPath = Path.Combine(Root, "request-primary.json");

    public static EngineLaunch Start(int? fixedPrimary = null, bool consumeRequest = true)
    {
        // Serialize next-game selection, request consumption, and configuration updates across wrappers.
        using var gate = AcquireGate();
        var configuration = JsonNode.Parse(File.ReadAllText(ConfigurationPath)) as JsonObject
            ?? throw new InvalidDataException("engine-switch.json must be an object.");
        int current = configuration["primary"]?.GetValue<int>() ?? 0;
        ValidateSlot(current);
        // 固定指定・能力問い合わせ用の仮起動では、共通主系と未処理要求に触れない。
        if (fixedPrimary.HasValue || !consumeRequest)
        {
            int selectedPrimary = fixedPrimary ?? current;
            ValidateSlot(selectedPrimary);
            var fixedEngine = Resolve(configuration, selectedPrimary);
            return RecordLaunch(StartProcess(fixedEngine), selectedPrimary, fixedEngine.Executable);
        }
        string? claimed = null;
        if (File.Exists(RequestPath))
        {
            var directory = Path.Combine(Root, "switch-requests");
            Directory.CreateDirectory(directory);
            claimed = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".request.json");
            // Claim by rename: a newer request written to the original path will not be deleted.
            File.Move(RequestPath, claimed);
        }
        if (claimed is not null)
        {
            string? requestId = null;
            int? requested = null;
            Process? candidate = null;
            try
            {
                var request = JsonNode.Parse(File.ReadAllText(claimed)) as JsonObject
                    ?? throw new InvalidDataException("request-primary.json must be an object.");
                requestId = request["request-id"]?.GetValue<string>();
                requested = request["request-primary"]?.GetValue<int>()
                    ?? throw new InvalidDataException("request-primary is required.");
                ValidateSlot(requested.Value);
                var engine = Resolve(configuration, requested.Value);
                candidate = StartProcess(engine);
                // Commit only after validation and successful process creation.
                configuration["primary"] = requested.Value;
                WriteAtomic(ConfigurationPath, configuration);
                Receipt(claimed, requestId, requested, "accepted", requested.Value, null);
                return RecordLaunch(candidate, requested.Value, engine.Executable);
            }
            catch (Exception error) when (IsControlError(error))
            {
                if (candidate is not null)
                {
                    try { if (!candidate.HasExited) { candidate.Kill(entireProcessTree: true); candidate.WaitForExit(); } }
                    finally { candidate.Dispose(); }
                }
                Receipt(claimed, requestId, requested, "rejected", current, error.Message);
                Console.Error.WriteLine("GTP switch request rejected; keeping primary " + current + ": " + error.Message);
            }
        }
        var selected = Resolve(configuration, current);
        return RecordLaunch(StartProcess(selected), current, selected.Executable);
    }

    private static EngineLaunch RecordLaunch(Process child, int primary, string executable)
    {
        var path = Path.Combine(Root, "engine-status", Environment.ProcessId + ".json");
        var launch = new EngineLaunch(child, primary, executable, path);
        launch.WriteStatus("running");
        return launch;
    }

    private static void Receipt(string claimed, string? requestId, int? requested, string status, int selected, string? error)
    {
        var receipt = new { schemaVersion = 1, requestId, requestedPrimary = requested, status,
            selectedPrimary = selected, processedUtc = DateTimeOffset.UtcNow, wrapperPid = Environment.ProcessId,
            archivedRequest = claimed, error };
        TryWrite(Path.ChangeExtension(claimed, ".result.json"), receipt);
        TryWrite(Path.Combine(Root, "request-primary-result.json"), receipt);
    }

    private static void ValidateSlot(int primary)
    {
        if (primary is < 1 or > 9) throw new InvalidDataException("Primary must be between 1 and 9.");
    }

    private static (string Executable, string[] Arguments) Resolve(JsonObject configuration, int primary)
    {
        if (configuration["engines"] is not JsonArray engines || engines.Count is < 1 or > 9 || primary > engines.Count || engines[primary - 1] is not JsonObject slot)
            throw new InvalidDataException("Requested slot is not configured (up to nine engine slots supported).");
        var path = slot["path"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidDataException("Engine path is required.");
        var executable = Path.GetFullPath(path, Root);
        var directory = Path.GetDirectoryName(executable)!;
        var name = Path.GetFileNameWithoutExtension(executable);
        foreach (string file in new[] { executable, Path.Combine(directory, name + ".dll"),
            Path.Combine(directory, name + ".deps.json"), Path.Combine(directory, name + ".runtimeconfig.json") })
            if (!File.Exists(file)) throw new FileNotFoundException("Engine runtime file is missing.", file);
        var arguments = slot["arguments"]?.Deserialize<string[]>(Json) ?? [];
        return (executable, arguments);
    }

    private static Process StartProcess((string Executable, string[] Arguments) engine)
    {
        var start = new ProcessStartInfo(engine.Executable)
        {
            WorkingDirectory = Path.GetDirectoryName(engine.Executable)!, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true,
        };
        foreach (string argument in engine.Arguments) start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start the selected GTP engine.");
    }

    private static FileStream AcquireGate()
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            try { return new FileStream(Path.Combine(Root, "engine-switch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (clock.Elapsed < TimeSpan.FromSeconds(5)) { Thread.Sleep(50); }
        }
    }

    public static bool IsControlError(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        JsonException or System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException or FormatException;

    public static void TryWrite(string path, object value)
    {
        try { WriteAtomic(path, value); }
        catch (Exception error) when (IsControlError(error)) { Console.Error.WriteLine("GTP switch status write failed: " + error.Message); }
    }

    private static void WriteAtomic(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Json).Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n", new System.Text.UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
