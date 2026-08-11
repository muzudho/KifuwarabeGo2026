namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

/// <summary>CGOS 接続プロファイルのカタログ操作を管理します。</summary>
public sealed partial class GoAppSession
{
    public void SetCgosConnectionProfiles(IEnumerable<CgosConnectionProfile> profiles)
    {
        _cgosConnectionProfiles.Clear();
        _cgosConnectionProfiles.AddRange(profiles);
        if (_cgosConnectionProfiles.Count == 0)
        {
            _cgosConnectionProfiles.Add(new CgosConnectionProfile("\u90B1\uFF74\u9119\u30FB", "uec-go.com", 6809, "PRACTICE", "CGOS practice server"));
        }

        SelectedCgosConnectionProfileIndex = Math.Clamp(SelectedCgosConnectionProfileIndex, 0, _cgosConnectionProfiles.Count - 1);
        CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
    }

    public void MoveCgosConnectionSelectionPage(int step)
    {
        CgosConnectionSelectionPageIndex = Math.Clamp(
            CgosConnectionSelectionPageIndex + step,
            0,
            GetCgosConnectionSelectionPageCount() - 1);
    }

    public int GetCgosConnectionSelectionPageCount() =>
        Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)CgosConnectionSelectionPageSize));

    public bool CanDeleteSelectedCgosConnectionProfile =>
        _cgosConnectionProfiles.Count > 1 &&
        SelectedCgosConnectionProfileIndex >= 0 &&
        SelectedCgosConnectionProfileIndex < _cgosConnectionProfiles.Count;

    public bool CanMoveCgosConnectionSelectionPage(int step) =>
        Math.Clamp(CgosConnectionSelectionPageIndex + step, 0, GetCgosConnectionSelectionPageCount() - 1) != CgosConnectionSelectionPageIndex;

    public string GetCgosConnectionEditFieldText(CgosConnectionProfileEditField field) => field switch
    {
        CgosConnectionProfileEditField.DisplayName => CgosConnectionEditDraft.DisplayName,
        CgosConnectionProfileEditField.Host => CgosConnectionEditDraft.Host,
        CgosConnectionProfileEditField.Port => CgosConnectionPortDraft,
        CgosConnectionProfileEditField.Event => CgosConnectionEditDraft.Event,
        CgosConnectionProfileEditField.Round => CgosConnectionEditDraft.Round,
        CgosConnectionProfileEditField.Note => CgosConnectionEditDraft.Note,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "CGOS connection edit field is out of range."),
    };

    public IEnumerable<int> GetVisibleCgosConnectionProfileIndexes()
    {
        var startIndex = CgosConnectionSelectionPageIndex * CgosConnectionSelectionPageSize;
        var endIndex = Math.Min(startIndex + CgosConnectionSelectionPageSize, _cgosConnectionProfiles.Count);
        for (var i = startIndex; i < endIndex; i++)
            yield return i;
    }

    public void OpenCgosConnectionOrderEditor() =>
        CgosConnectionOrderEditor.Open(
            _cgosConnectionProfiles,
            SelectedCgosConnectionProfileIndex,
            CgosConnectionSelectionPageSize);

    public void CancelCgosConnectionOrderEditor() => CgosConnectionOrderEditor.Cancel();

    public IReadOnlyList<CgosConnectionProfile> CommitCgosConnectionOrderEditor()
    {
        var selectedProfile =
            SelectedCgosConnectionProfileIndex >= 0 &&
            SelectedCgosConnectionProfileIndex < _cgosConnectionProfiles.Count
                ? _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex]
                : null;
        var orderedProfiles = CgosConnectionOrderEditor.Commit();
        _cgosConnectionProfiles.Clear();
        _cgosConnectionProfiles.AddRange(orderedProfiles);
        var selectedIndex = selectedProfile is null
            ? -1
            : _cgosConnectionProfiles.FindIndex(profile => ReferenceEquals(profile, selectedProfile));
        SelectedCgosConnectionProfileIndex =
            _cgosConnectionProfiles.Count == 0 ? 0 : Math.Max(0, selectedIndex);
        CgosConnectionSelectionPageIndex =
            SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
        return _cgosConnectionProfiles.ToArray();
    }
}
