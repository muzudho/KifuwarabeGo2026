namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System.Collections.Generic;

/// <summary>局面に付随するアゲハマ、コウ、連続パス、勝敗とスーパーコウ判定を管理します。</summary>
public sealed partial class GoAppSession
{
    public int BlackAgehama { get; private set; }
    public int WhiteAgehama { get; private set; }
    public int BlackStoneCount => _board.CountStones(GoStone.Black);
    public int WhiteStoneCount => _board.CountStones(GoStone.White);
    public GoPoint? KoPoint { get; private set; }
    public int ConsecutivePasses { get; private set; }
    public string GameOverReason { get; private set; } = "";
    public GoStone? Winner { get; private set; }

    public GoStone GetStone(int x, int y) => _board.GetStone(x, y);

    public bool IsSuperKoPoint(int x, int y)
    {
        if (CurrentMode.Kind is not (GoAppModeKind.Playing or GoAppModeKind.VariationEditing))
            return false;

        var trialBoard = _board.Clone();
        return trialBoard.TryPlaceStone(x, y, CurrentTurn, KoPoint, out _, out _) &&
            _positionHashes.Contains(trialBoard.CurrentHash);
    }

    public bool IsNobiCandidate(int x, int y)
    {
        if (_board.GetStone(x, y) != GoStone.Empty ||
            (KoPoint is { } ko && ko.X == x && ko.Y == y) ||
            IsSuperKoPoint(x, y))
        {
            return false;
        }

        return !_board.IsEyeFor(x, y, CurrentTurn);
    }

    public IEnumerable<GoPoint> EnumerateSuperKoPoints()
    {
        if (CurrentMode.Kind is not (GoAppModeKind.Playing or GoAppModeKind.VariationEditing))
            yield break;

        for (var y = 0; y < BoardSize; y++)
        for (var x = 0; x < BoardSize; x++)
        {
            if (IsSuperKoPoint(x, y))
                yield return new GoPoint(x, y);
        }
    }
}
