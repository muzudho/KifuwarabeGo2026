namespace KifuwarabeGo2026.Engine.ProblemProvider.NextMove;

/// <summary>
/// 次の一手問題を提供します。
/// </summary>
internal sealed class NextMoveProblemProvider : IProblemProvider
{
    public string ProblemId => "next-move";

    public string DisplayName => "次の一手問題";

    public bool IsAvailable => false;
}
