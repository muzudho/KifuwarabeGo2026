namespace KifuwarabeGo2026.Reference.Communication.Gtp.Server;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Builds an edited position away from the live board and exposes it only after a successful commit.
/// </summary>
public sealed class AtomicPositionSetup
{
    private GoBoard? _pendingBoard;
    private GoStone? _pendingTurn;

    public bool IsActive => _pendingBoard is not null;

    public bool Begin(int boardSize, out string? error)
    {
        error = null;
        if (IsActive)
        {
            Discard();
            error = "position setup is already active; pending position discarded";
            return false;
        }

        _pendingBoard = new GoBoard(boardSize);
        _pendingTurn = null;
        return true;
    }

    public bool AddStone(string vertex, GoStone stone, out string? error)
    {
        error = null;
        if (_pendingBoard is null)
        {
            error = "kfw-begin-position is required";
            return false;
        }

        if (!GtpEngine.TryParseVertex(vertex, _pendingBoard.Size, out var point))
        {
            return Fail("invalid vertex", out error);
        }

        if (!_pendingBoard.TrySetSetupStone(point.X, point.Y, stone))
        {
            return Fail("position point is already occupied", out error);
        }

        return true;
    }

    public bool SetTurn(string color, out string? error)
    {
        error = null;
        if (_pendingBoard is null)
        {
            error = "kfw-begin-position is required";
            return false;
        }

        if (!GtpEngine.TryParseColor(color, out var stone))
        {
            return Fail("kfw-set-to-play requires black or white", out error);
        }

        _pendingTurn = stone;
        return true;
    }

    public bool Commit(out GoBoard? board, out GoStone turn, out string? error)
    {
        board = null;
        turn = GoStone.Empty;
        error = null;
        if (_pendingBoard is null)
        {
            error = "kfw-begin-position is required";
            return false;
        }

        if (_pendingTurn is null)
        {
            return Fail("kfw-set-to-play is required", out error);
        }

        board = _pendingBoard;
        turn = _pendingTurn.Value;
        Discard();
        return true;
    }

    public void Discard()
    {
        _pendingBoard = null;
        _pendingTurn = null;
    }

    private bool Fail(string message, out string? error)
    {
        Discard();
        error = $"{message}; pending position discarded";
        return false;
    }
}
