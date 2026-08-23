namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Text.Json;

/// <summary>Protocol Gの共通盤面を、移行中の現行描画モデルへ一方向に投影します。</summary>
public sealed partial class GoAppSession
{
    private bool _isGameOasisProjectedLocalGame;

    public bool IsGameOasisProjectedLocalGame => _isGameOasisProjectedLocalGame;

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

        _matchSession = null;
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
        ResetPositionHistory();
        if (view.IsTerminal)
            ApplyGameOasisOutcome(view.Outcome);
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
