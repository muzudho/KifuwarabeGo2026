namespace KifuwarabeGo2026.GameOasis.Gui.Application.PlayRoom;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;
using KifuwarabeGo2026.GameOasis.Gui.Sgf;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>ロビーで選ばれた設定を、プレイルームが受け取れる起動契約へ写します。</summary>
public static class PlayRoomLaunchRequestFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PlayRoomLaunchRequest CreateLocalMatch(GoAppSession session) =>
        CreateGoRequest(session, PlayRoomIds.Match, CreatePlayers(session));

    public static PlayRoomLaunchRequest CreateBoardEditor(GoAppSession session) =>
        CreateGoRequest(session, PlayRoomIds.BoardEditor, []);

    public static PlayRoomLaunchRequest CreatePonnukiMatch(GoAppSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var participants = new List<PlayRoomParticipant>(CreatePlayers(session));
        if (session.HasSelectedAppProviderEngine)
        {
            var provider = session.SelectedAppProviderEngine;
            participants.Add(CreateParticipant("provider", provider.Id, provider.DisplayName, "provider", provider));
        }

        var playSpaceTypeId = new PlaySpaceTypeId(GameOasisOfficialNames.Ponnuki);
        return new(1, Guid.NewGuid().ToString("N"), PlayRoomIds.Match,
            GameOasisOfficialNames.Ponnuki, playSpaceTypeId,
            GameOasisSessionPresets.Create(playSpaceTypeId), null, participants);
    }

    private static PlayRoomLaunchRequest CreateGoRequest(GoAppSession session, string roomTypeId, IReadOnlyList<PlayRoomParticipant> participants)
    {
        ArgumentNullException.ThrowIfNull(session);
        var setupStones = new List<LocalMatchSetupStone>();
        for (var y = 0; y < session.BoardSize; y++)
        for (var x = 0; x < session.BoardSize; x++)
        {
            var stone = session.GetStone(x, y);
            if (stone != GoStone.Empty) setupStones.Add(new(stone, new GoPoint(x, y)));
        }

        var configuration = LocalMatchGameOasisConfiguration.Create(
            new LocalMatchInitialPosition(session.BoardSize, session.CurrentTurn, setupStones), session.Komi, session.MainTime);
        var initialPosition = new ContractDocument("application/x-go-sgf",
            GameOasisOfficialNames.Go + ".sgf.v1", SgfGameRecordConverter.ToSgf(session.CurrentGameRecord));
        return new(1, Guid.NewGuid().ToString("N"), roomTypeId, GameOasisOfficialNames.Go,
            new PlaySpaceTypeId(GameOasisOfficialNames.Go), configuration, initialPosition, participants);
    }

    private static IReadOnlyList<PlayRoomParticipant> CreatePlayers(GoAppSession session) =>
        [CreatePlayer(session, GoStone.Black), CreatePlayer(session, GoStone.White)];

    private static PlayRoomParticipant CreatePlayer(GoAppSession session, GoStone stone)
    {
        var entry = session.GetSelectedEntryProfile(stone);
        return CreateParticipant(stone == GoStone.Black ? "black" : "white", entry?.Id ?? "",
            session.GetLocalPlayerName(stone), session.GetPlayerKind(stone).ToString().ToLowerInvariant(),
            session.GetSelectedPlayerEngineProfile(stone));
    }

    private static PlayRoomParticipant CreateParticipant(string roleId, string entryId, string displayName, string kind, GtpEngineProfile? engine) =>
        new(roleId, entryId, displayName, kind, engine?.Id ?? "", engine is null ? null :
            new ContractDocument("application/json", GameOasisOfficialNames.Root + ".gtp-engine-options.v1",
                JsonSerializer.Serialize(engine.GuiOptions, JsonOptions)));
}
