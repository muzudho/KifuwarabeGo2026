namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;
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
    public static ContractDocument Create(MatchSnapshot initialSnapshot, decimal komi, TimeSpan mainTime)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        if (initialSnapshot.Revision != 0 || initialSnapshot.Actions.Count != 0)
            throw new ArgumentException("Only an initial local-match snapshot can become a play-space configuration.", nameof(initialSnapshot));
        if (komi is < -100m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(komi), komi, "Komi must be between -100 and 100.");
        if (mainTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(mainTime), mainTime, "Main time cannot be negative.");

        var document = new ConfigurationDocument(
            1,
            initialSnapshot.BoardSize,
            komi,
            "chinese-area",
            FormatStone(initialSnapshot.CurrentTurn),
            initialSnapshot.SetupStones
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
