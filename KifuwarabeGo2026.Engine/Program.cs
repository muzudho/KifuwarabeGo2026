namespace KifuwarabeGo2026.Engine;

using KifuwarabeGo2026.Shared.Domain;
using System.Reflection;
using System.Security.Cryptography;
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

/// <summary>
/// ［ＧＴＰエンジン］
/// </summary>
internal sealed class GtpEngine
{
    private static readonly string[] Commands =
    [
        "protocol_version", "name", "version", "known_command", "list_commands", "boardsize", "clear_board",
        "komi", "play", "genmove", "cgos-genmove_analyze",
        "kfw-options", "kfw-get-option", "kfw-set-option", "kfw-make-position", "kfw-listen-move",
        "gui_options", "gui_getoption", "gui_setoption",
        "kfw-begin-position", "kfw-add-black", "kfw-add-white", "kfw-set-to-play", "kfw-commit-position", "kfw-abort-position", "quit",
    ];
    private Random _random = new(0);
    private GoBoard _board = new(19);
    private GoPoint? _koPoint;
    private GoStone _sideToPlay = GoStone.Black;
    private readonly AtomicPositionSetup _positionSetup = new();
    private decimal _komi = 6.5m;
    private RandomMoveKind _randomMove = RandomMoveKind.ChebyshevDistanceFromStar;
    private bool _avoidEyes = true;
    private int _randomSeed;
    private string _engineTag = "";
    private string _debugLogFile = "";
    private bool _ponnukiProviderActive;
    private int _ponnukiBlackCaptures;
    private int _ponnukiWhiteCaptures;
    private const int PonnukiCaptureTarget = 20;

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
        var command = NormalizePrivateCommand(tokens[0]);
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
            case "kfw-options":
                response = CreateGuiOptionsJson();
                return false;
            case "kfw-get-option":
                ExecuteGuiGetOption(tokens, out response, out error);
                return false;
            case "kfw-set-option":
                ExecuteGuiSetOption(tokens, out error);
                return false;
            case "kfw-make-position":
                ExecuteMakePosition(tokens, out response, out error);
                return false;
            case "kfw-listen-move":
                ExecuteListenMove(tokens, out response, out error);
                return false;

            case "boardsize":
                if (RejectWhilePositionSetupActive(out error)) return false;
                ExecuteBoardSize(tokens, out error);
                return false;
            case "clear_board":
                if (RejectWhilePositionSetupActive(out error)) return false;
                _board = new GoBoard(_board.Size);
                _koPoint = null;
                _sideToPlay = GoStone.Black;
                return false;
            case "komi":
                ExecuteKomi(tokens, out error);
                return false;
            case "play":
                if (RejectWhilePositionSetupActive(out error)) return false;
                ExecutePlay(tokens, out error);
                return false;
            case "genmove":
                if (RejectWhilePositionSetupActive(out error)) return false;
                ExecuteGenMove(tokens, out response, out error);
                return false;
            case "cgos-genmove_analyze":
                if (RejectWhilePositionSetupActive(out error)) return false;
                ExecuteCgosGenMoveAnalyze(tokens, out response, out error);
                return false;
            case "kfw-begin-position":
                ExecuteBeginPosition(tokens, out error);
                return false;
            case "kfw-add-black":
                ExecuteAddPositionStone(tokens, GoStone.Black, out error);
                return false;
            case "kfw-add-white":
                ExecuteAddPositionStone(tokens, GoStone.White, out error);
                return false;
            case "kfw-set-to-play":
                ExecuteSetPositionTurn(tokens, out error);
                return false;
            case "kfw-commit-position":
                ExecuteCommitPosition(tokens, out error);
                return false;
            case "kfw-abort-position":
                ExecuteAbortPosition(tokens, out error);
                return false;
            case "quit":
                return true;
            default:
                error = $"unknown command: {tokens[0]}";
                return false;
        }
    }

    /// <summary>
    /// 旧独自コマンド名を、kfw-接頭辞の正規名へ読み替えます。
    /// </summary>
    private static string NormalizePrivateCommand(string command) => command.ToLowerInvariant() switch
    {
        "gui_options" => "kfw-options",
        "gui_getoption" => "kfw-get-option",
        "gui_setoption" => "kfw-set-option",
        "begin_position" => "kfw-begin-position",
        "add_black" => "kfw-add-black",
        "add_white" => "kfw-add-white",
        "set_to_play" => "kfw-set-to-play",
        "commit_position" => "kfw-commit-position",
        "abort_position" => "kfw-abort-position",
        var canonical => canonical,
    };

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
        _sideToPlay = GoStone.Black;
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
            _sideToPlay = Opponent(color);
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
        _sideToPlay = Opponent(color);
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
                    (!_avoidEyes || !_board.IsEyeFor(renParse, x, y, color)))
                {
                    legalMoves.Add(new GoPoint(x, y));
                }
            }
        }

        if (legalMoves.Count == 0)
        {
            _koPoint = null;
            _sideToPlay = Opponent(color);
            response = "pass";
            return;
        }

        var move = _randomMove == RandomMoveKind.Normal
            ? legalMoves[_random.Next(legalMoves.Count)]
            : StarRegionRandomMoveSelector.Select(legalMoves, _board.Size, _random);
        _board.TryPlaceStone(move.X, move.Y, color, _koPoint, out _, out _koPoint);
        _sideToPlay = Opponent(color);
        response = FormatVertex(move, _board.Size);
    }

    private void ExecuteMakePosition(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length is not (5 or 6) ||
            !tokens[1].Equals("ponnuki", StringComparison.OrdinalIgnoreCase) ||
            tokens[2] != "1" ||
            !int.TryParse(tokens[3], out var boardSize) || boardSize != 9 ||
            !int.TryParse(tokens[4], out var requestedMoveCount) || requestedMoveCount is < 0 or > 200 ||
            (tokens.Length == 6 && !int.TryParse(tokens[5], out _)))
        {
            error = "usage: kfw-make-position ponnuki 1 9 move-count [seed]";
            return;
        }

        // GetInt32 uses rejection sampling over the OS cryptographic random source,
        // so the automatically selected seed has no modulo bias.
        var seed = tokens.Length == 6
            ? int.Parse(tokens[5], System.Globalization.CultureInfo.InvariantCulture)
            : RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        var random = new Random(seed);
        var board = new GoBoard(boardSize);
        GoPoint? koPoint = null;
        var sideToPlay = GoStone.Black;

        for (var ply = 0; ply < requestedMoveCount; ply++)
        {
            var legalMoves = new List<GoPoint>();
            for (var y = 0; y < boardSize; y++)
            {
                for (var x = 0; x < boardSize; x++)
                {
                    var trial = board.Clone();
                    if (trial.TryPlaceStone(x, y, sideToPlay, koPoint, out _, out _))
                        legalMoves.Add(new GoPoint(x, y));
                }
            }

            if (legalMoves.Count == 0)
                break;

            var move = legalMoves[random.Next(legalMoves.Count)];
            board.TryPlaceStone(move.X, move.Y, sideToPlay, koPoint, out _, out koPoint);
            sideToPlay = Opponent(sideToPlay);
        }

        var black = new List<string>();
        var white = new List<string>();
        for (var y = 0; y < boardSize; y++)
        {
            for (var x = 0; x < boardSize; x++)
            {
                var stone = board.GetStone(x, y);
                if (stone == GoStone.Black) black.Add(FormatVertex(new GoPoint(x, y), boardSize));
                if (stone == GoStone.White) white.Add(FormatVertex(new GoPoint(x, y), boardSize));
            }
        }

        response = JsonSerializer.Serialize(new
        {
            app = "ponnuki",
            version = 1,
            boardSize,
            black,
            white,
            toPlay = sideToPlay == GoStone.Black ? "black" : "white",
            captures = new { black = 0, white = 0 },
            seed,
        });

        _board = board;
        _koPoint = null;
        _sideToPlay = sideToPlay;
        _ponnukiBlackCaptures = 0;
        _ponnukiWhiteCaptures = 0;
        _ponnukiProviderActive = true;
    }

    private void ExecuteListenMove(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (!_ponnukiProviderActive)
        {
            error = "kfw-make-position must be called first";
            return;
        }

        if (tokens.Length != 2)
        {
            error = "usage: kfw-listen-move vertex|pass";
            return;
        }

        var playedBy = _sideToPlay;
        var capturedStones = 0;
        if (IsPass(tokens[1]))
        {
            _koPoint = null;
        }
        else
        {
            if (!TryParseVertex(tokens[1], _board.Size, out var point) ||
                !_board.TryPlaceStone(point.X, point.Y, playedBy, _koPoint, out capturedStones, out var nextKoPoint))
            {
                error = "illegal provider move notification";
                return;
            }

            _koPoint = nextKoPoint;
        }

        if (playedBy == GoStone.Black)
            _ponnukiBlackCaptures += capturedStones;
        else
            _ponnukiWhiteCaptures += capturedStones;
        _sideToPlay = Opponent(playedBy);

        var gameOver = _ponnukiBlackCaptures >= PonnukiCaptureTarget ||
                       _ponnukiWhiteCaptures >= PonnukiCaptureTarget;
        var winner = !gameOver
            ? ""
            : _ponnukiBlackCaptures >= PonnukiCaptureTarget ? "black" : "white";
        response = JsonSerializer.Serialize(new
        {
            accepted = true,
            gameOver,
            winner,
            reason = gameOver ? $"PONNUKI {winner.ToUpperInvariant()} CAPTURED {PonnukiCaptureTarget}" : "",
            blackCaptures = _ponnukiBlackCaptures,
            whiteCaptures = _ponnukiWhiteCaptures,
            nextToPlay = _sideToPlay == GoStone.Black ? "black" : "white",
        });
    }

    private bool RejectWhilePositionSetupActive(out string? error)
    {
        error = _positionSetup.IsActive
            ? "position setup is active; use kfw-commit-position or kfw-abort-position"
            : null;
        return error is not null;
    }

    private void ExecuteBeginPosition(string[] tokens, out string? error)
    {
        if (tokens.Length != 1)
        {
            _positionSetup.Discard();
            error = "usage: kfw-begin-position; pending position discarded";
            return;
        }

        _positionSetup.Begin(_board.Size, out error);
    }

    private void ExecuteAddPositionStone(string[] tokens, GoStone stone, out string? error)
    {
        if (tokens.Length != 2)
        {
            _positionSetup.Discard();
            error = $"usage: {(stone == GoStone.Black ? "kfw-add-black" : "kfw-add-white")} vertex; pending position discarded";
            return;
        }

        _positionSetup.AddStone(tokens[1], stone, out error);
    }

    private void ExecuteSetPositionTurn(string[] tokens, out string? error)
    {
        if (tokens.Length != 2)
        {
            _positionSetup.Discard();
            error = "usage: kfw-set-to-play black|white; pending position discarded";
            return;
        }

        _positionSetup.SetTurn(tokens[1], out error);
    }

    private void ExecuteCommitPosition(string[] tokens, out string? error)
    {
        if (tokens.Length != 1)
        {
            _positionSetup.Discard();
            error = "usage: kfw-commit-position; pending position discarded";
            return;
        }

        if (!_positionSetup.Commit(out var board, out var turn, out error))
        {
            return;
        }

        _board = board!;
        _sideToPlay = turn;
        _koPoint = null;
    }

    private void ExecuteAbortPosition(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length != 1)
        {
            _positionSetup.Discard();
            error = "usage: kfw-abort-position";
            return;
        }

        _positionSetup.Discard();
    }

    /// <summary>CGOSへ着手、簡易評価値、1手の読み筋を返します。</summary>
    private void ExecuteCgosGenMoveAnalyze(string[] tokens, out string response, out string? error)
    {
        ExecuteGenMove(tokens, out var move, out error);
        if (error is not null)
        {
            response = "";
            return;
        }

        _ = TryParseColor(tokens[1], out var color);
        var reply = FindPreviewMove(Opponent(color));
        var blackLead = _board.CountStones(GoStone.Black) - _board.CountStones(GoStone.White) - (double)_komi;
        var perspectiveLead = color == GoStone.Black ? blackLead : -blackLead;
        var winrate = 1.0 / (1.0 + Math.Exp(-perspectiveLead / 5.0));
        var json = JsonSerializer.Serialize(new
        {
            comment = move.Equals("pass", StringComparison.OrdinalIgnoreCase)
                ? "パスしたぜ（＾～＾）"
                : $"{move.ToUpperInvariant()}に打ったぜ（＾～＾）",
            moves = new[]
            {
                new
                {
                    move,
                    winrate = Math.Round(winrate, 3),
                    score = Math.Round(perspectiveLead, 1),
                    pv = reply is null ? "" : FormatVertex(reply.Value, _board.Size),
                    visits = 1,
                },
            },
        });
        response = $"\n{json}\nplay {move}";
    }

    private GoPoint? FindPreviewMove(GoStone color)
    {
        var renParse = _board.ParseRens();
        for (var y = 0; y < _board.Size; y++)
        {
            for (var x = 0; x < _board.Size; x++)
            {
                var trial = _board.Clone();
                if (trial.TryPlaceStone(x, y, color, _koPoint, out _, out _) &&
                    (!_avoidEyes || !_board.IsEyeFor(renParse, x, y, color)))
                    return new GoPoint(x, y);
            }
        }

        return null;
    }

    private static GoStone Opponent(GoStone color) => color == GoStone.Black ? GoStone.White : GoStone.Black;

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

        var normalizedCommand = NormalizePrivateCommand(tokens[1]);
        response = Commands.Contains(normalizedCommand, StringComparer.OrdinalIgnoreCase) ? "true" : "false";
    }

    /// <summary>
    /// GUIが設定画面を構築するためのオプション定義を返します。
    /// </summary>
    private string CreateGuiOptionsJson() => JsonSerializer.Serialize(new
    {
        version = 1,
        options = new object[]
        {
            new
            {
                id = "RandomMove",
                label = "RandomMove",
                type = "combo",
                @default = "ChebyshevDistanceFromStar",
                value = _randomMove.ToString(),
                min = (int?)null,
                max = (int?)null,
                vars = new[] { "Normal", "ChebyshevDistanceFromStar" },
            },
            new { id = "AvoidEyes", label = "AvoidEyes", type = "check", @default = "true", value = _avoidEyes.ToString().ToLowerInvariant(), min = (int?)null, max = (int?)null, vars = Array.Empty<string>() },
            new { id = "RandomSeed", label = "RandomSeed", type = "spin", @default = "0", value = _randomSeed.ToString(), min = (int?)0, max = int.MaxValue, vars = Array.Empty<string>() },
            new { id = "EngineTag", label = "EngineTag", type = "string", @default = "", value = _engineTag, min = (int?)null, max = (int?)null, vars = Array.Empty<string>() },
            new { id = "DebugLogFile", label = "DebugLogFile", type = "filename", @default = "", value = _debugLogFile, min = (int?)null, max = (int?)null, vars = Array.Empty<string>() },
            new { id = "ClearCache", label = "ClearCache", type = "button", @default = "", value = "", min = (int?)null, max = (int?)null, vars = Array.Empty<string>() },
        },
    });

    /// <summary>
    /// GUIオプションの現在値を返します。
    /// </summary>
    private void ExecuteGuiGetOption(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 2)
        {
            error = "unknown option: " + (tokens.Length > 1 ? tokens[1] : "");
            return;
        }

        response = tokens[1].ToLowerInvariant() switch
        {
            "randommove" => _randomMove.ToString(),
            "avoideyes" => _avoidEyes.ToString().ToLowerInvariant(),
            "randomseed" => _randomSeed.ToString(),
            "enginetag" => _engineTag,
            "debuglogfile" => _debugLogFile,
            _ => "",
        };
        if (response.Length == 0 && !tokens[1].Equals("EngineTag", StringComparison.OrdinalIgnoreCase) && !tokens[1].Equals("DebugLogFile", StringComparison.OrdinalIgnoreCase))
            error = "unknown option: " + tokens[1];
    }

    /// <summary>
    /// GUIから送られたオプション値を設定します。
    /// </summary>
    private void ExecuteGuiSetOption(string[] tokens, out string? error)
    {
        error = null;
        if (tokens.Length < 2)
        {
            error = "usage: kfw-set-option RandomMove Normal|ChebyshevDistanceFromStar";
            return;
        }

        var value = tokens.Length >= 3 ? string.Join(' ', tokens[2..]) : "";
        if (tokens[1].Equals("RandomMove", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse(value, true, out RandomMoveKind randomMove)) error = "option RandomMove must be Normal or ChebyshevDistanceFromStar";
            else _randomMove = randomMove;
            return;
        }
        if (tokens[1].Equals("AvoidEyes", StringComparison.OrdinalIgnoreCase))
        {
            if (!bool.TryParse(value, out _avoidEyes)) error = "option AvoidEyes must be true or false";
            return;
        }
        if (tokens[1].Equals("RandomSeed", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(value, out var seed) || seed < 0) error = "option RandomSeed must be a non-negative integer";
            else { _randomSeed = seed; _random = new Random(seed); }
            return;
        }
        if (tokens[1].Equals("EngineTag", StringComparison.OrdinalIgnoreCase)) { _engineTag = value; return; }
        if (tokens[1].Equals("DebugLogFile", StringComparison.OrdinalIgnoreCase)) { _debugLogFile = value; return; }
        if (tokens[1].Equals("ClearCache", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Length != 2) error = "option ClearCache does not take a value";
            return;
        }
        error = "unknown option: " + tokens[1];
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

    internal static bool TryParseColor(string text, out GoStone stone)
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

    internal static bool TryParseVertex(string text, int boardSize, out GoPoint point)
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
