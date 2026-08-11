namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局・CGOSに割り当てるGTPエンジンを選択します。</summary>
public sealed partial class GoAppSession
{
    private GtpEngineProfile? GetCgosGtpEngineProfile(int? index) =>
        index is { } selectedIndex && selectedIndex >= 0 && selectedIndex < _gtpEngineProfiles.Count
            ? _gtpEngineProfiles[selectedIndex]
            : null;

    private int? GetSelectedCgosGtpEngineIndex(GoStone stone) => stone switch
    {
        GoStone.Black => SelectedCgosBlackGtpEngineIndex,
        GoStone.White => SelectedCgosWhiteGtpEngineIndex,
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white."),
    };

    private void SetSelectedCgosGtpEngineIndex(GoStone stone, int? index)
    {
        if (stone == GoStone.Black)
        {
            SelectedCgosBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedCgosWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white.");
    }

    public void SelectGtpEngine(GoStone stone, int index)
    {
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "GTP engine index is out of range.");

        if (IsGtpEngineSelectionForCgos)
        {
            SetSelectedCgosGtpEngineIndex(stone, index);
            SetCgosPlayerCredentials(
                stone,
                _gtpEngineProfiles[index].DefaultCgosLoginName,
                _gtpEngineProfiles[index].DefaultCgosPlainTextPassword);
            return;
        }

        if (stone == GoStone.Black)
        {
            SelectedBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be selected only for black or white.");
    }

    public GtpEngineProfile GetGtpEngineProfile(GoStone stone)
    {
        var index = stone switch
        {
            GoStone.Black => SelectedBlackGtpEngineIndex,
            GoStone.White => SelectedWhiteGtpEngineIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be read only for black or white."),
        };

        return _gtpEngineProfiles[Math.Clamp(index, 0, _gtpEngineProfiles.Count - 1)].Clone();
    }
}
