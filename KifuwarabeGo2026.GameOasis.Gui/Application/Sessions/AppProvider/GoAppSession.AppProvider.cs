namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;
using System;
using System.IO;

/// <summary>フォーマルアプリ連携用エンジンの選択と互換性確認状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public int SelectedAppProviderEngineIndex { get; private set; }
    public bool HasSelectedAppProviderEngine =>
        SelectedAppProviderEngineIndex >= 0 && SelectedAppProviderEngineIndex < _gtpEngineProfiles.Count;
    public string SelectedAppProviderEngineDisplayName =>
        HasSelectedAppProviderEngine ? _gtpEngineProfiles[SelectedAppProviderEngineIndex].DisplayName : "未選択";
    public GtpEngineProfile SelectedAppProviderEngine =>
        _gtpEngineProfiles[Math.Clamp(SelectedAppProviderEngineIndex, 0, _gtpEngineProfiles.Count - 1)];

    public bool CanUseSelectedAppProvider
    {
        get
        {
            if (!HasSelectedAppProviderEngine)
                return false;

            var path = SelectedAppProviderEngine.ExecutablePath;
            return !string.IsNullOrWhiteSpace(path) &&
                (!Path.IsPathFullyQualified(path) || File.Exists(path));
        }
    }

    public bool CanStartSelectedAppProvider => CanUseSelectedAppProvider && IsAppProviderCapabilityConfirmed;
    public string LocalAppsErrorMessage { get; private set; } = "";
    public string AppProviderCapabilityStatus { get; private set; } = "NOT CHECKED";
    public bool IsAppProviderCapabilityConfirmed { get; private set; }
    public bool IsAppProviderCapabilityCheckRunning =>
        AppProviderCapabilityStatus.StartsWith("CHECKING", StringComparison.Ordinal);

    public void ClearLocalAppsError() => LocalAppsErrorMessage = "";
    public void SetLocalAppsError(string message) => LocalAppsErrorMessage = message ?? "";

    public void SetAppProviderCapability(bool isConfirmed, string status)
    {
        IsAppProviderCapabilityConfirmed = isConfirmed;
        AppProviderCapabilityStatus = status ?? "";
    }

    public void SelectAppProviderEngine(int index)
    {
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "App Provider engine index is out of range.");

        SelectedAppProviderEngineIndex = index;
        SetAppProviderCapability(false, "NOT CHECKED");
    }

    public bool RestoreAppProviderEngine(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return false;

        var index = _gtpEngineProfiles.FindIndex(profile =>
            string.Equals(profile.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return false;

        SelectAppProviderEngine(index);
        return true;
    }
}
