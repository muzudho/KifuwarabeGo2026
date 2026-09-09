namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Windows;

using System;
using System.IO;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

internal static class GoStdioChecks
{
    public static void Run()
    {
        var plan = new GoPlayRoomLaunchPlan("stdio-test", "match", GoPlayRoomActivity.Playing,
            9, 6.5m, "chinese-area", GoStone.Black, [], TimeSpan.Zero, [], [], null);
        var room = new GoStdioSession(plan);
        Check(!room.SubmitInput(MatchActionKind.Pass), "Input must wait for authoritative state.");
        ContractDocument State(string stones = "[]") => new("application/json", GoStdioSession.StateSchema,
            "{\"version\":1,\"boardSize\":9,\"currentTurn\":\"black\",\"stones\":" + stones + "}");
        room.Update(new(room.SessionId, 0, State()));
        Check(room.SubmitInput(MatchActionKind.PlayPoint, 2, 3), "Click must enqueue input.");
        Check(room.View.GetStone(2, 3) == GoStone.Empty, "Input must not mutate authoritative state.");
        Check(!room.SubmitInput(MatchActionKind.Pass), "Only one input may await adjudication.");
        var events = room.ReadEvents(room.SessionId);
        Check(events.Events.Count == 1 && events.Events[0].Action?.Kind == MatchActionKind.PlayPoint &&
            events.Events[0].Action?.X == 2 && events.Events[0].Revision == 0, "Input must retain point and revision.");
        Check(room.ReadEvents(room.SessionId).Events.Count == 0,
            "Polling must drain events.");
        try { room.Update(new(room.SessionId, 0, State())); throw new Exception("Stale revision accepted."); }
        catch (InvalidDataException) { }
        try { room.Update(new(room.SessionId, 1, State("[{\"x\":99,\"y\":0,\"color\":\"black\"}]"))); throw new Exception("Invalid stone accepted."); }
        catch (InvalidDataException) { }
        room.Update(new(room.SessionId, 1, State("[{\"x\":2,\"y\":3,\"color\":\"black\"}]")));
        Check(room.View.GetStone(2, 3) == GoStone.Black, "Only updateState may change displayed stones.");
        Check(room.SubmitInput(MatchActionKind.Pass), "An update must release the pending input.");
        room.ReadEvents(room.SessionId);
        room.Update(new(room.SessionId, 2, State()));
        Check(room.SubmitInput(MatchActionKind.Resign), "Resignation must use the same event boundary.");
        room.Close(room.SessionId);
        Check(room.ShouldExit && !room.SubmitInput(MatchActionKind.Pass), "Closed rooms must reject input.");
        Check(room.ReadEvents(room.SessionId).Closed,
            "Window closure must be observable by polling.");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
