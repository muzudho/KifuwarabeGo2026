# Go window standard I/O capability v1

`--stdio` starts the window through the Play Room JSON Lines v1 envelope. It is an opt-in route; `--launch-request <file>` retains the existing local match behavior. The stdio route does not start GTP players or adjudicate moves.

## Discovery and lifecycle

Send `describe` with `{}` parameters before opening. The result advertises `roomTypes: ["match"]` and capabilities `go-display-state.v1`, `poll-input-events.v1`. Other Play Room v1 hosts need not support these capabilities. Discover them before sending the extension methods.

1. `open`: a public `PlayRoomLaunchRequest` for Go/Match. Configuration uses the existing Go configuration schema (version, boardSize, komi, ruleset, startingPlayer, setupStones). `initialPosition` must be null; player process connections are rejected. Setup stones belong in configuration. Ready is returned after graphics content initialization, not just after validating the request. Allow up to 35 seconds for startup.
2. `updateState`: `MatchStateUpdate` supplies sessionId, revision and a display-state document. The returned `MatchViewState` acknowledges the immutable projection accepted by the display; it does not guarantee a frame has already been presented.
3. `readEvents`: `{ "sessionId": "..." }` returns `{ "sessionId": "...", "events": [], "closed": false }` immediately. Poll about every 100 ms. This method drains events; there is one reader and no replay or reconnect support in v1.
4. `complete`: `MatchCompletionCommand` validates the final display document, returns `Finished` and closes the window. `goodbye` returns `Closed` and closes it instead.

Closing the window or pressing Esc adds a `closed` event with a `MatchCompletion` and `closed: true`. The host exits normally after returning that polling response. It waits at most 10 seconds after window closure for the caller to collect the event. EOF on stdin also closes the window. A caller must handle EOF, abnormal exit and timeouts even when no completion arrives.

Stdout is reserved for responses; stderr carries diagnostics. No unsolicited stdout messages are emitted. `submitAction` is intentionally not an input-injection API in this window host: user actions originate in the window and are returned by `readEvents`.

## Display document

`mediaType` is `application/json`; `schemaId` is `io.github.muzudho.kifuwarabego2026.games.go.display-state.v1`. `content` is a **string containing** JSON:

```json
{"version":1,"boardSize":9,"currentTurn":"black","stones":[{"x":2,"y":3,"color":"white"}],"gameOver":false}
```

Board size must match the launch (9, 13 or 19). Coordinates start at zero at the top left. Stone colors and currentTurn are `black` or `white`. Duplicate or out-of-bounds intersections are rejected. `gameOver` is optional and defaults to false. This is a minimal display projection; clocks, captures, last move and outcome decorations are not yet exposed.

Revision starts at 0 or greater and strictly increases for every accepted projection, including rejection of a pending action with an unchanged board. It is a **display revision**, not necessarily the rule Engine's revision. Invalid documents do not consume a revision or release pending input.

## Input events

Click emits PlayPoint (kind 0), P emits Pass (1), R emits Resign (2). An action event has the shape:

```json
{"eventId":1,"type":"action","revision":0,"action":{"sessionId":"...","actionId":"...","playerRoleId":"black","kind":0,"x":2,"y":3},"completion":null}
```

Only one input is accepted until an authoritative `updateState` arrives. No input is accepted before the first update or after a terminal projection. Input does not change stones, turn, legality, or game outcome. The external progression component accepts/rejects the action and sends the next display projection. It may use Protocol G's SubmitAction/GetSnapshot through its own adapter; this presentation transport does not duplicate Concierge session ownership.

Event IDs increase within the session. `closed` events use the same eventId/type/revision envelope, a null action, and a completion object. The shared .NET DTOs are `MatchInputEvent` and `MatchInputEvents` in public Contracts.

## Reference caller and tests

The official Windows Lobby now uses this route through [Reference.MatchRunner.Go](../KifuwarabeGo2026.Reference.MatchRunner.Go/README.md). That runner connects Protocol G/S authority and GTP players, advances legal moves, scores two passes, handles resignation, and returns the result to the Lobby. The Python caller below remains a transport-only demonstration.

```powershell
python Samples/External.PlayRoomClient/window_client.py -- dotnet KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows/bin/Release/net8.0/KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.dll --stdio
```

The caller logs clicks/pass/resign and returns the unchanged board with a newer revision. It is a transport demonstration that rejects inputs, not a Go referee. Close the window to finish. Add `--smoke` before `--` to update a stone and automatically complete the session.

For non-graphical process checks use host flag `--stdio-contract-smoke` with caller `--smoke`. It uses the same transport and session but skips window creation, and advertises `headless: true`. This cannot prove actual rendering or mouse input. `--stdio` advertises `headless: false`.
