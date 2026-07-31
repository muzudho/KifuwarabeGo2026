namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            PortabilityChecks.Run();
            Console.WriteLine(
                "PASS: Core, Match, and portable platform composition are free of Windows-only dependencies.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.Message}");
            return 1;
        }
    }
}
