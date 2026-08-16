namespace KifuwarabeGo2026.Launcher;

internal enum InstalledProduct
{
    Gui,
    Engine,
    LegacyGuiUpdate,
}

internal sealed record InstalledVersion(
    InstalledProduct Product,
    string Version,
    string DirectoryPath,
    long SizeInBytes,
    bool IsCurrent,
    bool IsPrevious,
    bool IsRunning)
{
    public bool CanUninstall => !IsCurrent && !IsPrevious && !IsRunning;

    public string ProductName => Product switch
    {
        InstalledProduct.Gui => "GUI",
        InstalledProduct.Engine => "ENGINE",
        _ => "GUI (旧更新機能)",
    };

    public string Protection => IsRunning ? "実行中" : IsCurrent ? "現在使用中" : IsPrevious ? "ロールバック用" : string.Empty;
}
