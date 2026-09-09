# Official Go match runner

`GoStdioMatchLauncher` implements `IPlayRoomProcessLauncher`. The Windows Lobby uses it for normal Go Match launches. Its command factory supplies the complete command, including `--stdio` for the official window.

The runner opens an authoritative Concierge session through Protocol G, backed by the existing Go Protocol S implementation. It discovers the window's public capabilities, opens it, sends display projections, polls inputs, submits actions to Protocol G, and returns the final Match completion to the Lobby. Invalid or stale human input receives an unchanged projection with a new display revision. Two passes use the existing Chinese area scoring; resignation returns the opponent as winner.

GTP players run on the progression side. `GoLocalMatchGtpController` is shared with the existing file-launch host. Engine options and connection details stay on this side; the display receives participant metadata and a self-contained configuration. Configuration setup stones are the starting position; the redundant SGF launch document is not used to replay move history. The window does not read Lobby settings or start player engines.

Closing the window closes the Concierge session and player processes. Cancellation, malformed communication, unexpected exits, and timeouts also clean up the session and children. Lobby disposal cancels an active match. Terminal games complete the window protocol, close the window, and show the result in the Lobby.

The runner has no MonoGame or Lobby GUI dependency. The polling transport lives in `PlayRoomGui.JsonLines`; the display protocol is documented in [STDIO.md](../KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows/STDIO.md).

## Verification

```powershell
dotnet run --project KifuwarabeGo2026.Tests.Reference.MatchRunner.Go -c Release
dotnet run --project KifuwarabeGo2026.Tests.Reference.MatchRunner.Go -c Release --no-build -- --real-host artifacts/stdio-match-host/KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.dll
dotnet run --project KifuwarabeGo2026.Tests.Reference.MatchRunner.Go -c Release --no-build -- --real-window artifacts/stdio-match-host/KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.dll
```

The first command uses an independent scripted protocol host to exercise legal play, capture, illegal play, stale/wrong-role input, resignation, scored passes, closure, malformed IDs, crashes, a host that refuses to exit, cancellation, GTP players, and restart. The real-host modes use two deterministic GTP players that pass, with the published official host; only `--real-window` creates a graphics window. They do not simulate mouse or keyboard input.

The minimal display protocol does not expose clocks, capture counters, or detailed outcome decoration. Clock behavior remains that of the existing Protocol S implementation (remaining-time observation, without automatic timeout adjudication). Board Editor, Review, Ponnuki and CGOS migration remain separate work.
