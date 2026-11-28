namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;
using System.IO;
using System.Text.Json;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args is ["--externally-managed-options-fixture", var format])
            return RunExternallyManagedOptionsFixture(format == "modern");
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
            if (args is ["--externally-managed-options"])
            {
                PortabilityChecks.VerifyExternallyManagedEngineOptions();
                Console.WriteLine("PASS externally managed engine options: legacy and modern");
                return 0;
            }
            if (args is ["--optional-audio"])
            {
                var method = typeof(KifuwarabeGo2026.GameOasis.Gui.Game1).GetMethod("TryInitializeOptionalAudio",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
                bool disabled = false;
                object? result = method.Invoke(null, new object[] {
                    (Action)(() => throw new Microsoft.Xna.Framework.Audio.NoAudioHardwareException("No device: regression fixture")),
                    (Action)(() => disabled = true) });
                if (!disabled || result is not false) throw new Exception("Missing audio must continue silently.");
                disabled = false;
                result = method.Invoke(null, new object[] { (Action)(() => {}), (Action)(() => disabled = true) });
                if (disabled || result is not true) throw new Exception("Working audio must remain enabled.");
                try
                {
                    method.Invoke(null, new object[] { (Action)(() => throw new InvalidOperationException("unrelated")), (Action)(() => {}) });
                    throw new Exception("Unrelated initialization error was swallowed.");
                }
                catch (System.Reflection.TargetInvocationException error) when (error.InnerException is InvalidOperationException) { }
                Console.WriteLine("PASS optional audio: no-device fallback, working audio, unrelated errors preserved");
                return 0;
            }
            if (args is ["--engine-option-discovery", var executable])
            {
                PortabilityChecks.VerifyEngineOptionDiscovery(executable);
                Console.WriteLine("PASS common engine option discovery: direct/wrapped schema and fractional setting");
                return 0;
            }
            if (args is ["--engine-slot-options"])
            {
                PortabilityChecks.VerifyEngineSlotOptions();
                Console.WriteLine("PASS engine slot options: dropdown, entry storage, local/CGOS launch merge, default isolation");
                return 0;
            }
            if (args.Length == 1 && args[0] == "--lobby-portal")
            {
                LobbyPortalChecks.Run();
                Console.WriteLine("PASS: Lobby portal navigation, pagination and geometry checks.");
                return 0;
            }
            if (args.Length == 1 && args[0] == "--play-room-launch")
            {
                PlayRoomLaunchChecks.Run();
                Console.WriteLine("PASS: Play-room launch checks.");
                return 0;
            }
            PortabilityChecks.Run();
            LobbyPortalChecks.Run();
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

    private static int RunExternallyManagedOptionsFixture(bool modern)
    {
        while (Console.ReadLine() is { } command)
        {
            var response = command switch
            {
                "known_command kfw-options" => modern ? "false" : "true",
                "known_command kfw-describe-options" => modern ? "true" : "false",
                "known_command gui_options" => "false",
                "kfw-options" =>
                    """{"version":1,"options":[{"id":"Threads","label":"Threads","type":"spin","default":"1","value":"8","externallyManaged":true},{"id":"UseBook","label":"UseBook","type":"check","default":"false"}]}""",
                "kfw-describe-options play player" =>
                    """{"version":1,"app":"play","role":"player","options":[{"id":"Threads","label":"Threads","type":"integer","default":1,"externallyManaged":true},{"id":"UseBook","label":"UseBook","type":"boolean","default":false}]}""",
                "kfw-get-options play player" =>
                    """{"version":1,"app":"play","role":"player","values":{"Threads":8,"UseBook":true}}""",
                _ => "",
            };
            var attemptsManagedWrite = (command.StartsWith("kfw-set-option ", StringComparison.Ordinal) ||
                command.StartsWith("kfw-patch-options ", StringComparison.Ordinal)) && command.Contains("Threads", StringComparison.Ordinal);
            Console.WriteLine(attemptsManagedWrite ? "? externally managed\n" : $"= {response}\n");
            Console.Out.Flush();
            if (command == "quit") break;
        }
        return 0;
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
