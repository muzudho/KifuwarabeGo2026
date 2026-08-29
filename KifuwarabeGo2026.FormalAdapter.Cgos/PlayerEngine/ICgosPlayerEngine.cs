namespace KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine;

public interface ICgosPlayerEngine : IAsyncDisposable
{
    bool SupportsAnalyze { get; }
    Task ConfigureAsync(int boardSize, decimal komi, CancellationToken cancellationToken = default);
    Task PlayAsync(string color, string vertex, long timeLeftMilliseconds, CancellationToken cancellationToken = default);
    Task<CgosGeneratedMove> GenerateMoveAsync(string color, bool includeAnalysis, CancellationToken cancellationToken = default);
}

public sealed record CgosGeneratedMove(string Vertex, string? AnalysisJson = null);

public delegate Task<ICgosPlayerEngine> CgosPlayerEngineFactory(
    CgosPlayerEngineSetup setup,
    CancellationToken cancellationToken);

public sealed record CgosPlayerEngineSetup(
    int GameId,
    int BoardSize,
    decimal Komi,
    string LocalColor,
    string WhitePlayer,
    string BlackPlayer);
