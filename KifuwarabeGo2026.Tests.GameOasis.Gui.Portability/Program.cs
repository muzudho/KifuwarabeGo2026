namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;
using System.IO;
using System.Text.Json;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--play-room-child-normal-exit")
        {
            var requestPath = args.Length == 3 && args[1] == "--launch-request" ? args[2] : "";
            var requestId = JsonDocument.Parse(File.ReadAllText(requestPath)).RootElement.GetProperty("requestId").GetString();
            Console.WriteLine(JsonSerializer.Serialize(new { ready = true, code = "ready", message = "test ready", requestId }));
            return 0;
        }
        if (args.Length > 0 && args[0] == "--play-room-child-invalid-ready")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ready = true, code = "ready", message = "wrong request", requestId = "wrong-request" }));
            return 0;
        }
        if (args.Length > 0 && args[0] == "--play-room-child-no-ready")
        {
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));
            return 0;
        }
        if (args.Length > 0 && args[0] == "--play-room-gtp-test-engine")
            return RunPlayRoomGtpTestEngine();
        if (args.Length > 0 && args[0] == "--play-room-child-fail-before-ready")
        {
            Console.Error.WriteLine("fixture failed before ready");
            return 23;
        }
        if (args.Length > 0 && args[0] == "--play-room-child-fail-after-ready")
        {
            var requestPath = args.Length == 3 && args[1] == "--launch-request" ? args[2] : "";
            var requestId = JsonDocument.Parse(File.ReadAllText(requestPath)).RootElement.GetProperty("requestId").GetString();
            Console.WriteLine(JsonSerializer.Serialize(new { ready = true, code = "ready", message = "test ready", requestId }));
            Console.Out.Flush();
            Console.Error.WriteLine("fixture failed after ready");
            return 24;
        }

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

    private static int RunPlayRoomGtpTestEngine()
    {
        while (Console.ReadLine() is { } command)
        {
            if (command == "quit")
            {
                Console.WriteLine("=\n");
                return 0;
            }
            Console.WriteLine(command.StartsWith("genmove ", StringComparison.Ordinal) ? "= D4\n" :
                command.StartsWith("known_command ", StringComparison.Ordinal) ? "= false\n" : "=\n");
        }
        return 0;
    }
}
