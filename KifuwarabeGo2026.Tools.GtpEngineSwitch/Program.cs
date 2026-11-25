using System.Diagnostics;

try
{
    var launch = SwitchControl.Start();
    using var child = launch.Process;
    Console.Error.WriteLine($"GTP engine slot {launch.Primary}: {launch.Executable}");
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
        if (completed == input && !child.HasExited) await input;
        await output;
        await exit;
        return child.ExitCode;
    }
    finally
    {
        if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
        launch.WriteStatus("stopped", child.ExitCode);
    }
}
catch (Exception error) when (SwitchControl.IsControlError(error))
{
    Console.Error.WriteLine($"GTP engine switch: {error.Message}");
    return 1;
}
