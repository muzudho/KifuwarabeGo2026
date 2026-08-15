namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

public sealed partial class GoAppSession
{
    public bool LocalMatchBlackSeedAutoChange { get; private set; }
    public bool LocalMatchWhiteSeedAutoChange { get; private set; }

    public bool CanAutoChangeLocalMatchSeed(GoStone stone) =>
        GetPlayerKind(stone) == GoPlayerKind.Computer;

    public void ToggleLocalMatchSeedAutoChange(GoStone stone)
    {
        if (!CanAutoChangeLocalMatchSeed(stone)) return;

        if (stone == GoStone.Black)
            LocalMatchBlackSeedAutoChange = !LocalMatchBlackSeedAutoChange;
        else if (stone == GoStone.White)
            LocalMatchWhiteSeedAutoChange = !LocalMatchWhiteSeedAutoChange;
        else
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "A random seed belongs to black or white.");
    }

    public LocalMatchRandomSeedSnapshot ApplyLocalMatchRandomSeedsAtStart()
    {
        var black = ResolveLocalMatchSeed(GoStone.Black, LocalMatchBlackSeedAutoChange);
        var white = ResolveLocalMatchSeed(GoStone.White, LocalMatchWhiteSeedAutoChange);
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

    private int? ResolveLocalMatchSeed(GoStone stone, bool autoChange)
    {
        if (!CanAutoChangeLocalMatchSeed(stone)) return null;
        if (autoChange) return RandomNumberGenerator.GetInt32(1, int.MaxValue);

        var profile = GetGtpEngineProfile(stone);
        return profile.GuiOptions.TryGetValue(GtpEngineGuiOptions.RandomSeedId, out var text) &&
               int.TryParse(text, out var seed)
            ? seed
            : 0;
    }
}

public readonly record struct LocalMatchRandomSeedSnapshot(int? Black, int? White);
