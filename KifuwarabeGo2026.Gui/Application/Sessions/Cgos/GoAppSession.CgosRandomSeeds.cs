namespace KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;

public sealed partial class GoAppSession
{
    public bool SupportsCgosRandomSeed(GoStone stone) =>
        (stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile)?
        .GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomSeedId) == true;
    public string GetCgosRandomSeedText(GoStone stone) =>
        (stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile)?
        .GuiOptions.GetValueOrDefault(GtpEngineGuiOptions.RandomSeedId, "") ?? "";
    public void SetCgosRandomSeedText(GoStone stone, string value)
    {
        var index = stone == GoStone.Black ? SelectedCgosBlackGtpEngineIndex : SelectedCgosWhiteGtpEngineIndex;
        if (index is { } i && i >= 0 && i < _gtpEngineProfiles.Count)
            _gtpEngineProfiles[i].GuiOptions[GtpEngineGuiOptions.RandomSeedId] = value;
    }
    public GtpEngineProfile? GetCgosEngineProfileForStart(GoStone stone)
    {
        var profile = stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile;
        if (profile is null) return null;
        var clone = profile.Clone();
        if (!int.TryParse(GetCgosRandomSeedText(stone), out var seed)) seed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        clone.GuiOptions[GtpEngineGuiOptions.RandomSeedId] = seed.ToString();
        return clone;
    }
}
