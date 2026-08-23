namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            PortabilityChecks.Run();
            Console.WriteLine(
                "PASS: Core, Go play-space, Go foundation, GtpExtensions, and portable platform composition are free of Windows-only dependencies.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.Message}");
            return 1;
        }
    }
}
