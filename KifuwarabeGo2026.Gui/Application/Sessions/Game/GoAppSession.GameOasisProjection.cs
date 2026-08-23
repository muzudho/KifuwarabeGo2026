namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Linq;
using System.Text.Json;

/// <summary>Protocol Gの共通盤面を、移行中の現行描画モデルへ一方向に投影します。</summary>
public sealed partial class GoAppSession
{
    private bool _isGameOasisProjectedLocalGame;
    private bool _isGameOasisLocalGame;
    private int _gameOasisProjectedMoveCount;

    public bool IsGameOasisProjectedLocalGame => _isGameOasisProjectedLocalGame;
    public bool IsGameOasisLocalGame => _isGameOasisLocalGame;

    /// <summary>
    /// 現在のローカル対局をGame Oasisの表示投影へ切り替えます。
    /// 呼び出し後はProtocol Sが唯一のゲーム状態となり、旧着手APIは受け付けません。
    /// </summary>
    public void ApplyGameOasisBoardProjection(GuiBoardView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (CurrentMode.Kind != GoAppModeKind.Playing)
            throw new InvalidOperationException("A Game Oasis board can be projected only while a local match is playing.");
        if (view.PlaySpaceTypeId.Value != GameOasisOfficialNames.Go)
            throw new ArgumentException("Only the official Go play-space can be projected onto the current local-match screen.", nameof(view));
        if (view.BoardSize != BoardSize)
            throw new ArgumentException($"The projected board size {view.BoardSize} does not match the local board size {BoardSize}.", nameof(view));

        var board = new GoBoard(view.BoardSize);
        foreach (var point in view.Black)
            SetProjectionStone(board, point, GoStone.Black);
        foreach (var point in view.White)
            SetProjectionStone(board, point, GoStone.White);

        var firstProjection = !_isGameOasisProjectedLocalGame;
        _matchSession = null;
        _isGameOasisLocalGame = true;
        _isGameOasisProjectedLocalGame = true;
        _board = board;
        CurrentTurn = view.NextToPlay switch
        {
            "black" => GoStone.Black,
            "white" => GoStone.White,
            _ => throw new ArgumentException($"The projected next player '{view.NextToPlay}' is invalid.", nameof(view)),
        };
        BlackAgehama = view.BlackCaptures;
        WhiteAgehama = view.WhiteCaptures;
        KoPoint = view.KoPoint is { } ko ? new GoPoint(ko.X, ko.Y) : null;
        ApplyGameOasisClockProjection(view);
        ApplyGameOasisRecordProjection(view, firstProjection);
        ResetPositionHistory();
        if (view.IsTerminal)
            ApplyGameOasisOutcome(view.Outcome);
    }

    private void ApplyGameOasisRecordProjection(GuiBoardView view, bool firstProjection)
    {
        if (firstProjection)
        {
            CurrentGameRecord.SetupStones.Clear();
            CurrentGameRecord.SetupStones.AddRange(view.SetupBlack.Select(point =>
                new GoGameSetupStone(GoStone.Black, new GoPoint(point.X, point.Y))));
            CurrentGameRecord.SetupStones.AddRange(view.SetupWhite.Select(point =>
                new GoGameSetupStone(GoStone.White, new GoPoint(point.X, point.Y))));
            CurrentGameRecord.Moves.Clear();
            _gameOasisProjectedMoveCount = 0;
        }
        if (view.MoveHistory.Count < _gameOasisProjectedMoveCount)
            throw new InvalidOperationException("The Game Oasis move history cannot shrink during an active local match.");
        for (var index = 0; index < _gameOasisProjectedMoveCount; index++)
        {
            var projected = view.MoveHistory[index];
            var recorded = CurrentGameRecord.Moves[index];
            var projectedStone = projected.Player == "black" ? GoStone.Black : GoStone.White;
            var samePoint = projected.Type == "pass"
                ? recorded.Point is null
                : projected.Point is { } point && recorded.Point == new GoPoint(point.X, point.Y);
            if (recorded.Stone != projectedStone || !samePoint || recorded.TimeLeftAfterMove != projected.TimeLeftAfterMove)
                throw new InvalidOperationException($"The Game Oasis move history changed at index {index}.");
        }

        for (var index = _gameOasisProjectedMoveCount; index < view.MoveHistory.Count; index++)
        {
            var move = view.MoveHistory[index];
            var stone = move.Player == "black" ? GoStone.Black : GoStone.White;
            var point = move.Type == "play" && move.Point is { } played
                ? new GoPoint(played.X, played.Y)
                : (GoPoint?)null;
            CurrentGameRecord.Moves.Add(new GoGameMove(
                stone,
                point,
                timeLeftAfterMove: move.TimeLeftAfterMove));
        }
        _gameOasisProjectedMoveCount = view.MoveHistory.Count;
        PlayedMoveCount = _gameOasisProjectedMoveCount;
    }

    private void ApplyGameOasisClockProjection(GuiBoardView view)
    {
        if (view.MainTime is not { } mainTime) return;
        if (mainTime != MainTime)
            throw new InvalidOperationException($"The Game Oasis main time {mainTime} does not match the local match main time {MainTime}.");
        if (view.BlackTimeLeft is { } blackLeft)
        {
            if (blackLeft > mainTime) throw new InvalidOperationException("The Game Oasis black remaining time exceeds the main time.");
            BlackElapsedTime = mainTime - blackLeft;
        }
        if (view.WhiteTimeLeft is { } whiteLeft)
        {
            if (whiteLeft > mainTime) throw new InvalidOperationException("The Game Oasis white remaining time exceeds the main time.");
            WhiteElapsedTime = mainTime - whiteLeft;
        }
        BlackUsedTime = BlackElapsedTime;
        WhiteUsedTime = WhiteElapsedTime;
    }

    private void ApplyGameOasisOutcome(ContractDocument? outcome)
    {
        Winner = null;
        GameOverReason = "GAME OASIS MATCH COMPLETED";
        CurrentGameRecord.Result = "0";
        if (outcome is { MediaType: "application/json" } &&
            outcome.SchemaId == GameOasisOfficialNames.Go + ".outcome.v1")
        {
            try
            {
                using var json = JsonDocument.Parse(outcome.Content);
                var root = json.RootElement;
                var kind = root.GetProperty("kind").GetString();
                var winner = root.TryGetProperty("winner", out var winnerElement) && winnerElement.ValueKind == JsonValueKind.String
                    ? winnerElement.GetString()
                    : null;
                var reason = root.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : null;
                Winner = winner switch
                {
                    "black" => GoStone.Black,
                    "white" => GoStone.White,
                    _ => null,
                };
                GameOverReason = string.IsNullOrWhiteSpace(reason) ? "GAME OASIS MATCH COMPLETED" : reason.ToUpperInvariant();
                CurrentGameRecord.Result = kind == "winner" && Winner is { } winningStone
                    ? winningStone == GoStone.Black ? "B+GameOasis" : "W+GameOasis"
                    : "0";
            }
            catch (JsonException)
            {
                // The common terminal state remains displayable even if an optional outcome is malformed.
            }
        }
        ChangeMode(GoAppModeKind.GameOver);
    }

    private static void SetProjectionStone(GoBoard board, GuiBoardPoint point, GoStone stone)
    {
        if (!board.TrySetSetupStone(point.X, point.Y, stone))
            throw new ArgumentException($"The projected point ({point.X},{point.Y}) is invalid or occupied.");
    }
}
