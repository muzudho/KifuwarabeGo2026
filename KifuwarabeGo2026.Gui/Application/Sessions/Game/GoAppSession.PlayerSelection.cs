namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Linq;

/// <summary>ローカル対局の Player 選択ダイアログの状態と操作。</summary>
public sealed partial class GoAppSession
{
    public const int PlayerSelectionPageSize = 6;
    public bool IsPlayerSelectionDialogOpen { get; private set; }
    public PlayerSelectionPurpose PlayerSelectionPurpose { get; private set; }
    public GoStone PlayerSelectionTargetStone { get; private set; } = GoStone.Black;
    public int PlayerDialogSelectionIndex { get; private set; } = -1;
    public int PlayerSelectionPageIndex { get; private set; }

    public void OpenPlayerSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player can be selected only for black or white.");

        IsPlayerSelectionDialogOpen = true;
        PlayerSelectionPurpose = PlayerSelectionPurpose.LocalMatch;
        PlayerSelectionTargetStone = stone;
        PlayerDialogSelectionIndex = _playerProfiles.FindIndex(profile =>
            string.Equals(profile.Id, stone == GoStone.Black ? BlackPlayerProfileId : WhitePlayerProfileId, StringComparison.Ordinal));
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
    }

    public void OpenCgosPlayerSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS player can be selected only for black or white.");

        IsPlayerSelectionDialogOpen = true;
        PlayerSelectionPurpose = PlayerSelectionPurpose.Cgos;
        PlayerSelectionTargetStone = stone;
        var currentId = stone == GoStone.Black ? CgosBlackPlayerProfileId : CgosWhitePlayerProfileId;
        PlayerDialogSelectionIndex = _playerProfiles.FindIndex(profile => string.Equals(profile.Id, currentId, StringComparison.Ordinal));
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
        var playerId = _playerProfiles[PlayerDialogSelectionIndex].Id;
        var selected = PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
            ? TrySelectCgosPlayerProfile(PlayerSelectionTargetStone, playerId)
            : TrySelectPlayerProfile(PlayerSelectionTargetStone, playerId);
        if (!selected)
            return false;

        IsPlayerSelectionDialogOpen = false;
        return true;
    }

    public void CancelPlayerSelectionDialog() => IsPlayerSelectionDialogOpen = false;

    public bool CanCommitPlayerSelection =>
        PlayerDialogSelectionIndex >= 0 &&
        PlayerDialogSelectionIndex < _playerProfiles.Count &&
        (PlayerSelectionPurpose != PlayerSelectionPurpose.Cgos || CanSelectPlayerForCgos(_playerProfiles[PlayerDialogSelectionIndex]));

    public void MovePlayerSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_playerProfiles.Count / (double)PlayerSelectionPageSize));
        PlayerSelectionPageIndex = Math.Clamp(PlayerSelectionPageIndex + step, 0, pageCount - 1);
    }

    public bool AddPlayerProfile(PlayerProfileKind kind)
    {
        var engineId = "";
        if (kind == PlayerProfileKind.Computer)
        {
            if (_gtpEngineProfiles.Count == 0) return false;
            engineId = _gtpEngineProfiles[0].Id;
        }

        var ordinal = _playerProfiles.Count(profile => profile.Kind == kind) + 1;
        var player = new PlayerProfile
        {
            DisplayName = kind == PlayerProfileKind.Human ? $"New Human {ordinal}" : $"New Computer {ordinal}",
            Identifier = "",
            Kind = kind,
            EngineProfileId = engineId,
        };
        _playerProfiles.Add(player);
        AddDefaultTargetProfiles(player);
        PlayerDialogSelectionIndex = _playerProfiles.Count - 1;
        PlayerSelectionPageIndex = PlayerDialogSelectionIndex / PlayerSelectionPageSize;
        return true;
    }

    public bool DeleteSelectedPlayerProfile()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        var removed = _playerProfiles[PlayerDialogSelectionIndex];
        if (string.Equals(removed.Id, BlackPlayerProfileId, StringComparison.Ordinal) ||
            string.Equals(removed.Id, WhitePlayerProfileId, StringComparison.Ordinal))
        {
            return false;
        }

        _playerProfiles.RemoveAt(PlayerDialogSelectionIndex);
        var stillReferencedTargetIds = _playerProfiles.SelectMany(profile => profile.TargetProfileIds).ToHashSet(StringComparer.Ordinal);
        _targetProfiles.RemoveAll(target => removed.TargetProfileIds.Contains(target.Id, StringComparer.Ordinal) && !stillReferencedTargetIds.Contains(target.Id));
        PlayerDialogSelectionIndex = Math.Min(PlayerDialogSelectionIndex, _playerProfiles.Count - 1);
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
        return true;
    }

    public bool CanDeleteSelectedPlayerProfile =>
        PlayerDialogSelectionIndex >= 0 && PlayerDialogSelectionIndex < _playerProfiles.Count &&
        !string.Equals(_playerProfiles[PlayerDialogSelectionIndex].Id, BlackPlayerProfileId, StringComparison.Ordinal) &&
        !string.Equals(_playerProfiles[PlayerDialogSelectionIndex].Id, WhitePlayerProfileId, StringComparison.Ordinal);

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

    private bool CanSelectPlayerForCgos(PlayerProfile player) =>
        player.Kind == PlayerProfileKind.Computer &&
        FindGtpEngineIndex(player.EngineProfileId) >= 0 &&
        GetPlayerTargetProfiles(player.Id).Any(target =>
            string.Equals(target.ConnectionProfileId, SelectedCgosConnectionProfile.Id, StringComparison.Ordinal));

    private void AddDefaultTargetProfiles(PlayerProfile player)
    {
        var localMatch = new TargetProfile { DisplayName = "LocalMatch", LoginName = player.Identifier };
        _targetProfiles.Add(localMatch);
        player.TargetProfileIds.Add(localMatch.Id);

        if (player.Kind != PlayerProfileKind.Computer || _cgosConnectionProfiles.Count == 0)
            return;

        var engineIndex = FindGtpEngineIndex(player.EngineProfileId);
        if (engineIndex < 0) return;
        var engine = _gtpEngineProfiles[engineIndex];
        var cgos = new TargetProfile
        {
            DisplayName = "CGOS",
            ConnectionProfileId = SelectedCgosConnectionProfile.Id,
            LoginName = engine.DefaultCgosLoginName,
            LoginPass = engine.DefaultCgosPlainTextPassword,
        };
        _targetProfiles.Add(cgos);
        player.TargetProfileIds.Add(cgos.Id);
    }
}

public enum PlayerSelectionPurpose
{
    LocalMatch,
    Cgos,
}
