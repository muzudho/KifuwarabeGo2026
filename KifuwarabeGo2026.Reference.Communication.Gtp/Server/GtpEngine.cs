namespace KifuwarabeGo2026.Reference.Communication.Gtp.Server;

using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Reference.PlayerEngine.Strategies.Ponnuki;
using KifuwarabeGo2026.Reference.PlayerEngine.Strategies;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// ［ＧＴＰエンジン］
/// </summary>
public sealed partial class GtpEngine
{
    private static readonly PlayStrategy _playStrategy = new();
    private static readonly PonnukiStrategy _ponnukiStrategy = new();
    private static readonly string[] SupportedAppIds = ["play", "ponnuki"];
    private static readonly string[] SupportedPlayerAppIds = ["play", "ponnuki"];
    private static readonly string[] SupportedProviderAppIds = ["ponnuki"];

    private static readonly string[] Commands =
    [
        "protocol_version", "name", "version", "known_command", "list_commands", "boardsize", "clear_board",
        "komi", "play", "genmove", "cgos-genmove_analyze",
        "kfw-list-apps",
        "kfw-describe-options", "kfw-get-options", "kfw-evaluate-options", "kfw-patch-options", "kfw-invoke-option",
        "kfw-options", "kfw-get-option", "kfw-set-option", "kfw-start-app", "kfw-end-app", "kfw-make-position", "kfw-listen-move",
        "gui_options", "gui_getoption", "gui_setoption",
        "kfw-begin-position", "kfw-add-black", "kfw-add-white", "kfw-set-to-play", "kfw-commit-position", "kfw-abort-position", "quit",
    ];
    private Random _random = new(0);
    private GoBoard _board = new(19);
    private GoPoint? _koPoint;
    private GoStone _sideToPlay = GoStone.Black;
    private readonly AtomicPositionSetup _positionSetup = new();
    private decimal _komi = 6.5m;
    private MoveSelectionMode _randomMove = MoveSelectionMode.ChebyshevDistanceFromStar;
    private bool _avoidEyes = true;
    private int _randomSeed;
    private string _engineTag = "";
    private string _debugLogFile = "";
    private string _lastMoveComment = "";

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
            case "kfw-list-apps":
                if (tokens.Length > 2)
                {
                    error = "usage: kfw-list-apps [player|provider]";
                    return false;
                }

                if (tokens.Length == 1)
                {
                    response = string.Join('\n', SupportedAppIds);
                    return false;
                }

                var roleAppIds = tokens[1].ToLowerInvariant() switch
                {
                    "player" => SupportedPlayerAppIds,
                    "provider" => SupportedProviderAppIds,
                    _ => null,
                };
                if (roleAppIds is null)
                {
                    error = "usage: kfw-list-apps [player|provider]";
                    return false;
                }

                response = string.Join('\n', roleAppIds);
                return false;
            case "kfw-describe-options":
                ExecuteDescribeOptions(tokens, out response, out error);
                return false;
            case "kfw-get-options":
                ExecuteGetOptions(tokens, out response, out error);
                return false;
            case "kfw-evaluate-options":
                ExecuteEvaluateOptions(commandLine, tokens, out response, out error);
                return false;
            case "kfw-patch-options":
                ExecutePatchOptions(commandLine, tokens, out response, out error);
                return false;
            case "kfw-invoke-option":
                ExecuteInvokeOption(tokens, out response, out error);
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
            case "kfw-start-app":
                ExecuteStartApp(tokens, out response, out error);
                return false;
            case "kfw-end-app":
                ExecuteEndApp(tokens, out response, out error);
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

        var request = new GenerateMoveRequest(
            _board,
            color,
            _koPoint,
            _avoidEyes,
            _randomMove,
            _random);
        _lastMoveComment = "";
        GoPoint? move;
        if (IsPonnukiCasualPlayerActive)
        {
            move = _ponnukiStrategy.GenerateMoveWithDecision(request) is { } ponnukiDecision
                ? SetPonnukiMoveDecision(ponnukiDecision)
                : null;
        }
        else
        {
            move = _playStrategy.GenerateMoveWithDecision(request) is { } playDecision
                ? SetPlayMoveDecision(playDecision)
                : null;
        }
        if (move is null)
        {
            _koPoint = null;
            _sideToPlay = Opponent(color);
            response = "pass";
            return;
        }

        _board.TryPlaceStone(move.Value.X, move.Value.Y, color, _koPoint, out _, out _koPoint);
        _sideToPlay = Opponent(color);
        response = FormatVertex(move.Value, _board.Size);
    }

    private GoPoint SetPonnukiMoveDecision(PonnukiMoveDecision decision)
    {
        _lastMoveComment = decision.ToComment();
        return decision.Move;
    }

    private GoPoint SetPlayMoveDecision(PlayMoveDecision decision)
    {
        _lastMoveComment = decision.ToComment();
        return decision.Move;
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
            comment = !string.IsNullOrWhiteSpace(_lastMoveComment)
                ? _lastMoveComment
                : move.Equals("pass", StringComparison.OrdinalIgnoreCase)
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
            if (!Enum.TryParse(value, true, out MoveSelectionMode randomMove)) error = "option RandomMove must be Normal or ChebyshevDistanceFromStar";
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

    public static bool TryParseColor(string text, out GoStone stone)
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

    public static bool TryParseVertex(string text, int boardSize, out GoPoint point)
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

}
