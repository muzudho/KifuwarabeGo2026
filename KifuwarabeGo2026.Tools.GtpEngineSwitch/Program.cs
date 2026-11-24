using System.Diagnostics;
using System.Text.Json;

try
{
    var configurationPath = Path.Combine(AppContext.BaseDirectory, "engine-switch.json");
    var configuration = JsonSerializer.Deserialize<EngineSwitchConfiguration>(
        File.ReadAllText(configurationPath), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    if (configuration is not { Primary: >= 1 and <= 3, Engines.Length: 3 } ||
        configuration.Engines[configuration.Primary - 1] is not { } engine ||
        string.IsNullOrWhiteSpace(engine.Path))
        throw new InvalidDataException("engine-switch.json must select a configured engine from slots 1 to 3.");

    var executable = Path.GetFullPath(engine.Path, AppContext.BaseDirectory);
    var startInfo = new ProcessStartInfo(executable)
    {
        WorkingDirectory = Path.GetDirectoryName(executable)!,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
    };
    foreach (var argument in engine.Arguments ?? [])
        startInfo.ArgumentList.Add(argument);

    using var child = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the selected GTP engine.");
    Console.Error.WriteLine($"GTP engine slot {configuration.Primary}: {executable}");
    var input = Task.Run(async () =>
    {
        using var destination = child.StandardInput.BaseStream;
        await Console.OpenStandardInput().CopyToAsync(destination);
    });
    var output = child.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput());
    var exit = child.WaitForExitAsync();
    try
    {
        var completed = await Task.WhenAny(input, output, exit);
        if (completed == input && !child.HasExited)
            await input;
        await output;
        await exit;
        return child.ExitCode;
    }
    finally
    {
        if (!child.HasExited)
            child.Kill(entireProcessTree: true);
    }
}
catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or
    JsonException or System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException)
{
    Console.Error.WriteLine($"GTP engine switch: {error.Message}");
    return 1;
}

internal sealed record EngineSwitchConfiguration(int Primary, EngineSlot?[] Engines);
internal sealed record EngineSlot(string Path, string[]? Arguments);
