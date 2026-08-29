namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>盤面レンズと表示用の連解析結果を、盤面ハッシュごとにキャッシュします。</summary>
public sealed partial class GoAppSession
{
    private GoRenParseResult? _cachedRenParseResult;
    private int _cachedRenParseBoardSize;
    private ulong _cachedRenParseHash;
    private const int RenBoardLensCount = 4;
    private const int MeasureBoardLensCount = 8;
    private const int BoardLensStepCycleLength = 28;
    private int _boardLensStep;
    private int _boardLensFamily;

    public RenParseDisplayMode RenParseDisplayMode { get; private set; }
    public bool IsRenParseDisplayEnabled => RenParseDisplayMode != RenParseDisplayMode.Off;
    public bool IsMeasureBoardLens => RenParseDisplayMode is
        RenParseDisplayMode.RenArea or RenParseDisplayMode.BoundaryCount or
        RenParseDisplayMode.BoundaryEmptyCount or RenParseDisplayMode.BoundaryOpponentCount or
        RenParseDisplayMode.AdjacentEmptyArea or RenParseDisplayMode.AdjacentOpponentArea or
        RenParseDisplayMode.Strong or RenParseDisplayMode.Nobi;
    public bool IsGlassesBoardLens => RenParseDisplayMode == RenParseDisplayMode.Glasses;

    public string BoardLensDisplayName => RenParseDisplayMode switch
    {
        RenParseDisplayMode.Off => "BOARD LENS OFF",
        RenParseDisplayMode.Overlay => "REN INDEX LENS  1/4",
        RenParseDisplayMode.Graph => "REN RECTANGLE LENS  2/4",
        RenParseDisplayMode.GraphStep2 => "REN NETWORK LENS - BASIC  3/4",
        RenParseDisplayMode.Eye => "REN NETWORK LENS - EYE MODE  4/4",
        RenParseDisplayMode.RenArea => "REN AREA LENS  1/8",
        RenParseDisplayMode.BoundaryCount => "BOUNDARY COUNT LENS  2/8",
        RenParseDisplayMode.BoundaryEmptyCount => "BOUNDARY EMPTY COUNT LENS  3/8",
        RenParseDisplayMode.BoundaryOpponentCount => "BOUNDARY OPPONENT COUNT LENS  4/8",
        RenParseDisplayMode.AdjacentEmptyArea => "ADJACENT EMPTY AREA LENS  5/8",
        RenParseDisplayMode.AdjacentOpponentArea => "ADJACENT OPPONENT AREA LENS  6/8",
        RenParseDisplayMode.Strong => "STRONG LENS  7/8",
        RenParseDisplayMode.Nobi => "NOBI LENS  8/8",
        RenParseDisplayMode.Glasses => "CHIPPED SINGLE EYE GLASS SEED LENS  1/1",
        _ => RenParseDisplayMode.ToString().ToUpperInvariant(),
    };

    public string BoardLensGuide => RenParseDisplayMode switch
    {
        RenParseDisplayMode.Off => "[L] OPEN",
        _ => "[L] SYSTEM    [J]/[K] PREV/NEXT    [1] EXIT",
    };

    public string BoardLensAlias => RenParseDisplayMode == RenParseDisplayMode.BoundaryEmptyCount
        ? "(a.k.a. LIBERTY COUNT)"
        : "";

    public GoRenParseResult ParseRens()
    {
        if (_cachedRenParseResult is not null &&
            _cachedRenParseBoardSize == BoardSize &&
            _cachedRenParseHash == _board.CurrentHash)
        {
            return _cachedRenParseResult;
        }

        _cachedRenParseResult = _board.ParseRens();
        _cachedRenParseBoardSize = BoardSize;
        _cachedRenParseHash = _board.CurrentHash;
        return _cachedRenParseResult;
    }

    public void ToggleRenParseDisplay()
    {
        if (RenParseDisplayMode == RenParseDisplayMode.Off)
        {
            _boardLensFamily = 0;
            _boardLensStep = 0;
        }
        else if (_boardLensFamily < 2)
        {
            _boardLensFamily++;
            _boardLensStep = 0;
        }
        else
        {
            RenParseDisplayMode = RenParseDisplayMode.Off;
            return;
        }

        ApplyBoardLensStep();
    }

    public bool TrySwitchBoardLensFamily()
    {
        if (RenParseDisplayMode == RenParseDisplayMode.Off)
            return false;

        _boardLensFamily = (_boardLensFamily + 1) % 3;
        ApplyBoardLensStep();
        return true;
    }

    public bool TryStepBoardLens(int direction)
    {
        if (RenParseDisplayMode == RenParseDisplayMode.Off)
            return false;

        var count = _boardLensFamily == 1 ? MeasureBoardLensCount : RenBoardLensCount;
        _boardLensStep = (_boardLensStep + direction % count + count) % count;
        ApplyBoardLensStep();
        return true;
    }

    public bool TryDeactivateBoardLens()
    {
        if (RenParseDisplayMode == RenParseDisplayMode.Off)
            return false;

        RenParseDisplayMode = RenParseDisplayMode.Off;
        return true;
    }

    private void ApplyBoardLensStep()
    {
        RenParseDisplayMode = _boardLensFamily == 1
            ? (_boardLensStep % MeasureBoardLensCount) switch
            {
                0 => RenParseDisplayMode.RenArea,
                1 => RenParseDisplayMode.BoundaryCount,
                2 => RenParseDisplayMode.BoundaryEmptyCount,
                3 => RenParseDisplayMode.BoundaryOpponentCount,
                4 => RenParseDisplayMode.AdjacentEmptyArea,
                5 => RenParseDisplayMode.AdjacentOpponentArea,
                6 => RenParseDisplayMode.Strong,
                _ => RenParseDisplayMode.Nobi,
            }
            : _boardLensFamily == 2
                ? RenParseDisplayMode.Glasses
                : (_boardLensStep % RenBoardLensCount) switch
                {
                    0 => RenParseDisplayMode.Overlay,
                    1 => RenParseDisplayMode.Graph,
                    2 => RenParseDisplayMode.GraphStep2,
                    _ => RenParseDisplayMode.Eye,
                };
    }
}
