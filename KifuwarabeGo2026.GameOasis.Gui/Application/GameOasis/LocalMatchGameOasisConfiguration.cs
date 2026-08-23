namespace KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Linq;
using System.Text.Json;

/// <summary>
/// 現行ローカル対局の開始局面を、通常囲碁Protocol Sの設定文書へ変換します。
/// 対局進行後の盤面同期には使用しません。
/// </summary>
public static class LocalMatchGameOasisConfiguration
{
    public static ContractDocument Create(LocalMatchInitialPosition initialPosition, decimal komi, TimeSpan mainTime)
    {
        ArgumentNullException.ThrowIfNull(initialPosition);
        if (initialPosition.BoardSize is not (9 or 13 or 19))
            throw new ArgumentOutOfRangeException(nameof(initialPosition), "Board size must be 9, 13, or 19.");
        if (komi is < -100m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(komi), komi, "Komi must be between -100 and 100.");
        if (mainTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(mainTime), mainTime, "Main time cannot be negative.");

        var document = new ConfigurationDocument(
            1,
            initialPosition.BoardSize,
            komi,
            "chinese-area",
            FormatStone(initialPosition.StartingTurn),
            initialPosition.SetupStones
                .Select(stone => new SetupStoneDocument(
                    stone.Point.X,
                    stone.Point.Y,
                    FormatStone(stone.Stone)))
                .ToArray(),
            checked((long)mainTime.TotalMilliseconds));

        return new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Go + ".configuration.v1",
            JsonSerializer.Serialize(document, JsonOptions));
    }

    private static string FormatStone(GoStone stone) => stone switch
    {
        GoStone.Black => "black",
        GoStone.White => "white",
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "A play-space turn or setup stone must be black or white."),
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record ConfigurationDocument(
        int Version,
        int BoardSize,
        decimal Komi,
        string Ruleset,
        string StartingPlayer,
        SetupStoneDocument[] SetupStones,
        long MainTimeMilliseconds);

    private sealed record SetupStoneDocument(int X, int Y, string Color);
}
