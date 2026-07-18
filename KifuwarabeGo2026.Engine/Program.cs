namespace KifuwarabeGo2026.Engine;

using KifuwarabeGo2026.Domain;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// コンピュータ囲碁の思考エンジンの本体だぜ（＾～＾）
/// </summary>
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
    private static readonly string[] Commands =
    [
        "protocol_version", "name", "version", "known_command", "list_commands", "boardsize", "clear_board",
        "komi", "play", "genmove", "gui_options", "gui_getoption", "gui_setoption", "quit",
    ];
    private readonly Random _random = new();
    private GoBoard _board = new(19);
    private GoPoint? _koPoint;
    private decimal _komi = 6.5m;
    private RandomMoveKind _randomMove = RandomMoveKind.ChebyshevDistanceFromStar;

    public void Run(TextReader input, TextWriter output)
    {
        string? line;
        while ((line = input.ReadLine()) is not null)
        {
            var commandLine = line.Trim().TrimStart('\uFEFF');
            if (commandLine.Length == 0) continue;

            var quit = Execute(commandLine, out var response, out var error);
            WriteResponse(output, response, error);
            if (quit) return;
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
                response = "Kifuwarabe Star Random GTP";
                return false;

            // バージョン番号
            case "version":
                response = GetGtpVersion();
                return false;

            case "known_command":
                ExecuteKnownCommand(tokens, out response, out error);
                return false;
            case "list_commands":
                response = string.Join('\n', Commands);
                return false;
            case "gui_options":
                response = CreateGuiOptionsJson();
                return false;
            case "gui_getoption":
                ExecuteGuiGetOption(tokens, out response, out error);
                return false;
            case "gui_setoption":
                ExecuteGuiSetOption(tokens, out error);
                return false;

            case "boardsize":
                ExecuteBoardSize(tokens, out error);
                return false;
            case "clear_board":
                _board = new GoBoard(_board.Size);
                _koPoint = null;
                return false;
            case "komi":
                ExecuteKomi(tokens, out error);
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

        _board = new GoBoard(size);
        _koPoint = null;
    }

    private void ExecuteKomi(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length != 2 || !decimal.TryParse(tokens[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var komi))
        {
            error = "usage: komi number";
            return;
        }

        _komi = komi;
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

        if (!_board.TryPlaceStone(point.X, point.Y, color, _koPoint, out _, out var nextKoPoint))
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

        var renParse = _board.ParseRens();
        var legalMoves = new List<GoPoint>();
        for (var y = 0; y < _board.Size; y++)
        {
            for (var x = 0; x < _board.Size; x++)
            {
                var trial = _board.Clone();
                if (trial.TryPlaceStone(x, y, color, _koPoint, out _, out _) &&
                    !_board.IsEyeFor(renParse, x, y, color))
                {
                    legalMoves.Add(new GoPoint(x, y));
                }
            }
        }

        if (legalMoves.Count == 0)
        {
            _koPoint = null;
            response = "pass";
            return;
        }

        var move = _randomMove == RandomMoveKind.Normal
            ? legalMoves[_random.Next(legalMoves.Count)]
            : StarRegionRandomMoveSelector.Select(legalMoves, _board.Size, _random);
        _board.TryPlaceStone(move.X, move.Y, color, _koPoint, out _, out _koPoint);
        response = FormatVertex(move, _board.Size);
    }

    /// <summary>
    /// GTPコマンドへの対応状況を返します。
    /// </summary>
    private static void ExecuteKnownCommand(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 2)
        {
            error = "usage: known_command command_name";
            return;
        }

        response = Commands.Contains(tokens[1], StringComparer.OrdinalIgnoreCase) ? "true" : "false";
    }

    /// <summary>
    /// GUIが設定画面を構築するためのオプション定義を返します。
    /// </summary>
    private string CreateGuiOptionsJson() => JsonSerializer.Serialize(new
    {
        version = 1,
        options = new[]
        {
            new
            {
                id = "RandomMove",
                label = "RandomMove",
                type = "combo",
                @default = "ChebyshevDistanceFromStar",
                value = _randomMove.ToString(),
                vars = new[] { "Normal", "ChebyshevDistanceFromStar" },
            },
        },
    });

    /// <summary>
    /// GUIオプションの現在値を返します。
    /// </summary>
    private void ExecuteGuiGetOption(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 2 || !tokens[1].Equals("RandomMove", StringComparison.OrdinalIgnoreCase))
        {
            error = "unknown option: " + (tokens.Length > 1 ? tokens[1] : "");
            return;
        }

        response = _randomMove.ToString();
    }

    /// <summary>
    /// GUIから送られたオプション値を設定します。
    /// </summary>
    private void ExecuteGuiSetOption(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length != 3 || !tokens[1].Equals("RandomMove", StringComparison.OrdinalIgnoreCase))
        {
            error = "usage: gui_setoption RandomMove Normal|ChebyshevDistanceFromStar";
            return;
        }

        if (!Enum.TryParse(tokens[2], ignoreCase: true, out RandomMoveKind randomMove))
        {
            error = "option RandomMove must be Normal or ChebyshevDistanceFromStar";
            return;
        }

        _randomMove = randomMove;
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

    private static string GetGtpVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "0.0.0";
        }

        var fieldCount = version.Build < 0 ? 2 : version.Revision <= 0 ? 3 : 4;
        return version.ToString(fieldCount);
    }

    private static bool TryParseColor(string text, out GoStone stone)
    {
        if (text.Equals("black", StringComparison.OrdinalIgnoreCase) || text.Equals("b", StringComparison.OrdinalIgnoreCase))
        {
            stone = GoStone.Black;
            return true;
        }

        if (text.Equals("white", StringComparison.OrdinalIgnoreCase) || text.Equals("w", StringComparison.OrdinalIgnoreCase))
        {
            stone = GoStone.White;
            return true;
        }

        stone = GoStone.Empty;
        return false;
    }

    private static bool TryParseVertex(string text, int boardSize, out GoPoint point)
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

        point = new GoPoint(x, y);
        return true;
    }

    private static string FormatVertex(GoPoint point, int boardSize)
    {
        var column = (char)('A' + point.X);
        if (column >= 'I')
        {
            column++;
        }

        return $"{column}{boardSize - point.Y}";
    }

    private static bool IsPass(string text) => text.Equals("pass", StringComparison.OrdinalIgnoreCase);

    private enum RandomMoveKind
    {
        Normal,
        ChebyshevDistanceFromStar,
    }
}
