namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

public sealed partial class GoAppSession
{
    public bool SupportsLocalMatchRandomSeed(GoStone stone)
    {
        if (GetPlayerKind(stone) != GoPlayerKind.Computer) return false;
        return GetGtpEngineProfile(stone).GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomSeedId);
    }

    public string GetLocalMatchRandomSeedText(GoStone stone) =>
        SupportsLocalMatchRandomSeed(stone)
            ? GetGtpEngineProfile(stone).GuiOptions.GetValueOrDefault(GtpEngineGuiOptions.RandomSeedId, "")
            : "";

    public void SetLocalMatchRandomSeedText(GoStone stone, string value)
    {
        if (!SupportsLocalMatchRandomSeed(stone)) return;
        var index = stone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex;
        _gtpEngineProfiles[Math.Clamp(index, 0, _gtpEngineProfiles.Count - 1)]
            .GuiOptions[GtpEngineGuiOptions.RandomSeedId] = value;
    }

    public LocalMatchRandomSeedSnapshot ApplyLocalMatchRandomSeedsAtStart()
    {
        var black = ResolveLocalMatchSeed(GoStone.Black);
        var white = ResolveLocalMatchSeed(GoStone.White);
        _localMatchRandomSeeds = new LocalMatchRandomSeedSnapshot(black, white);
        return _localMatchRandomSeeds;
    }

    public IReadOnlyDictionary<string, string> GetLocalMatchEngineGuiOptions(GoStone stone)
    {
        var options = new Dictionary<string, string>(GetGtpEngineProfile(stone).GuiOptions);
        var seed = stone == GoStone.Black ? _localMatchRandomSeeds.Black : _localMatchRandomSeeds.White;
        if (seed is { } value)
            options[GtpEngineGuiOptions.RandomSeedId] = value.ToString();
        return options;
    }

    private LocalMatchRandomSeedSnapshot _localMatchRandomSeeds;

    private int? ResolveLocalMatchSeed(GoStone stone)
    {
        if (!SupportsLocalMatchRandomSeed(stone)) return null;
        return int.TryParse(GetLocalMatchRandomSeedText(stone), out var seed)
            ? seed
            : RandomNumberGenerator.GetInt32(1, int.MaxValue);
    }
}

public readonly record struct LocalMatchRandomSeedSnapshot(int? Black, int? White);
