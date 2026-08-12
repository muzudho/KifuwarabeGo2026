namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>GTP エンジンの GUI オプション編集ダイアログを管理します。</summary>
public sealed partial class GoAppSession
{
    public void OpenGtpEngineRandomMoveSelectionDialog(GtpEngineGuiOptionSpec option)
    {
        ActiveGtpEngineComboOption = option;
        var choices = GetActiveGtpEngineComboChoices();
        var current = GetGtpEngineGuiOptionDraft(option);
        var currentIndex = choices.ToList().FindIndex(choice => choice.Value == current && choice.IsEnabled);
        GtpEngineRandomMoveSelectionIndex = currentIndex >= 0 ? currentIndex : Math.Max(0, choices.ToList().FindIndex(choice => choice.IsEnabled));
        GtpEngineRandomMoveSelectionPageIndex = GtpEngineRandomMoveSelectionIndex / GtpEngineComboSelectionPageSize;
        IsGtpEngineRandomMoveSelectionDialogOpen = true;
        ActivateWindow(ActiveWindowId.GtpEngineComboSelection);
    }

    public void SelectGtpEngineRandomMoveItem(int index)
    {
        var choices = GetActiveGtpEngineComboChoices();
        if (index >= 0 && index < choices.Count && choices[index].IsEnabled)
            GtpEngineRandomMoveSelectionIndex = index;
    }

    public int GetGtpEngineGuiOptionsPageCount() => Math.Max(1, (ActiveGtpEngineGuiOptionSpecs.Count + GtpEngineGuiOptionsPageSize - 1) / GtpEngineGuiOptionsPageSize);
    public void MoveGtpEngineGuiOptionsPage(int step) => GtpEngineGuiOptionsPageIndex = Math.Clamp(GtpEngineGuiOptionsPageIndex + step, 0, GetGtpEngineGuiOptionsPageCount() - 1);
    public int GetGtpEngineRandomMoveSelectionPageCount() => Math.Max(1, (GetActiveGtpEngineComboChoices().Count + GtpEngineComboSelectionPageSize - 1) / GtpEngineComboSelectionPageSize);
    public void MoveGtpEngineRandomMoveSelectionPage(int step) => GtpEngineRandomMoveSelectionPageIndex = Math.Clamp(GtpEngineRandomMoveSelectionPageIndex + step, 0, GetGtpEngineRandomMoveSelectionPageCount() - 1);
    public void CancelGtpEngineRandomMoveSelectionDialog()
    {
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineComboSelection);
    }

    public void CommitGtpEngineRandomMoveSelectionDialog()
    {
        var choices = GetActiveGtpEngineComboChoices();
        if (ActiveGtpEngineComboOption is { } option && GtpEngineRandomMoveSelectionIndex >= 0 && GtpEngineRandomMoveSelectionIndex < choices.Count && choices[GtpEngineRandomMoveSelectionIndex].IsEnabled)
            GtpEngineGuiOptionsDialogDraft[option.Id] = choices[GtpEngineRandomMoveSelectionIndex].Value;
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineComboSelection);
    }

    public IReadOnlyList<GtpEngineGuiOptionChoice> GetActiveGtpEngineComboChoices()
    {
        if (ActiveGtpEngineComboOption is not { } option) return [];
        return option.Choices ?? option.Values?.Select(value => new GtpEngineGuiOptionChoice(value)).ToArray() ?? [];
    }

    public string GtpEngineRandomMoveDraft => GtpEngineGuiOptionsDialogDraft.GetValueOrDefault(GtpEngineGuiOptions.RandomMoveId, GtpEngineGuiOptions.ChebyshevDistanceFromStarRandomMove);
    public string GetGtpEngineGuiOptionDraft(GtpEngineGuiOptionSpec option) => GtpEngineGuiOptionsDialogDraft.GetValueOrDefault(option.Id, option.DefaultValue);

    public void ToggleGtpEngineCheckOption(GtpEngineGuiOptionSpec option)
    {
        var current = bool.TryParse(GetGtpEngineGuiOptionDraft(option), out var value) && value;
        GtpEngineGuiOptionsDialogDraft[option.Id] = (!current).ToString().ToLowerInvariant();
    }

    public void StepGtpEngineSpinOption(GtpEngineGuiOptionSpec option, int step)
    {
        _ = int.TryParse(GetGtpEngineGuiOptionDraft(option), out var current);
        GtpEngineGuiOptionsDialogDraft[option.Id] = Math.Clamp((long)current + step, option.Min ?? int.MinValue, option.Max ?? int.MaxValue).ToString();
    }

    public void SetGtpEngineGuiOptionDraft(GtpEngineGuiOptionSpec option, string value) => GtpEngineGuiOptionsDialogDraft[option.Id] = value.Length <= GtpEngineGuiOptions.MaximumTextLength ? value : value[..GtpEngineGuiOptions.MaximumTextLength];

    public void ToggleGtpEngineButtonOption(GtpEngineGuiOptionSpec option)
    {
        var queued = bool.TryParse(GetGtpEngineGuiOptionDraft(option), out var value) && value;
        GtpEngineGuiOptionsDialogDraft[option.Id] = (!queued).ToString().ToLowerInvariant();
    }

    public bool ConsumeQueuedGtpEngineButtonsForComputerPlayers()
    {
        var consumed = false;
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
        {
            if (GetPlayerKind(stone) != GoPlayerKind.Computer) continue;
            var profile = GetGtpEngineProfile(stone);
            foreach (var option in GtpEngineGuiOptions.Specs.Where(option => option.Type == "button"))
            {
                consumed |= bool.TryParse(profile.GuiOptions.GetValueOrDefault(option.Id), out var queued) && queued;
                profile.GuiOptions[option.Id] = "false";
            }
        }

        return consumed;
    }
    public void OpenGtpEngineGuiOptionsDialog()
    {
        GtpEngineGuiOptionsDialogDraft = new Dictionary<string, string>(GtpEngineEditDraft.GuiOptions);
        foreach (var option in ActiveGtpEngineGuiOptionSpecs)
            GtpEngineGuiOptionsDialogDraft.TryAdd(option.Id, option.DefaultValue);
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        GtpEngineGuiOptionsPageIndex = 0;
        IsGtpEngineGuiOptionsDialogOpen = true;
        ActivateWindow(ActiveWindowId.GtpEngineGuiOptions);
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
        DeactivateWindow(ActiveWindowId.GtpEngineComboSelection);
        IsGtpEngineGuiOptionsDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineGuiOptions);
        GtpEngineGuiOptionsDialogDraft.Clear();
    }

    public void CommitGtpEngineGuiOptionsDialog()
    {
        GtpEngineEditDraft.GuiOptions = new Dictionary<string, string>(GtpEngineGuiOptionsDialogDraft);
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineComboSelection);
        IsGtpEngineGuiOptionsDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineGuiOptions);
        GtpEngineGuiOptionsDialogDraft.Clear();
        GtpEngineEditSaveMessage = "UNSAVED";
    }
}
