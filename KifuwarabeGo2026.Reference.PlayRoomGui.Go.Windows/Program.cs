using System.Text.Json;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

if (args.SequenceEqual(new[] { "--stdio" }) || args.SequenceEqual(new[] { "--stdio-contract-smoke" }))
    return GoStdioHost.Run(args[0] == "--stdio-contract-smoke");

var result = GoPlayRoomHostStartup.Load(args);
var output = JsonSerializer.Serialize(new
{
    ready = result.IsReady,
    code = result.Code,
    message = result.Message,
    requestId = result.Plan?.RequestId,
    roomTypeId = result.Plan?.RoomTypeId,
    boardSize = result.Plan?.BoardSize,
});
if (!result.IsReady || result.Plan is null)
{
    Console.Error.WriteLine(output);
    return result.ExitCode;
}

Console.Out.WriteLine(output);
Console.Out.Flush();
if (args.Length == 3 && args[2] == "--contract-smoke-fail-after-ready")
{
    Console.Error.WriteLine("contract-smoke-failure-after-ready");
    return GoPlayRoomHostExitCodes.ContractSmokeFailed;
}
if (args.Length == 3 && args[2] == "--contract-smoke")
    return GoPlayRoomHostExitCodes.Success;
using var game = new GoInitialBoardGame(result.Plan);
game.Run();
return GoPlayRoomHostExitCodes.Success;
