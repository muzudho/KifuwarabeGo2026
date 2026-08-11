namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Security.Cryptography;

public sealed partial class GoAppSession
{
    public bool PonnukiProviderSeedAutoChange { get; private set; }
    public bool PonnukiBlackPlayerSeedAutoChange { get; private set; }
    public bool PonnukiWhitePlayerSeedAutoChange { get; private set; }
    public bool CanAutoChangePonnukiPlayer1Seed => GetSelectedPlayerProfile(GoStone.Black)?.Kind == PlayerProfileKind.Computer;
    public bool CanAutoChangePonnukiPlayer2Seed => GetSelectedPlayerProfile(GoStone.White)?.Kind == PlayerProfileKind.Computer;

    public void TogglePonnukiRandomSeedAutoChange(PonnukiRandomSeedRole role)
    {
        switch (role)
        {
            case PonnukiRandomSeedRole.Provider: PonnukiProviderSeedAutoChange = !PonnukiProviderSeedAutoChange; break;
            case PonnukiRandomSeedRole.Player1: PonnukiBlackPlayerSeedAutoChange = !PonnukiBlackPlayerSeedAutoChange; break;
            case PonnukiRandomSeedRole.Player2: PonnukiWhitePlayerSeedAutoChange = !PonnukiWhitePlayerSeedAutoChange; break;
        }
    }

    public PonnukiRandomSeedSnapshot ApplyPonnukiRandomSeedsAtStart()
    {
        var provider = SelectedAppProviderEngine;
        var black = GetProfileReference(GoStone.Black);
        var white = GetProfileReference(GoStone.White);
        if (PonnukiProviderSeedAutoChange) provider.GuiOptions["RandomSeed"] = NextSeed().ToString();
        if (CanAutoChangePonnukiPlayer1Seed && PonnukiBlackPlayerSeedAutoChange) black.GuiOptions["RandomSeed"] = NextSeed().ToString();
        if (CanAutoChangePonnukiPlayer2Seed && PonnukiWhitePlayerSeedAutoChange) white.GuiOptions["RandomSeed"] = NextSeed().ToString();
        return new PonnukiRandomSeedSnapshot(ReadSeed(provider), ReadSeed(black), ReadSeed(white));
    }

    private GtpEngineProfile GetProfileReference(GoStone stone) =>
        _gtpEngineProfiles[stone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex];

    private static int NextSeed() => RandomNumberGenerator.GetInt32(1, int.MaxValue);
    private static int ReadSeed(GtpEngineProfile profile) =>
        profile.GuiOptions.TryGetValue("RandomSeed", out var text) && int.TryParse(text, out var value) ? value : 0;

    public string GetPonnukiPlayerSeedLabel(GoStone stone)
    {
        var player = GetSelectedPlayerProfile(stone);
        if (player is null) return stone == GoStone.Black ? "Black Player" : "White Player";
        return string.IsNullOrWhiteSpace(player.Identifier)
            ? player.DisplayName
            : $"{player.DisplayName} ({player.Identifier})";
    }
}

public enum PonnukiRandomSeedRole { Provider, Player1, Player2 }
public readonly record struct PonnukiRandomSeedSnapshot(int Provider, int Player1, int Player2);
