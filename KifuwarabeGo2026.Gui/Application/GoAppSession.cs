namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed partial class GoAppSession
{
    private GoBoard _board;
    private readonly HashSet<ulong> _positionHashes = new();

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
        _gtpEngineProfiles.Add(new GtpEngineProfile());
        _gtpEngineAppCompatibilities.Add(new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY FORMAL APP"));
        ResetPositionHistory();
    }

    public int BoardSize { get; private set; } = 19;

    public void SetCgosConnectionProfiles(IEnumerable<CgosConnectionProfile> profiles)
    {
        _cgosConnectionProfiles.Clear();
        _cgosConnectionProfiles.AddRange(profiles);
        if (_cgosConnectionProfiles.Count == 0)
        {
            _cgosConnectionProfiles.Add(new CgosConnectionProfile("練習", "uec-go.com", 6809, "PRACTICE", "CGOS practice server"));
        }

        SelectedCgosConnectionProfileIndex = Math.Clamp(SelectedCgosConnectionProfileIndex, 0, _cgosConnectionProfiles.Count - 1);
        CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
    }

    /// <summary>
    /// CGOSプレイヤー用の共通GTPエンジン選択ダイアログを開きます。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログを開きます。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログの編集内容を破棄します。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログの編集内容をエンジン設定へ反映します。
    /// </summary>
    /// <summary>次回対局用button予約を、使用したエンジンプロファイルから取り除きます。</summary>
    public const int TournamentRulesSelectionPageSize = 6;

    public const int GtpEngineSelectionPageSize = 6;

    public const int CgosConnectionSelectionPageSize = 5;

    public const int CgosAdminPlayerSelectionPageSize = 6;

    private static CgosConnectionProfile CreateDefaultCgosConnectionProfile() =>
        new("New CGOS Connection", "uec-go.com", 6809, "PRACTICE", "CGOS practice server") { Event = "PRACTICE" };

}
