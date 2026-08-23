namespace KifuwarabeGo2026.Reference.Communication.Gtp.Server;

using KifuwarabeGo2026.Shared.Domain;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Casual App であるポン抜きの局面生成、進行、および終局判定を担当します。
/// </summary>
public sealed partial class GtpEngine
{
    private const int PonnukiCaptureTarget = 20;
    private int _ponnukiBoardSize = 9;
    private int _ponnukiInitialMoveCount = 20;
    private int _ponnukiRandomSeed;
    private bool _ponnukiProviderActive;
    private string? _activeCasualPlayerAppId;
    private int _ponnukiBlackCaptures;
    private int _ponnukiWhiteCaptures;

    private bool IsPonnukiCasualPlayerActive =>
        string.Equals(_activeCasualPlayerAppId, "ponnuki", StringComparison.OrdinalIgnoreCase);

    private void ExecuteMakePosition(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length is not (5 or 6) || !tokens[1].Equals("ponnuki", StringComparison.OrdinalIgnoreCase) || tokens[2] != "1" || !int.TryParse(tokens[3], out var boardSize) || !IsSupportedPonnukiBoardSize(boardSize) || !int.TryParse(tokens[4], out var requestedMoveCount) || requestedMoveCount < 0 || requestedMoveCount > GetPonnukiInitialMoveCountMaximum(boardSize) || (tokens.Length == 6 && !int.TryParse(tokens[5], out _)))
        {
            error = "usage: kfw-make-position ponnuki 1 {9|13|19} move-count [seed]";
            return;
        }

        var seed = tokens.Length == 6 ? int.Parse(tokens[5], System.Globalization.CultureInfo.InvariantCulture) : RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        var random = new Random(seed);
        var board = new GoBoard(boardSize);
        GoPoint? koPoint = null;
        var sideToPlay = GoStone.Black;
        for (var ply = 0; ply < requestedMoveCount; ply++)
        {
            var candidates = new List<GoPoint>(boardSize * boardSize);
            for (var y = 0; y < boardSize; y++) for (var x = 0; x < boardSize; x++) candidates.Add(new GoPoint(x, y));
            for (var index = candidates.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
            }
            GoPoint? selectedMove = null;
            foreach (var candidate in candidates)
            {
                var trial = board.Clone();
                if (!trial.TryPlaceStone(candidate.X, candidate.Y, sideToPlay, koPoint, out _, out _)) continue;
                selectedMove = candidate;
                break;
            }
            if (selectedMove is not { } move) break;
            board.TryPlaceStone(move.X, move.Y, sideToPlay, koPoint, out _, out koPoint);
            sideToPlay = Opponent(sideToPlay);
        }

        var black = new List<string>();
        var white = new List<string>();
        for (var y = 0; y < boardSize; y++) for (var x = 0; x < boardSize; x++)
        {
            var stone = board.GetStone(x, y);
            if (stone == GoStone.Black) black.Add(FormatVertex(new GoPoint(x, y), boardSize));
            if (stone == GoStone.White) white.Add(FormatVertex(new GoPoint(x, y), boardSize));
        }
        response = JsonSerializer.Serialize(new { app = "ponnuki", version = 1, boardSize, black, white, toPlay = sideToPlay == GoStone.Black ? "black" : "white", captures = new { black = 0, white = 0 }, seed });
        _board = board;
        _koPoint = null;
        _sideToPlay = sideToPlay;
        _ponnukiBlackCaptures = 0;
        _ponnukiWhiteCaptures = 0;
        _ponnukiProviderActive = true;
    }

    private void ExecuteStartApp(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 3 || !tokens[1].Equals("ponnuki", StringComparison.OrdinalIgnoreCase) || (!tokens[2].Equals("provider", StringComparison.OrdinalIgnoreCase) && !tokens[2].Equals("player", StringComparison.OrdinalIgnoreCase))) { error = "usage: kfw-start-app ponnuki provider|player"; return; }
        if (tokens[2].Equals("player", StringComparison.OrdinalIgnoreCase))
        {
            if (_activeCasualPlayerAppId is not null) { error = "casual player app is already started"; return; }
            _activeCasualPlayerAppId = "ponnuki";
            return;
        }
        if (_ponnukiProviderActive) { error = "ponnuki provider app is already started"; return; }
        var arguments = new List<string> { "kfw-make-position", "ponnuki", "1", _ponnukiBoardSize.ToString(System.Globalization.CultureInfo.InvariantCulture), _ponnukiInitialMoveCount.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (_ponnukiRandomSeed != 0) arguments.Add(_ponnukiRandomSeed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ExecuteMakePosition(arguments.ToArray(), out response, out error);
    }

    private void ExecuteEndApp(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 3 || !tokens[1].Equals("ponnuki", StringComparison.OrdinalIgnoreCase) || (!tokens[2].Equals("provider", StringComparison.OrdinalIgnoreCase) && !tokens[2].Equals("player", StringComparison.OrdinalIgnoreCase))) { error = "usage: kfw-end-app ponnuki provider|player"; return; }
        if (tokens[2].Equals("player", StringComparison.OrdinalIgnoreCase))
        {
            _activeCasualPlayerAppId = null;
            return;
        }
        _ponnukiProviderActive = false;
        _ponnukiBlackCaptures = 0;
        _ponnukiWhiteCaptures = 0;
    }

    private void ExecuteListenMove(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (!_ponnukiProviderActive) { error = "kfw-start-app or kfw-make-position must be called first"; return; }
        if (tokens.Length != 2) { error = "usage: kfw-listen-move vertex|pass"; return; }
        var playedBy = _sideToPlay;
        var capturedStones = 0;
        if (IsPass(tokens[1])) _koPoint = null;
        else
        {
            if (!TryParseVertex(tokens[1], _board.Size, out var point) || !_board.TryPlaceStone(point.X, point.Y, playedBy, _koPoint, out capturedStones, out var nextKoPoint)) { error = "illegal provider move notification"; return; }
            _koPoint = nextKoPoint;
        }
        if (playedBy == GoStone.Black) _ponnukiBlackCaptures += capturedStones;
        else _ponnukiWhiteCaptures += capturedStones;
        _sideToPlay = Opponent(playedBy);
        var gameOver = _ponnukiBlackCaptures >= PonnukiCaptureTarget || _ponnukiWhiteCaptures >= PonnukiCaptureTarget;
        var winner = !gameOver ? "" : _ponnukiBlackCaptures >= PonnukiCaptureTarget ? "black" : "white";
        response = JsonSerializer.Serialize(new { accepted = true, gameOver, winner, reason = gameOver ? $"PONNUKI {winner.ToUpperInvariant()} CAPTURED {PonnukiCaptureTarget}" : "", blackCaptures = _ponnukiBlackCaptures, whiteCaptures = _ponnukiWhiteCaptures, nextToPlay = _sideToPlay == GoStone.Black ? "black" : "white" });
    }
}
