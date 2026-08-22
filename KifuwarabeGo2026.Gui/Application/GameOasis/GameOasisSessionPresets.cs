namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using System;

public static class GameOasisSessionPresets
{
    public static bool TryCreate(PlaySpaceTypeId playSpaceTypeId, out ContractDocument configuration)
    {
        configuration = playSpaceTypeId.Value switch
        {
            GameOasisOfficialNames.Go => new(
                "application/json",
                GameOasisOfficialNames.Go + ".configuration.v1",
                """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}"""),
            GameOasisOfficialNames.Ponnuki => new(
                "application/json",
                GameOasisOfficialNames.Ponnuki + ".configuration.v1",
                """{"version":1,"boardSize":9,"initialMoveCount":20,"captureTarget":1,"startingPlayer":"black","setupStones":[]}"""),
            _ => null!,
        };
        return configuration is not null;
    }

    public static ContractDocument Create(PlaySpaceTypeId playSpaceTypeId) =>
        TryCreate(playSpaceTypeId, out var configuration)
            ? configuration
            : throw new ArgumentOutOfRangeException(nameof(playSpaceTypeId), playSpaceTypeId.Value, "No reference GUI preset is available for this play-space.");
}
