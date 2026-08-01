namespace KifuwarabeGo2026.Engine.ProblemProvider.Tsumego;

/// <summary>
/// 詰碁の問題を提供します。
/// </summary>
internal sealed class TsumegoProblemProvider : IProblemProvider
{
    public string ProblemId => "tsumego";

    public string DisplayName => "詰碁";

    public bool IsAvailable => false;
}
