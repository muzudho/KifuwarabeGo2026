namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局の Player 選択ダイアログの状態と操作。</summary>
public sealed partial class GoAppSession
{
    public const int PlayerSelectionPageSize = 6;
    public bool IsPlayerSelectionDialogOpen { get; private set; }
    public GoStone PlayerSelectionTargetStone { get; private set; } = GoStone.Black;
    public int PlayerDialogSelectionIndex { get; private set; } = -1;
    public int PlayerSelectionPageIndex { get; private set; }

    public void OpenPlayerSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player can be selected only for black or white.");

        IsPlayerSelectionDialogOpen = true;
        PlayerSelectionTargetStone = stone;
        PlayerDialogSelectionIndex = _playerProfiles.FindIndex(profile =>
            string.Equals(profile.Id, stone == GoStone.Black ? BlackPlayerProfileId : WhitePlayerProfileId, StringComparison.Ordinal));
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
    }

    public void SelectPlayerDialogItem(int index)
    {
        if (index < 0 || index >= _playerProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Player index is out of range.");
        PlayerDialogSelectionIndex = index;
    }

    public bool CommitPlayerSelectionDialog()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;
        if (!TrySelectPlayerProfile(PlayerSelectionTargetStone, _playerProfiles[PlayerDialogSelectionIndex].Id))
            return false;

        IsPlayerSelectionDialogOpen = false;
        return true;
    }

    public void CancelPlayerSelectionDialog() => IsPlayerSelectionDialogOpen = false;

    public void MovePlayerSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_playerProfiles.Count / (double)PlayerSelectionPageSize));
        PlayerSelectionPageIndex = Math.Clamp(PlayerSelectionPageIndex + step, 0, pageCount - 1);
    }

    public string GetPlayerSelectionDetail(int index)
    {
        if (index < 0 || index >= _playerProfiles.Count)
            return "";

        var player = _playerProfiles[index];
        if (player.Kind == PlayerProfileKind.Human)
            return "HUMAN";

        var engineIndex = FindGtpEngineIndex(player.EngineProfileId);
        return engineIndex >= 0 ? $"COMPUTER  /  {_gtpEngineProfiles[engineIndex].DisplayName}" : "COMPUTER  /  ENGINE NOT FOUND";
    }
}
