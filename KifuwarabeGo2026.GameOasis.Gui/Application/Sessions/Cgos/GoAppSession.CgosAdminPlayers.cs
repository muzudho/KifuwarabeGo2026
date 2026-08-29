namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using System.Collections.Generic;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>CGOS 管理者による待機プレイヤー選択を管理します。</summary>
public sealed partial class GoAppSession
{
    public void SetCgosAdminWaitingPlayers(IReadOnlyList<string> players)
    {
        var previousWhite = CgosAdminWhitePlayerName;
        var previousBlack = CgosAdminBlackPlayerName;
        CgosAdminWaitingPlayers = players;
        CgosAdminWhitePlayerIndex = FindCgosAdminWaitingPlayerIndex(previousWhite, 0);
        CgosAdminBlackPlayerIndex = FindCgosAdminWaitingPlayerIndex(previousBlack, Math.Min(1, players.Count - 1));
        if (players.Count > 1 && CgosAdminBlackPlayerIndex == CgosAdminWhitePlayerIndex)
            CgosAdminBlackPlayerIndex = (CgosAdminWhitePlayerIndex + 1) % players.Count;

        CgosAdminPlayerDialogSelectionIndex = players.Count == 0
            ? 0
            : Math.Clamp(CgosAdminPlayerDialogSelectionIndex, 0, players.Count - 1);
        CgosAdminPlayerSelectionPageIndex = Math.Clamp(
            CgosAdminPlayerSelectionPageIndex,
            0,
            GetCgosAdminPlayerSelectionPageCount() - 1);
    }

    public void MoveCgosAdminWhitePlayerSelection(int step) =>
        CgosAdminWhitePlayerIndex = MoveCgosAdminWaitingPlayerIndex(CgosAdminWhitePlayerIndex, step);

    public void MoveCgosAdminBlackPlayerSelection(int step) =>
        CgosAdminBlackPlayerIndex = MoveCgosAdminWaitingPlayerIndex(CgosAdminBlackPlayerIndex, step);

    public void OpenCgosAdminPlayerSelectionDialog(GoStone target)
    {
        CgosAdminPlayerSelectionTarget = target;
        CgosAdminPlayerDialogSelectionIndex = target == GoStone.White
            ? CgosAdminWhitePlayerIndex
            : CgosAdminBlackPlayerIndex;
        CgosAdminPlayerSelectionPageIndex = CgosAdminPlayerDialogSelectionIndex / CgosAdminPlayerSelectionPageSize;
        IsCgosAdminPlayerSelectionDialogOpen = true;
        ActivateWindow(ActiveWindowId.CgosAdminPlayerSelection);
    }

    public void SelectCgosAdminPlayerDialogItem(int index)
    {
        if (index >= 0 && index < CgosAdminWaitingPlayers.Count)
            CgosAdminPlayerDialogSelectionIndex = index;
    }

    public void CommitCgosAdminPlayerSelectionDialog()
    {
        if (CgosAdminPlayerDialogSelectionIndex >= 0 && CgosAdminPlayerDialogSelectionIndex < CgosAdminWaitingPlayers.Count)
        {
            if (CgosAdminPlayerSelectionTarget == GoStone.White)
                CgosAdminWhitePlayerIndex = CgosAdminPlayerDialogSelectionIndex;
            else
                CgosAdminBlackPlayerIndex = CgosAdminPlayerDialogSelectionIndex;
        }

        IsCgosAdminPlayerSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.CgosAdminPlayerSelection);
    }

    public void CancelCgosAdminPlayerSelectionDialog()
    {
        IsCgosAdminPlayerSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.CgosAdminPlayerSelection);
    }

    public int GetCgosAdminPlayerSelectionPageCount() =>
        Math.Max(1, (int)Math.Ceiling(CgosAdminWaitingPlayers.Count / (double)CgosAdminPlayerSelectionPageSize));

    public void MoveCgosAdminPlayerSelectionPage(int step) =>
        CgosAdminPlayerSelectionPageIndex = Math.Clamp(
            CgosAdminPlayerSelectionPageIndex + step,
            0,
            GetCgosAdminPlayerSelectionPageCount() - 1);

    public void SwapCgosAdminPlayers() =>
        (CgosAdminWhitePlayerIndex, CgosAdminBlackPlayerIndex) = (CgosAdminBlackPlayerIndex, CgosAdminWhitePlayerIndex);

    private string GetCgosAdminWaitingPlayer(int index) =>
        index >= 0 && index < CgosAdminWaitingPlayers.Count ? CgosAdminWaitingPlayers[index] : "-";

    private int FindCgosAdminWaitingPlayerIndex(string name, int fallbackIndex)
    {
        for (var index = 0; index < CgosAdminWaitingPlayers.Count; index++)
        {
            if (CgosAdminWaitingPlayers[index].Equals(name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return CgosAdminWaitingPlayers.Count == 0 ? 0 : Math.Clamp(fallbackIndex, 0, CgosAdminWaitingPlayers.Count - 1);
    }

    private int MoveCgosAdminWaitingPlayerIndex(int index, int step)
    {
        if (CgosAdminWaitingPlayers.Count == 0)
            return 0;

        return (index + step % CgosAdminWaitingPlayers.Count + CgosAdminWaitingPlayers.Count) % CgosAdminWaitingPlayers.Count;
    }
}
