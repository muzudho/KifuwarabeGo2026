namespace KifuwarabeGo2026.Engine.AppProvider;

/// <summary>
/// GUIへGo Appを提供するエンジン内部コンポーネントの共通契約です。
/// </summary>
internal interface IAppProvider
{
    /// <summary>GTPで使用する、変更しない機械用ID。</summary>
    string AppId { get; }

    /// <summary>人間向けのアプリ名。</summary>
    string DisplayName { get; }

    /// <summary>現在、このアプリを提供できるか。</summary>
    bool IsAvailable { get; }
}
