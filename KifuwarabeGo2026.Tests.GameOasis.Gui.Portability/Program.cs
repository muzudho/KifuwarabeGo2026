namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--play-room-child-normal-exit") return 0;

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
