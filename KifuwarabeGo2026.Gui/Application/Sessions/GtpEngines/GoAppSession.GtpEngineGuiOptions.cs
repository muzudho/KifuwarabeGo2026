namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using KifuwarabeGo2026.GtpExtensions.Engines;

/// <summary>GTP エンジンの GUI オプション編集ダイアログを管理します。</summary>
public sealed partial class GoAppSession
{
    public void OpenGtpEngineGuiOptionsDialog()
    {
        GtpEngineGuiOptionsDialogDraft = new Dictionary<string, string>(GtpEngineEditDraft.GuiOptions);
        foreach (var option in ActiveGtpEngineGuiOptionSpecs)
            GtpEngineGuiOptionsDialogDraft.TryAdd(option.Id, option.DefaultValue);
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        GtpEngineGuiOptionsPageIndex = 0;
        IsGtpEngineGuiOptionsDialogOpen = true;
        ActiveGtpEngineEditField = null;
    }

    public void OpenAppProviderGameSettingsDialog(IReadOnlyList<GtpEngineGuiOptionSpec> specs)
    {
        _appProviderGameSettingSpecs = specs.Count > 0 ? specs : GtpEngineGuiOptions.PonnukiProviderSpecs;
        GtpEngineEditProfileIndex = Math.Clamp(SelectedAppProviderEngineIndex, 0, _gtpEngineProfiles.Count - 1);
        GtpEngineEditDraft = _gtpEngineProfiles[GtpEngineEditProfileIndex].Clone();
        IsAppProviderGameSettingsDialogOpen = true;
        OpenGtpEngineGuiOptionsDialog();
    }

    public void ApplyAppProviderGameSettingsEvaluation(IReadOnlyList<GtpEngineGuiOptionSpec> specs, IReadOnlyDictionary<string, string> values)
    {
        if (!IsAppProviderGameSettingsDialogOpen) return;
        _appProviderGameSettingSpecs = specs.Count > 0 ? specs : _appProviderGameSettingSpecs;
        foreach (var pair in values)
            GtpEngineGuiOptionsDialogDraft[pair.Key] = pair.Value;
        GtpEngineGuiOptionsPageIndex = Math.Clamp(GtpEngineGuiOptionsPageIndex, 0, GetGtpEngineGuiOptionsPageCount() - 1);
    }

    public void CancelAppProviderGameSettingsDialog()
    {
        CancelGtpEngineGuiOptionsDialog();
        IsAppProviderGameSettingsDialogOpen = false;
    }

    public IReadOnlyList<GtpEngineProfile> CommitAppProviderGameSettingsDialog()
    {
        CommitGtpEngineGuiOptionsDialog();
        ReplaceSelectedGtpEngine(GtpEngineEditDraft);
        IsAppProviderGameSettingsDialogOpen = false;
        return _gtpEngineProfiles;
    }

    public void CancelGtpEngineGuiOptionsDialog()
    {
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        IsGtpEngineGuiOptionsDialogOpen = false;
        GtpEngineGuiOptionsDialogDraft.Clear();
    }

    public void CommitGtpEngineGuiOptionsDialog()
    {
        GtpEngineEditDraft.GuiOptions = new Dictionary<string, string>(GtpEngineGuiOptionsDialogDraft);
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        IsGtpEngineGuiOptionsDialogOpen = false;
        GtpEngineGuiOptionsDialogDraft.Clear();
        GtpEngineEditSaveMessage = "UNSAVED";
    }
}
