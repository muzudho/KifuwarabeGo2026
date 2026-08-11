namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>現在の盤面から表示・操作用の棋譜レコードを組み立てます。</summary>
public sealed partial class GoAppSession
{
    private GoGameRecord CreateGameRecordFromCurrentPosition()
    {
        var record = new GoGameRecord
        {
            GameName = "Kifuwarabe Go 2026",
            RuleName = RuleKind.ToString(),
            BlackPlayerName = GetLocalPlayerName(GoStone.Black),
            WhitePlayerName = GetLocalPlayerName(GoStone.White),
            BoardSize = BoardSize,
            Komi = Komi,
            TimeLimit = MainTime,
        };

        for (var y = 0; y < BoardSize; y++)
        for (var x = 0; x < BoardSize; x++)
        {
            var stone = _board.GetStone(x, y);
            if (stone != GoStone.Empty)
                record.SetupStones.Add(new GoGameSetupStone(stone, new GoPoint(x, y)));
        }

        return record;
    }

    private static void CopyGameRecordMetadata(GoGameRecord source, GoGameRecord destination)
    {
        destination.GameName = source.GameName;
        destination.RuleName = source.RuleName;
        destination.BlackPlayerName = source.BlackPlayerName;
        destination.WhitePlayerName = source.WhitePlayerName;
        destination.BlackRank = source.BlackRank;
        destination.WhiteRank = source.WhiteRank;
        destination.PlayedDate = source.PlayedDate;
        destination.Result = source.Result;
        destination.Place = source.Place;
        destination.Komi = source.Komi;
        destination.TimeLimit = source.TimeLimit;
    }
}
