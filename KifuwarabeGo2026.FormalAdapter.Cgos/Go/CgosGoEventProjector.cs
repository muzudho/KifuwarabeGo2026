namespace KifuwarabeGo2026.FormalAdapter.Cgos.Go;

using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;

public enum CgosGoColor { Black, White }

public sealed record CgosGoVertex(string Text, int? X, int? Y)
{
    public bool IsPass => X is null || Y is null;
}

public abstract record CgosGoEvent;

public sealed record CgosGoSetup(
    int GameId,
    int BoardSize,
    decimal Komi,
    long MainTimeMilliseconds,
    string WhitePlayer,
    string BlackPlayer,
    IReadOnlyList<CgosGoMove> MoveHistory) : CgosGoEvent;

public sealed record CgosGoMove(
    CgosGoColor Color,
    CgosGoVertex Vertex,
    long? TimeLeftMilliseconds,
    string? AnalysisJson,
    bool IsGenerated) : CgosGoEvent;

public sealed record CgosGoGameOver(string Result) : CgosGoEvent;

/// <summary>Projects CGOS notifications into protocol-independent Go game events.</summary>
public sealed class CgosGoEventProjector
{
    private int _boardSize;

    public bool TryProject(CgosNotification notification, out CgosGoEvent? gameEvent)
    {
        gameEvent = notification switch
        {
            CgosSetupNotification setup => ProjectSetup(setup),
            CgosPlayNotification play when _boardSize > 0 => ProjectMove(play),
            CgosGameOverNotification gameOver => new CgosGoGameOver(
                string.IsNullOrWhiteSpace(gameOver.Result) ? "GAME OVER" : gameOver.Result),
            _ => null,
        };
        return gameEvent is not null;
    }

    public void Reset() => _boardSize = 0;

    private CgosGoSetup? ProjectSetup(CgosSetupNotification setup)
    {
        if (setup.BoardSize is not (9 or 13 or 19) || setup.GameId < 0 || setup.MainTimeMilliseconds < 0) return null;
        var moves = new List<CgosGoMove>(setup.MoveHistory.Count);
        foreach (var move in setup.MoveHistory)
        {
            if (!TryParseColor(move.Color, out var color) || !TryParseVertex(move.Vertex, setup.BoardSize, out var vertex))
                return null;
            moves.Add(new CgosGoMove(color, vertex, move.TimeLeftMilliseconds, null, false));
        }
        _boardSize = setup.BoardSize;
        return new CgosGoSetup(
            setup.GameId, setup.BoardSize, setup.Komi, setup.MainTimeMilliseconds,
            setup.WhitePlayer, setup.BlackPlayer, moves);
    }

    private CgosGoMove? ProjectMove(CgosPlayNotification play)
    {
        if (!TryParseColor(play.Color, out var color) || !TryParseVertex(play.Vertex, _boardSize, out var vertex)) return null;
        return new CgosGoMove(color, vertex, play.TimeLeftMilliseconds, play.AnalysisJson, play.IsGenerated);
    }

    private static bool TryParseColor(string text, out CgosGoColor color)
    {
        if (text.Equals("b", StringComparison.OrdinalIgnoreCase) || text.Equals("black", StringComparison.OrdinalIgnoreCase))
        { color = CgosGoColor.Black; return true; }
        if (text.Equals("w", StringComparison.OrdinalIgnoreCase) || text.Equals("white", StringComparison.OrdinalIgnoreCase))
        { color = CgosGoColor.White; return true; }
        color = default;
        return false;
    }

    private static bool TryParseVertex(string text, int boardSize, out CgosGoVertex vertex)
    {
        if (text.Equals("pass", StringComparison.OrdinalIgnoreCase))
        { vertex = new CgosGoVertex("pass", null, null); return true; }
        vertex = new CgosGoVertex(text, null, null);
        if (text.Length < 2) return false;
        var column = char.ToUpperInvariant(text[0]);
        if (column == 'I' || column < 'A' || column > 'Z') return false;
        if (column > 'I') column--;
        var x = column - 'A';
        if (!int.TryParse(text[1..], out var row)) return false;
        var y = boardSize - row;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize) return false;
        vertex = new CgosGoVertex(text.ToUpperInvariant(), x, y);
        return true;
    }
}
