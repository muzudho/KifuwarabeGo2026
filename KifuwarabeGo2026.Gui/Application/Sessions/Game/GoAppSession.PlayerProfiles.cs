namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ローカル対局で黒白に割り当てる PlayerProfile の参照を管理する。
/// 既存の Human/Computer・GTP 選択 API は、段階移行が完了するまで同居させる。
/// </summary>
public sealed partial class GoAppSession
{
    private readonly List<PlayerProfile> _playerProfiles = [];
    private readonly List<TargetProfile> _targetProfiles = [];

    public IReadOnlyList<PlayerProfile> PlayerProfiles => _playerProfiles;

    public IReadOnlyList<TargetProfile> TargetProfiles => _targetProfiles;

    public CatalogOrderEditor<PlayerProfile> PlayerOrderEditor { get; } = new();

    public string BlackPlayerProfileId { get; private set; } = "";

    public string WhitePlayerProfileId { get; private set; } = "";

    public void SetPlayerProfiles(IEnumerable<PlayerProfile> profiles)
    {
        _playerProfiles.Clear();
        _playerProfiles.AddRange(profiles.Select(profile => profile.Clone()));

        BlackPlayerProfileId = FindCompatiblePlayerId(GoStone.Black, GoPlayerKind.Human);
        WhitePlayerProfileId = FindCompatiblePlayerId(GoStone.White, GoPlayerKind.Computer);
        ApplySelectedPlayerProfile(GoStone.Black);
        ApplySelectedPlayerProfile(GoStone.White);
    }

    public void SetTargetProfiles(IEnumerable<TargetProfile> profiles)
    {
        _targetProfiles.Clear();
        _targetProfiles.AddRange(profiles.Select(profile => profile.Clone()));
        ApplyCgosTargetCredentials(GoStone.Black);
        ApplyCgosTargetCredentials(GoStone.White);
    }

    public IReadOnlyList<TargetProfile> GetPlayerTargetProfiles(string playerProfileId) =>
        FindPlayerProfile(playerProfileId) is not { } player
            ? Array.Empty<TargetProfile>()
            : _targetProfiles
                .Where(target => player.TargetProfileIds.Contains(target.Id, StringComparer.Ordinal))
                .Select(target => target.Clone())
                .ToArray();

    public TargetProfile? GetSelectedCgosTargetProfile(GoStone stone)
    {
        var engine = stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile;
        if (engine is null || _cgosConnectionProfiles.Count == 0)
            return null;

        var connectionId = SelectedCgosConnectionProfile.Id;
        var selectedPlayerId = stone == GoStone.Black ? CgosBlackPlayerProfileId : CgosWhitePlayerProfileId;
        var players = FindPlayerProfile(selectedPlayerId) is { } selectedPlayer
            ? new[] { selectedPlayer }
            : _playerProfiles.Where(player => player.Kind == PlayerProfileKind.Computer &&
                                               string.Equals(player.EngineProfileId, engine.Id, StringComparison.Ordinal));
        return players
            .SelectMany(player => GetPlayerTargetProfiles(player.Id))
            .FirstOrDefault(target => string.Equals(target.ConnectionProfileId, connectionId, StringComparison.Ordinal));
    }

    public void ApplyCgosTargetCredentials(GoStone stone)
    {
        var target = GetSelectedCgosTargetProfile(stone);
        if (target is not null)
            SetCgosPlayerCredentials(stone, target.LoginName, target.LoginPass);
    }

    public PlayerProfile? GetSelectedPlayerProfile(GoStone stone) =>
        FindPlayerProfile(stone == GoStone.Black ? BlackPlayerProfileId : WhitePlayerProfileId);

    public bool TrySelectPlayerProfile(GoStone stone, string playerProfileId)
    {
        if (stone is not (GoStone.Black or GoStone.White))
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player can be selected only for black or white.");

        var profile = FindPlayerProfile(playerProfileId);
        if (profile is null ||
            (profile.Kind == PlayerProfileKind.Computer && FindGtpEngineIndex(profile.EngineProfileId) < 0))
        {
            return false;
        }

        if (stone == GoStone.Black)
            BlackPlayerProfileId = profile.Id;
        else
            WhitePlayerProfileId = profile.Id;

        ApplySelectedPlayerProfile(stone);
        return true;
    }

    public GtpEngineProfile? GetSelectedPlayerEngineProfile(GoStone stone)
    {
        var player = GetSelectedPlayerProfile(stone);
        if (player?.Kind != PlayerProfileKind.Computer)
            return null;

        var index = FindGtpEngineIndex(player.EngineProfileId);
        return index >= 0 ? _gtpEngineProfiles[index].Clone() : null;
    }

    private void ApplySelectedPlayerProfile(GoStone stone)
    {
        var profile = GetSelectedPlayerProfile(stone);
        if (profile is null)
            return;

        SetPlayerKind(stone, profile.Kind == PlayerProfileKind.Human ? GoPlayerKind.Human : GoPlayerKind.Computer);
        if (profile.Kind == PlayerProfileKind.Human)
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

    private string FindCompatiblePlayerId(GoStone stone, GoPlayerKind fallbackKind)
    {
        var currentId = stone == GoStone.Black ? BlackPlayerProfileId : WhitePlayerProfileId;
        if (FindPlayerProfile(currentId) is not null)
            return currentId;

        var exactDefaultName = stone == GoStone.Black ? "Black Player" : "White Player";
        var profile = _playerProfiles.FirstOrDefault(candidate =>
                          candidate.Kind == (fallbackKind == GoPlayerKind.Human ? PlayerProfileKind.Human : PlayerProfileKind.Computer) &&
                          string.Equals(candidate.DisplayName, exactDefaultName, StringComparison.Ordinal)) ??
                      _playerProfiles.FirstOrDefault(candidate =>
                          candidate.Kind == (fallbackKind == GoPlayerKind.Human ? PlayerProfileKind.Human : PlayerProfileKind.Computer));
        return profile?.Id ?? "";
    }

    private PlayerProfile? FindPlayerProfile(string id) =>
        _playerProfiles.FirstOrDefault(profile => string.Equals(profile.Id, id, StringComparison.Ordinal));

    private int FindGtpEngineIndex(string engineProfileId) =>
        _gtpEngineProfiles.FindIndex(profile => string.Equals(profile.Id, engineProfileId, StringComparison.Ordinal));

    public void OpenPlayerOrderEditor() =>
        PlayerOrderEditor.Open(_playerProfiles, PlayerDialogSelectionIndex, PlayerSelectionPageSize);

    public void CancelPlayerOrderEditor() => PlayerOrderEditor.Cancel();

    public IReadOnlyList<PlayerProfile> CommitPlayerOrderEditor()
    {
        var ordered = PlayerOrderEditor.Commit();
        _playerProfiles.Clear();
        _playerProfiles.AddRange(ordered.Select(profile => profile.Clone()));
        PlayerDialogSelectionIndex = Math.Clamp(PlayerDialogSelectionIndex, 0, _playerProfiles.Count - 1);
        return _playerProfiles;
    }
}
