namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;

public sealed partial class GoAppSession
{
    public bool SupportsPonnukiRandomSeed(PonnukiRandomSeedRole role)
    {
        return role switch
        {
            PonnukiRandomSeedRole.Provider => HasSelectedAppProviderEngine &&
                _appProviderGameSettingSpecs.Any(spec => spec.Id == GtpEngineGuiOptions.RandomSeedId),
            PonnukiRandomSeedRole.Player1 => SupportsPonnukiPlayerRandomSeed(GoStone.Black),
            PonnukiRandomSeedRole.Player2 => SupportsPonnukiPlayerRandomSeed(GoStone.White),
            _ => false,
        };
    }

    public string GetPonnukiRandomSeedText(PonnukiRandomSeedRole role) =>
        SupportsPonnukiRandomSeed(role)
            ? GetPonnukiSeedProfile(role).GuiOptions.GetValueOrDefault(GtpEngineGuiOptions.RandomSeedId, "")
            : "";

    public void SetPonnukiRandomSeedText(PonnukiRandomSeedRole role, string value)
    {
        if (SupportsPonnukiRandomSeed(role))
            GetPonnukiSeedProfile(role).GuiOptions[GtpEngineGuiOptions.RandomSeedId] = value;
    }

    public PonnukiRandomSeedSnapshot ApplyPonnukiRandomSeedsAtStart()
    {
        var snapshot = new PonnukiRandomSeedSnapshot(
            ResolveSeed(PonnukiRandomSeedRole.Provider),
            ResolveSeed(PonnukiRandomSeedRole.Player1),
            ResolveSeed(PonnukiRandomSeedRole.Player2));
        _localMatchRandomSeeds = new LocalMatchRandomSeedSnapshot(snapshot.Player1, snapshot.Player2);
        return snapshot;
    }

    public GtpEngineProfile GetPonnukiProviderProfileForStart(int seed)
    {
        var profile = SelectedAppProviderEngine.Clone();
        profile.GuiOptions[GtpEngineGuiOptions.RandomSeedId] = seed.ToString();
        return profile;
    }

    private GtpEngineProfile GetProfileReference(GoStone stone) =>
        _gtpEngineProfiles[stone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex];

    private bool SupportsPonnukiPlayerRandomSeed(GoStone stone) =>
        GetSelectedEntryProfile(stone)?.Kind == EntryProfileKind.Computer &&
        GetProfileReference(stone).GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomSeedId);

    private GtpEngineProfile GetPonnukiSeedProfile(PonnukiRandomSeedRole role) => role switch
    {
        PonnukiRandomSeedRole.Provider => SelectedAppProviderEngine,
        PonnukiRandomSeedRole.Player1 => GetProfileReference(GoStone.Black),
        PonnukiRandomSeedRole.Player2 => GetProfileReference(GoStone.White),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    private static int NextSeed() => RandomNumberGenerator.GetInt32(1, int.MaxValue);
    private int ResolveSeed(PonnukiRandomSeedRole role) =>
        !SupportsPonnukiRandomSeed(role) ? 0 :
        int.TryParse(GetPonnukiRandomSeedText(role), out var value) ? value : NextSeed();

    public string GetPonnukiPlayerSeedLabel(GoStone stone)
    {
        var player = GetSelectedEntryProfile(stone);
        if (player is null) return stone == GoStone.Black ? "Black Player" : "White Player";
        return string.IsNullOrWhiteSpace(player.Identifier)
            ? player.DisplayName
            : $"{player.DisplayName} ({player.Identifier})";
    }
}

public enum PonnukiRandomSeedRole { Provider, Player1, Player2 }
public readonly record struct PonnukiRandomSeedSnapshot(int Provider, int Player1, int Player2);
