namespace KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

public abstract record CgosServerMessage(string RawLine);

public sealed record CgosProtocolAdvertised(string RawLine, string Advertisement, bool SupportsGenMoveAnalyze)
    : CgosServerMessage(RawLine);
public sealed record CgosUsernameRequested(string RawLine) : CgosServerMessage(RawLine);
public sealed record CgosPasswordRequested(string RawLine) : CgosServerMessage(RawLine);
public sealed record CgosLoginAccepted(string RawLine) : CgosServerMessage(RawLine);
public sealed record CgosServerError(string RawLine, string Message) : CgosServerMessage(RawLine);
public sealed record CgosInfoMessage(string RawLine, string Payload) : CgosServerMessage(RawLine);
public sealed record CgosUnknownServerMessage(string RawLine, string Command, IReadOnlyList<string> Arguments)
    : CgosServerMessage(RawLine);

public sealed record CgosMatchSetup(
    string RawLine,
    int GameId,
    int BoardSize,
    decimal Komi,
    long MainTimeMilliseconds,
    string WhitePlayer,
    string BlackPlayer,
    IReadOnlyList<CgosHistoricalMove> MoveHistory)
    : CgosServerMessage(RawLine);

public sealed record CgosHistoricalMove(string Color, string Vertex, long TimeLeftMilliseconds);
public sealed record CgosMovePlayed(string RawLine, string Color, string Vertex, long TimeLeftMilliseconds)
    : CgosServerMessage(RawLine);
public sealed record CgosGenMoveRequested(string RawLine, string Color, long TimeLeftMilliseconds)
    : CgosServerMessage(RawLine);
public sealed record CgosGameOver(string RawLine, string Result) : CgosServerMessage(RawLine);
