namespace KifuwarabeGo2026.Engine.ProblemProvider.Ponnuki;

/// <summary>
/// ポン抜きゲームの初期局面、進行、終局判定、結果を提供します。
/// </summary>
internal sealed class PonnukiProblemProvider : IProblemProvider
{
    public string ProblemId => "ponnuki";

    public string DisplayName => "ポン抜きゲーム";

    public bool IsAvailable => false;
}
