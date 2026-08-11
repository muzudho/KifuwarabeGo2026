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

    public IReadOnlyList<PlayerProfile> PlayerProfiles => _playerProfiles;

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
}
