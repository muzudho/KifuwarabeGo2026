using System.Text.Json;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

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
if (result.IsReady)
    Console.Out.WriteLine(output);
else
    Console.Error.WriteLine(output);
return result.ExitCode;
