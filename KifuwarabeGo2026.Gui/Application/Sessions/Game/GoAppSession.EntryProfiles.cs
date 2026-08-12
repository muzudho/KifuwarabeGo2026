namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ローカル対局で黒白に割り当てる EntryProfile の参照を管理する。
/// 既存の Human/Computer・GTP 選択 API は、段階移行が完了するまで同居させる。
/// </summary>
public sealed partial class GoAppSession
{
    private readonly List<EntryProfile> _playerProfiles = [];
    private readonly List<ClientIdentityProfile> _clientIdentityProfiles = [];

    public IReadOnlyList<EntryProfile> EntryProfiles => _playerProfiles;

    public IReadOnlyList<ClientIdentityProfile> ClientIdentityProfiles => _clientIdentityProfiles;

    public CatalogOrderEditor<EntryProfile> PlayerOrderEditor { get; } = new();

    public string BlackEntryProfileId { get; private set; } = "";

    public string WhiteEntryProfileId { get; private set; } = "";

    public string BlackLocalMatchClientIdentityProfileId { get; private set; } = "";

    public string WhiteLocalMatchClientIdentityProfileId { get; private set; } = "";

    public void SetEntryProfiles(IEnumerable<EntryProfile> profiles)
    {
        _playerProfiles.Clear();
        _playerProfiles.AddRange(profiles.Select(profile => profile.Clone()));

        BlackEntryProfileId = FindCompatiblePlayerId(GoStone.Black, GoPlayerKind.Human);
        WhiteEntryProfileId = FindCompatiblePlayerId(GoStone.White, GoPlayerKind.Computer);
        ApplySelectedEntryProfile(GoStone.Black);
        ApplySelectedEntryProfile(GoStone.White);
    }

    public void SetClientIdentityProfiles(IEnumerable<ClientIdentityProfile> profiles)
    {
        _clientIdentityProfiles.Clear();
        _clientIdentityProfiles.AddRange(profiles.Select(profile => profile.Clone()));
        ApplyCgosClientIdentityCredentials(GoStone.Black);
        ApplyCgosClientIdentityCredentials(GoStone.White);
    }

    public IReadOnlyList<ClientIdentityProfile> GetPlayerClientIdentityProfiles(string playerProfileId) =>
        FindEntryProfile(playerProfileId) is not { } player
            ? Array.Empty<ClientIdentityProfile>()
            : player.ClientIdentityProfileIds
                .Select(id => _clientIdentityProfiles.FirstOrDefault(target => string.Equals(target.Id, id, StringComparison.Ordinal)))
                .OfType<ClientIdentityProfile>()
                .Select(target => target.Clone())
                .ToArray();

    public ClientIdentityProfile? GetSelectedCgosClientIdentityProfile(GoStone stone)
    {
        var engine = stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile;
        if (engine is null || _cgosConnectionProfiles.Count == 0)
            return null;

        var connectionId = SelectedCgosConnectionProfile.Id;
        var selectedPlayerId = stone == GoStone.Black ? CgosBlackEntryProfileId : CgosWhiteEntryProfileId;
        var players = FindEntryProfile(selectedPlayerId) is { } selectedPlayer
            ? new[] { selectedPlayer }
            : _playerProfiles.Where(player => player.Kind == EntryProfileKind.Computer &&
                                               string.Equals(player.EngineProfileId, engine.Id, StringComparison.Ordinal));
        var selectedClientIdentityId = stone == GoStone.Black ? CgosBlackClientIdentityProfileId : CgosWhiteClientIdentityProfileId;
        var selectedClientIdentity = players
            .SelectMany(player => GetPlayerClientIdentityProfiles(player.Id))
            .FirstOrDefault(target => string.Equals(target.Id, selectedClientIdentityId, StringComparison.Ordinal) &&
                                      string.Equals(target.ConnectionProfileId, connectionId, StringComparison.Ordinal));
        return selectedClientIdentity ?? players
            .SelectMany(player => GetPlayerClientIdentityProfiles(player.Id))
            .FirstOrDefault(target => string.Equals(target.ConnectionProfileId, connectionId, StringComparison.Ordinal));
    }

    /// <summary>
    /// 現在の CGOS 接続先で使う既定 Target を表示用に返す。
    /// 同じ接続先の Target が複数ある場合も、Player 内の並び順で最初のものが既定となる。
    /// </summary>
    public string GetSelectedCgosClientIdentitySummary(GoStone stone)
    {
        var target = GetSelectedCgosClientIdentityProfile(stone);
        return target is null
            ? "NO DEFAULT TARGET"
            : $"DEFAULT TARGET: {target.DisplayName} / {target.LoginName}";
    }

    /// <summary>LocalMatch 用に既定となる Target。Player 内の先頭の LocalMatch Target を使う。</summary>
    public ClientIdentityProfile? GetSelectedLocalMatchClientIdentityProfile(GoStone stone) =>
        GetSelectedEntryProfile(stone) is not { } player
            ? null
            : GetPlayerClientIdentityProfiles(player.Id).FirstOrDefault(target =>
                  string.Equals(target.Id, stone == GoStone.Black ? BlackLocalMatchClientIdentityProfileId : WhiteLocalMatchClientIdentityProfileId, StringComparison.Ordinal))
              ?? GetPlayerClientIdentityProfiles(player.Id)
                  .FirstOrDefault(target => string.IsNullOrEmpty(target.ConnectionProfileId));

    /// <summary>LocalMatch のファイル名など、外部へ提示する名前を返す。</summary>
    public string GetLocalMatchPresentedName(GoStone stone)
    {
        var targetName = GetSelectedLocalMatchClientIdentityProfile(stone)?.LoginName;
        if (!string.IsNullOrWhiteSpace(targetName))
            return targetName;

        var player = GetSelectedEntryProfile(stone);
        return !string.IsNullOrWhiteSpace(player?.Identifier)
            ? player.Identifier
            : player?.DisplayName ?? GetLocalPlayerName(stone);
    }

    public void ApplyCgosClientIdentityCredentials(GoStone stone)
    {
        var target = GetSelectedCgosClientIdentityProfile(stone);
        if (target is not null)
            SetCgosPlayerCredentials(stone, target.LoginName, target.LoginPass);
    }

    public EntryProfile? GetSelectedEntryProfile(GoStone stone) =>
        FindEntryProfile(stone == GoStone.Black ? BlackEntryProfileId : WhiteEntryProfileId);

    public bool TrySelectEntryProfile(GoStone stone, string playerProfileId)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player can be selected only for black or white.");

        var profile = FindEntryProfile(playerProfileId);
        if (profile is null ||
            (profile.Kind == EntryProfileKind.Computer && FindGtpEngineIndex(profile.EngineProfileId) < 0))
        {
            return false;
        }

        if (stone == GoStone.Black)
            BlackEntryProfileId = profile.Id;
        else
            WhiteEntryProfileId = profile.Id;

        SetDefaultLocalMatchClientIdentity(stone, profile);
        ApplySelectedEntryProfile(stone);
        return true;
    }

    public GtpEngineProfile? GetSelectedPlayerEngineProfile(GoStone stone)
    {
        var player = GetSelectedEntryProfile(stone);
        if (player?.Kind != EntryProfileKind.Computer)
            return null;

        var index = FindGtpEngineIndex(player.EngineProfileId);
        return index >= 0 ? _gtpEngineProfiles[index].Clone() : null;
    }

    private void ApplySelectedEntryProfile(GoStone stone)
    {
        var profile = GetSelectedEntryProfile(stone);
        if (profile is null)
            return;

        SetPlayerKind(stone, profile.Kind == EntryProfileKind.Human ? GoPlayerKind.Human : GoPlayerKind.Computer);
        if (profile.Kind == EntryProfileKind.Human)
        {
            if (stone == GoStone.Black)
                BlackHumanPlayerName = profile.DisplayName;
            else
                WhiteHumanPlayerName = profile.DisplayName;
            return;
        }

        var engineIndex = FindGtpEngineIndex(profile.EngineProfileId);
        if (engineIndex >= 0)
            SelectGtpEngine(stone, engineIndex);
    }

    public bool TrySelectLocalMatchClientIdentityProfile(GoStone stone, string targetProfileId)
    {
        var player = GetSelectedEntryProfile(stone);
        if (player is null || !GetPlayerClientIdentityProfiles(player.Id).Any(target =>
                string.Equals(target.Id, targetProfileId, StringComparison.Ordinal) &&
                string.IsNullOrEmpty(target.ConnectionProfileId)))
            return false;

        if (stone == GoStone.Black) BlackLocalMatchClientIdentityProfileId = targetProfileId;
        else if (stone == GoStone.White) WhiteLocalMatchClientIdentityProfileId = targetProfileId;
        else return false;
        return true;
    }

    private void SetDefaultLocalMatchClientIdentity(GoStone stone, EntryProfile profile)
    {
        var targetId = GetPlayerClientIdentityProfiles(profile.Id)
            .FirstOrDefault(target => string.IsNullOrEmpty(target.ConnectionProfileId))?.Id ?? "";
        if (stone == GoStone.Black) BlackLocalMatchClientIdentityProfileId = targetId;
        else WhiteLocalMatchClientIdentityProfileId = targetId;
    }

    private string FindCompatiblePlayerId(GoStone stone, GoPlayerKind fallbackKind)
    {
        var currentId = stone == GoStone.Black ? BlackEntryProfileId : WhiteEntryProfileId;
        if (FindEntryProfile(currentId) is not null)
            return currentId;

        var exactDefaultName = stone == GoStone.Black ? "Black Player" : "White Player";
        var profile = _playerProfiles.FirstOrDefault(candidate =>
                          candidate.Kind == (fallbackKind == GoPlayerKind.Human ? EntryProfileKind.Human : EntryProfileKind.Computer) &&
                          string.Equals(candidate.DisplayName, exactDefaultName, StringComparison.Ordinal)) ??
                      _playerProfiles.FirstOrDefault(candidate =>
                          candidate.Kind == (fallbackKind == GoPlayerKind.Human ? EntryProfileKind.Human : EntryProfileKind.Computer));
        return profile?.Id ?? "";
    }

    private EntryProfile? FindEntryProfile(string id) =>
        _playerProfiles.FirstOrDefault(profile => string.Equals(profile.Id, id, StringComparison.Ordinal));

    private int FindGtpEngineIndex(string engineProfileId) =>
        _gtpEngineProfiles.FindIndex(profile => string.Equals(profile.Id, engineProfileId, StringComparison.Ordinal));

    public void OpenPlayerOrderEditor() =>
        PlayerOrderEditor.Open(_playerProfiles, PlayerDialogSelectionIndex, PlayerSelectionPageSize);

    public void CancelPlayerOrderEditor() => PlayerOrderEditor.Cancel();

    public IReadOnlyList<EntryProfile> CommitPlayerOrderEditor()
    {
        var ordered = PlayerOrderEditor.Commit();
        _playerProfiles.Clear();
        _playerProfiles.AddRange(ordered.Select(profile => profile.Clone()));
        PlayerDialogSelectionIndex = Math.Clamp(PlayerDialogSelectionIndex, 0, _playerProfiles.Count - 1);
        return _playerProfiles;
    }
}
