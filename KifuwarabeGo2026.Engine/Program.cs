namespace KifuwarabeGo2026.Engine;

internal static class Program
{
    public static void Main()
    {
        var engine = new GtpEngine();
        engine.Run(Console.In, Console.Out);
    }
}

internal sealed class GtpEngine
{
    private readonly Random _random = new();
    private EngineBoard _board = new(19);
    private Point? _koPoint;

    public void Run(TextReader input, TextWriter output)
    {
        string? line;
        while ((line = input.ReadLine()) is not null)
        {
            var commandLine = line.Trim().TrimStart('\uFEFF');
            if (commandLine.Length == 0)
            {
                continue;
            }

            var quit = Execute(commandLine, out var response, out var error);
            WriteResponse(output, response, error);
            if (quit)
            {
                return;
            }
        }
    }

    private bool Execute(string commandLine, out string response, out string? error)
    {
        response = "";
        error = null;

        var tokens = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = tokens[0].ToLowerInvariant();
        switch (command)
        {
            case "protocol_version":
                response = "2";
                return false;
            case "name":
                response = "Kifuwarabe Random GTP";
                return false;
            case "version":
                response = "0.1.0";
                return false;
            case "boardsize":
                ExecuteBoardSize(tokens, out error);
                return false;
            case "clear_board":
                _board = new EngineBoard(_board.Size);
                _koPoint = null;
                return false;
            case "play":
                ExecutePlay(tokens, out error);
                return false;
            case "genmove":
                ExecuteGenMove(tokens, out response, out error);
                return false;
            case "quit":
                return true;
            default:
                error = $"unknown command: {tokens[0]}";
                return false;
        }
    }

    private void ExecuteBoardSize(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length != 2 || !int.TryParse(tokens[1], out var size) || size is not (9 or 13 or 19))
        {
            error = "boardsize must be 9, 13, or 19";
            return;
        }

        _board = new EngineBoard(size);
        _koPoint = null;
    }

    private void ExecutePlay(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length != 3 || !TryParseColor(tokens[1], out var color))
        {
            error = "usage: play black|white vertex";
            return;
        }

        if (IsPass(tokens[2]))
        {
            _koPoint = null;
            return;
        }

        if (!TryParseVertex(tokens[2], _board.Size, out var point))
        {
            error = "invalid vertex";
            return;
        }

        if (!_board.TryPlaceStone(point.X, point.Y, color, _koPoint, out var nextKoPoint))
        {
            error = "illegal move";
            return;
        }

        _koPoint = nextKoPoint;
    }

    private void ExecuteGenMove(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 2 || !TryParseColor(tokens[1], out var color))
        {
            error = "usage: genmove black|white";
            return;
        }

        var legalMoves = new List<Point>();
        for (var y = 0; y < _board.Size; y++)
        {
            for (var x = 0; x < _board.Size; x++)
            {
                var trial = _board.Clone();
                if (trial.TryPlaceStone(x, y, color, _koPoint, out _))
                {
                    legalMoves.Add(new Point(x, y));
                }
            }
        }

        if (legalMoves.Count == 0)
        {
            _koPoint = null;
            response = "pass";
            return;
        }

        var move = legalMoves[_random.Next(legalMoves.Count)];
        _board.TryPlaceStone(move.X, move.Y, color, _koPoint, out _koPoint);
        response = FormatVertex(move, _board.Size);
    }

    private static void WriteResponse(TextWriter output, string response, string? error)
    {
        output.Write(error is null ? "=" : $"? {error}");
        if (!string.IsNullOrWhiteSpace(response))
        {
            output.Write($" {response}");
        }

        output.WriteLine();
        output.WriteLine();
        output.Flush();
    }

    private static bool TryParseColor(string text, out Stone stone)
    {
        if (text.Equals("black", StringComparison.OrdinalIgnoreCase) || text.Equals("b", StringComparison.OrdinalIgnoreCase))
        {
            stone = Stone.Black;
            return true;
        }

        if (text.Equals("white", StringComparison.OrdinalIgnoreCase) || text.Equals("w", StringComparison.OrdinalIgnoreCase))
        {
            stone = Stone.White;
            return true;
        }

        stone = Stone.Empty;
        return false;
    }

    private static bool TryParseVertex(string text, int boardSize, out Point point)
    {
        point = default;
        if (text.Length < 2 || IsPass(text))
        {
            return false;
        }

        var column = char.ToUpperInvariant(text[0]);
        if (column >= 'I')
        {
            column--;
        }

        var x = column - 'A';
        if (!int.TryParse(text[1..], out var row))
        {
            return false;
        }

        var y = boardSize - row;
        if (x < 0 || x >= boardSize || y < 0 || y >= boardSize)
        {
            return false;
        }

        point = new Point(x, y);
        return true;
    }

    private static string FormatVertex(Point point, int boardSize)
    {
        var column = (char)('A' + point.X);
        if (column >= 'I')
        {
            column++;
        }

        return $"{column}{boardSize - point.Y}";
    }

    private static bool IsPass(string text) => text.Equals("pass", StringComparison.OrdinalIgnoreCase);
}

internal sealed class EngineBoard
{
    private readonly Stone[,] _stones;

    public EngineBoard(int size)
    {
        if (size is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be 9, 13, or 19.");
        }

        Size = size;
        _stones = new Stone[size, size];
    }

    private EngineBoard(EngineBoard source)
    {
        Size = source.Size;
        _stones = new Stone[Size, Size];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                _stones[x, y] = source._stones[x, y];
            }
        }
    }

    public int Size { get; }

    public EngineBoard Clone() => new(this);

    public bool TryPlaceStone(int x, int y, Stone stone, Point? forbiddenKoPoint, out Point? koPoint)
    {
        koPoint = null;
        if (stone == Stone.Empty || !IsOnBoard(x, y) || _stones[x, y] != Stone.Empty)
        {
            return false;
        }

        if (forbiddenKoPoint is { } ko && ko.X == x && ko.Y == y)
        {
            return false;
        }

        SetStone(x, y, stone);
        var opponent = stone == Stone.Black ? Stone.White : Stone.Black;
        var removedStones = new List<Point>();
        foreach (var neighbor in EnumerateNeighbors(x, y))
        {
            if (_stones[neighbor.X, neighbor.Y] != opponent)
            {
                continue;
            }

            var ren = CollectRen(neighbor.X, neighbor.Y);
            if (!HasLiberty(ren))
            {
                RemoveRen(ren, removedStones);
            }
        }

        var placedRen = CollectRen(x, y);
        if (!HasLiberty(placedRen))
        {
            SetStone(x, y, Stone.Empty);
            foreach (var point in removedStones)
            {
                SetStone(point.X, point.Y, opponent);
            }

            return false;
        }

        if (removedStones.Count == 1 && placedRen.Count == 1 && CountLiberties(placedRen) == 1)
        {
            koPoint = removedStones[0];
        }

        return true;
    }

    private bool IsOnBoard(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;

    private void SetStone(int x, int y, Stone stone) => _stones[x, y] = stone;

    private List<Point> CollectRen(int x, int y)
    {
        var color = _stones[x, y];
        var ren = new List<Point>();
        if (color == Stone.Empty)
        {
            return ren;
        }

        var visited = new bool[Size, Size];
        var queue = new Queue<Point>();
        visited[x, y] = true;
        queue.Enqueue(new Point(x, y));
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            ren.Add(point);
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (visited[neighbor.X, neighbor.Y] || _stones[neighbor.X, neighbor.Y] != color)
                {
                    continue;
                }

                visited[neighbor.X, neighbor.Y] = true;
                queue.Enqueue(neighbor);
            }
        }

        return ren;
    }

    private bool HasLiberty(List<Point> ren)
    {
        foreach (var point in ren)
        {
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (_stones[neighbor.X, neighbor.Y] == Stone.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int CountLiberties(List<Point> ren)
    {
        var liberties = new HashSet<Point>();
        foreach (var point in ren)
        {
            foreach (var neighbor in EnumerateNeighbors(point.X, point.Y))
            {
                if (_stones[neighbor.X, neighbor.Y] == Stone.Empty)
                {
                    liberties.Add(neighbor);
                }
            }
        }

        return liberties.Count;
    }

    private void RemoveRen(List<Point> ren, List<Point> removedStones)
    {
        foreach (var point in ren)
        {
            removedStones.Add(point);
            SetStone(point.X, point.Y, Stone.Empty);
        }
    }

    private IEnumerable<Point> EnumerateNeighbors(int x, int y)
    {
        if (x > 0)
        {
            yield return new Point(x - 1, y);
        }

        if (x < Size - 1)
        {
            yield return new Point(x + 1, y);
        }

        if (y > 0)
        {
            yield return new Point(x, y - 1);
        }

        if (y < Size - 1)
        {
            yield return new Point(x, y + 1);
        }
    }
}

internal enum Stone
{
    Empty,
    Black,
    White,
}

internal readonly record struct Point(int X, int Y);
