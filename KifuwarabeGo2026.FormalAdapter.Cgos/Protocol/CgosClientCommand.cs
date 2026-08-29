namespace KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

public abstract record CgosClientCommand(bool IsSensitive = false);
public sealed record CgosClientIdentity(string ClientId, bool SupportsGenMoveAnalyze = false) : CgosClientCommand;
public sealed record CgosUsername(string Value) : CgosClientCommand;
public sealed record CgosPassword(string Value) : CgosClientCommand(IsSensitive: true);
public sealed record CgosMove(string Vertex, string? AnalysisJson = null) : CgosClientCommand;
public sealed record CgosResign() : CgosClientCommand;
public sealed record CgosReady() : CgosClientCommand;
public sealed record CgosQuit() : CgosClientCommand;
public sealed record CgosWho() : CgosClientCommand;
public sealed record CgosMatch(string Arguments = "") : CgosClientCommand;
