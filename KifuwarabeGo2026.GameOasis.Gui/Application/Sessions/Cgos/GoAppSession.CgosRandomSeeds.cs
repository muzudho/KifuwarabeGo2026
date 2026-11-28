namespace KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;
using System.Security.Cryptography;
using System.Collections.Generic;

public sealed partial class GoAppSession
{
    public bool SupportsCgosRandomSeed(GoStone stone) =>
        (stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile)?
        .GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomSeedId) == true;
    public string GetCgosRandomSeedText(GoStone stone) =>
        GetCgosEngineWithEntryOptions(stone)?
        .GuiOptions.GetValueOrDefault(GtpEngineGuiOptions.RandomSeedId, "") ?? "";
    public void SetCgosRandomSeedText(GoStone stone, string value)
    {
        var index = stone == GoStone.Black ? SelectedCgosBlackGtpEngineIndex : SelectedCgosWhiteGtpEngineIndex;
        if (index is { } i && i >= 0 && i < _gtpEngineProfiles.Count)
        {
            var engine = _gtpEngineProfiles[i];
            var entry = GetCgosSeedEntry(stone);
            var options = UsesEngine(entry, engine) ? entry!.EngineOptionOverrides : engine.GuiOptions;
            options[GtpEngineGuiOptions.RandomSeedId] = value;
        }
    }
    public GtpEngineProfile? GetCgosEngineProfileForStart(GoStone stone)
    {
        var clone = GetCgosEngineWithEntryOptions(stone);
        if (clone is null) return null;
        if (!int.TryParse(clone.GuiOptions.GetValueOrDefault(GtpEngineGuiOptions.RandomSeedId), out var seed))
            seed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        clone.GuiOptions[GtpEngineGuiOptions.RandomSeedId] = seed.ToString();
        return clone;
    }

    private EntryProfile? GetCgosSeedEntry(GoStone stone) =>
        stone == GoStone.Black ? SelectedCgosBlackEntryProfile : SelectedCgosWhiteEntryProfile;

    private GtpEngineProfile? GetCgosEngineWithEntryOptions(GoStone stone)
    {
        var engine = stone == GoStone.Black ? SelectedCgosBlackGtpEngineProfile : SelectedCgosWhiteGtpEngineProfile;
        return engine is null ? null : CloneEngineWithEntryOptions(engine, GetCgosSeedEntry(stone));
    }
}
