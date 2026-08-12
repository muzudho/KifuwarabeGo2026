namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>ローカル対局の Player 選択ダイアログの状態と操作。</summary>
public sealed partial class GoAppSession
{
    public const int PlayerSelectionPageSize = 6;
    public bool IsPlayerSelectionDialogOpen { get; private set; }
    public PlayerSelectionPurpose PlayerSelectionPurpose { get; private set; }
    public GoStone PlayerSelectionTargetStone { get; private set; } = GoStone.Black;
    public int PlayerDialogSelectionIndex { get; private set; } = -1;
    public int ClientIdentityDialogSelectionIndex { get; private set; } = -1;
    public int PlayerSelectionPageIndex { get; private set; }

    public void OpenPlayerSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player can be selected only for black or white.");

        IsPlayerSelectionDialogOpen = true;
        PlayerSelectionPurpose = PlayerSelectionPurpose.LocalMatch;
        PlayerSelectionTargetStone = stone;
        PlayerDialogSelectionIndex = _playerProfiles.FindIndex(profile =>
            string.Equals(profile.Id, stone == GoStone.Black ? BlackEntryProfileId : WhiteEntryProfileId, StringComparison.Ordinal));
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
        SelectDialogDefaultClientIdentity();
    }

    public void OpenCgosPlayerSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS player can be selected only for black or white.");

        IsPlayerSelectionDialogOpen = true;
        PlayerSelectionPurpose = PlayerSelectionPurpose.Cgos;
        PlayerSelectionTargetStone = stone;
        var currentId = stone == GoStone.Black ? CgosBlackEntryProfileId : CgosWhiteEntryProfileId;
        PlayerDialogSelectionIndex = _playerProfiles.FindIndex(profile => string.Equals(profile.Id, currentId, StringComparison.Ordinal));
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
        SelectDialogDefaultClientIdentity();
    }

    public void SelectPlayerDialogItem(int index)
    {
        if (index < 0 || index >= _playerProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Player index is out of range.");
        PlayerDialogSelectionIndex = index;
        SelectDialogDefaultClientIdentity();
    }

    public IReadOnlyList<ClientIdentityProfile> GetPlayerSelectionClientIdentities()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return Array.Empty<ClientIdentityProfile>();

        var identities = GetPlayerClientIdentityProfiles(_playerProfiles[PlayerDialogSelectionIndex].Id);
        return PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
            ? identities.Where(identity => string.Equals(identity.ConnectionProfileId, SelectedCgosConnectionProfile.Id, StringComparison.Ordinal)).ToArray()
            : identities.Where(identity => string.IsNullOrEmpty(identity.ConnectionProfileId)).ToArray();
    }

    public void SelectPlayerSelectionClientIdentity(int index)
    {
        if (index < 0 || index >= GetPlayerSelectionClientIdentities().Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        ClientIdentityDialogSelectionIndex = index;
    }

    public bool CommitPlayerSelectionDialog()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;
        var playerId = _playerProfiles[PlayerDialogSelectionIndex].Id;
        var identities = GetPlayerSelectionClientIdentities();
        if (ClientIdentityDialogSelectionIndex < 0 || ClientIdentityDialogSelectionIndex >= identities.Count)
            return false;

        var selected = PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
            ? TrySelectCgosEntryProfile(PlayerSelectionTargetStone, playerId) &&
              TrySelectCgosClientIdentityProfile(PlayerSelectionTargetStone, identities[ClientIdentityDialogSelectionIndex].Id)
            : TrySelectEntryProfile(PlayerSelectionTargetStone, playerId) &&
              TrySelectLocalMatchClientIdentityProfile(PlayerSelectionTargetStone, identities[ClientIdentityDialogSelectionIndex].Id);
        if (!selected)
            return false;

        IsPlayerSelectionDialogOpen = false;
        return true;
    }

    public void CancelPlayerSelectionDialog() => IsPlayerSelectionDialogOpen = false;

    public bool CanCommitPlayerSelection =>
        PlayerDialogSelectionIndex >= 0 &&
        PlayerDialogSelectionIndex < _playerProfiles.Count &&
        GetPlayerSelectionClientIdentities().Count > 0 &&
        ClientIdentityDialogSelectionIndex >= 0 &&
        ClientIdentityDialogSelectionIndex < GetPlayerSelectionClientIdentities().Count;

    private void SelectDialogDefaultClientIdentity()
    {
        var identities = GetPlayerSelectionClientIdentities();
        var selectedId = PlayerSelectionPurpose == PlayerSelectionPurpose.Cgos
            ? PlayerSelectionTargetStone == GoStone.Black ? CgosBlackClientIdentityProfileId : CgosWhiteClientIdentityProfileId
            : PlayerSelectionTargetStone == GoStone.Black ? BlackLocalMatchClientIdentityProfileId : WhiteLocalMatchClientIdentityProfileId;
        ClientIdentityDialogSelectionIndex = Math.Max(0, identities.ToList().FindIndex(identity => string.Equals(identity.Id, selectedId, StringComparison.Ordinal)));
    }

    public void MovePlayerSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_playerProfiles.Count / (double)PlayerSelectionPageSize));
        PlayerSelectionPageIndex = Math.Clamp(PlayerSelectionPageIndex + step, 0, pageCount - 1);
    }

    public bool AddEntryProfile(EntryProfileKind kind)
    {
        var engineId = "";
        if (kind == EntryProfileKind.Computer)
        {
            if (_gtpEngineProfiles.Count == 0) return false;
            engineId = _gtpEngineProfiles[0].Id;
        }

        var ordinal = _playerProfiles.Count(profile => profile.Kind == kind) + 1;
        var player = new EntryProfile
        {
            DisplayName = kind == EntryProfileKind.Human ? $"New Human {ordinal}" : $"New Computer {ordinal}",
            Identifier = "",
            Kind = kind,
            EngineProfileId = engineId,
        };
        _playerProfiles.Add(player);
        AddDefaultClientIdentityProfiles(player);
        PlayerDialogSelectionIndex = _playerProfiles.Count - 1;
        PlayerSelectionPageIndex = PlayerDialogSelectionIndex / PlayerSelectionPageSize;
        return true;
    }

    public bool DeleteSelectedEntryProfile()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        var removed = _playerProfiles[PlayerDialogSelectionIndex];
        if (string.Equals(removed.Id, BlackEntryProfileId, StringComparison.Ordinal) ||
            string.Equals(removed.Id, WhiteEntryProfileId, StringComparison.Ordinal))
        {
            return false;
        }

        _playerProfiles.RemoveAt(PlayerDialogSelectionIndex);
        var stillReferencedTargetIds = _playerProfiles.SelectMany(profile => profile.ClientIdentityProfileIds).ToHashSet(StringComparer.Ordinal);
        _clientIdentityProfiles.RemoveAll(target => removed.ClientIdentityProfileIds.Contains(target.Id, StringComparer.Ordinal) && !stillReferencedTargetIds.Contains(target.Id));
        PlayerDialogSelectionIndex = Math.Min(PlayerDialogSelectionIndex, _playerProfiles.Count - 1);
        PlayerSelectionPageIndex = Math.Max(0, PlayerDialogSelectionIndex) / PlayerSelectionPageSize;
        return true;
    }

    public bool CanDeleteSelectedEntryProfile =>
        PlayerDialogSelectionIndex >= 0 && PlayerDialogSelectionIndex < _playerProfiles.Count &&
        !string.Equals(_playerProfiles[PlayerDialogSelectionIndex].Id, BlackEntryProfileId, StringComparison.Ordinal) &&
        !string.Equals(_playerProfiles[PlayerDialogSelectionIndex].Id, WhiteEntryProfileId, StringComparison.Ordinal);

    public string GetPlayerSelectionDetail(int index)
    {
        if (index < 0 || index >= _playerProfiles.Count)
            return "";

        var player = _playerProfiles[index];
        if (player.Kind == EntryProfileKind.Human)
            return "";

        var engineIndex = FindGtpEngineIndex(player.EngineProfileId);
        return engineIndex >= 0 ? _gtpEngineProfiles[engineIndex].DisplayName : "ENGINE NOT FOUND";
    }

    private bool CanSelectPlayerForCgos(EntryProfile player) =>
        player.Kind == EntryProfileKind.Computer &&
        FindGtpEngineIndex(player.EngineProfileId) >= 0 &&
        GetPlayerClientIdentityProfiles(player.Id).Any(target =>
            string.Equals(target.ConnectionProfileId, SelectedCgosConnectionProfile.Id, StringComparison.Ordinal));

    private void AddDefaultClientIdentityProfiles(EntryProfile player)
    {
        var localMatch = new ClientIdentityProfile { DisplayName = "LocalMatch", LoginName = new string(player.Identifier.Where(character => !char.IsWhiteSpace(character)).ToArray()) };
        _clientIdentityProfiles.Add(localMatch);
        player.ClientIdentityProfileIds.Add(localMatch.Id);

        if (player.Kind != EntryProfileKind.Computer || _cgosConnectionProfiles.Count == 0)
            return;

        var engineIndex = FindGtpEngineIndex(player.EngineProfileId);
        if (engineIndex < 0) return;
        var engine = _gtpEngineProfiles[engineIndex];
        var cgos = new ClientIdentityProfile
        {
            DisplayName = "OnlineMatch (CGOS)",
            ConnectionProfileId = SelectedCgosConnectionProfile.Id,
            LoginName = engine.DefaultCgosLoginName,
            LoginPass = engine.DefaultCgosPlainTextPassword,
        };
        _clientIdentityProfiles.Add(cgos);
        player.ClientIdentityProfileIds.Add(cgos.Id);
    }
}

public enum PlayerSelectionPurpose
{
    LocalMatch,
    Cgos,
}
