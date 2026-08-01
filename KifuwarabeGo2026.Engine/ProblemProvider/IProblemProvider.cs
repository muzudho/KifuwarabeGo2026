namespace KifuwarabeGo2026.Engine.ProblemProvider;

/// <summary>
/// GUIへ問題を提供するエンジン内部コンポーネントの共通契約です。
/// </summary>
internal interface IProblemProvider
{
    /// <summary>GTPで使用する、変更しない機械用ID。</summary>
    string ProblemId { get; }

    /// <summary>人間向けの問題名。</summary>
    string DisplayName { get; }

    /// <summary>現在、この問題を提供できるか。</summary>
    bool IsAvailable { get; }
}
