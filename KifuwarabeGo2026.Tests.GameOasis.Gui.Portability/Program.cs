namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;

internal static class Program
{
    private static int Main()
    {
        try
        {
            PortabilityChecks.Run();
            PlayRoomLaunchChecks.Run();
            FormalAdapterBaselineChecks.Run();
            Console.WriteLine(
                "PASS: Portability checks and GTP, CGOS, SGF pre-migration baseline vectors passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.Message}");
            return 1;
        }
    }
}
