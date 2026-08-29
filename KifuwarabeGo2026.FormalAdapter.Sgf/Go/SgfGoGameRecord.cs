namespace KifuwarabeGo2026.FormalAdapter.Sgf.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>A GUI-independent projection of one main Go game from an SGF document.</summary>
public sealed class SgfGoGameRecord
{
    private int _boardSize = 19;

    public string GameName { get; set; } = "";
    public string RuleName { get; set; } = "";
    public string BlackPlayerName { get; set; } = "";
    public string WhitePlayerName { get; set; } = "";
    public string BlackRank { get; set; } = "";
    public string WhiteRank { get; set; } = "";
    public string PlayedDate { get; set; } = "";
    public string Result { get; set; } = "";
    public string Place { get; set; } = "";
    public string RootComment { get; set; } = "";

    public int BoardSize
    {
        get => _boardSize;
        set
        {
            if (value is not (9 or 13 or 19))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Board size must be 9, 13, or 19.");
            _boardSize = value;
        }
    }

    public decimal Komi { get; set; } = 6.5m;
    public TimeSpan TimeLimit { get; set; }
    public IList<SgfGoSetupStone> SetupStones { get; } = new List<SgfGoSetupStone>();
    public IList<SgfGoMove> Moves { get; } = new List<SgfGoMove>();
}

public readonly record struct SgfGoSetupStone(GoStone Stone, GoPoint Point);

/// <param name="AnalysisPropertyIdentifier">CC, KFW, or legacy KFA.</param>
public sealed record SgfGoMove(
    GoStone Stone,
    GoPoint? Point,
    string Comment = "",
    TimeSpan? TimeLeftAfterMove = null,
    string? AnalysisPropertyIdentifier = null,
    string? AnalysisJson = null);
