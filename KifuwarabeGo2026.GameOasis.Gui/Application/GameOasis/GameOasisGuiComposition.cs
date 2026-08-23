namespace KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.Reference.PlaySpace.Go;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>現行GUIがGame Oasis参照実装へ接続するための段階移行用コンポジションです。</summary>
public sealed class GameOasisGuiComposition : IDisposable
{
    private GameOasisGuiComposition(GameOasisConcierge concierge, GameOasisGuiClient client)
    {
        Concierge = concierge;
        Client = client;
        BoardController = new(client);
        PlayingBridge = new(BoardController);
        PlayerCoordinator = new(concierge);
        GameMasterCoordinator = new(concierge, PlayerCoordinator);
        HumanGameMaster = new();
        HumanGameMasterParticipation = new(GameMasterCoordinator, HumanGameMaster, client);
        PlayerParticipation = new(PlayerCoordinator, client);
        PlayerParticipationBridge = new(PlayerParticipation, BoardController);
        SecondaryPlayerParticipationBridge = new(PlayerParticipation, BoardController);
        LocalMatchLifecycle = new(BoardController);
    }

    public GameOasisConcierge Concierge { get; }
    public GameOasisGuiClient Client { get; }
    public GameOasisBoardController BoardController { get; }
    public GameOasisPlayingBridge PlayingBridge { get; }
    public GameOasisPlayerCoordinator PlayerCoordinator { get; }
    public GameOasisGameMasterCoordinator GameMasterCoordinator { get; }
    public HumanGameMasterProtocol HumanGameMaster { get; }
    public GameOasisHumanGameMasterParticipation HumanGameMasterParticipation { get; }
    public GameOasisPlayerParticipation PlayerParticipation { get; }
    public GameOasisPlayerParticipationBridge PlayerParticipationBridge { get; }
    public GameOasisPlayerParticipationBridge SecondaryPlayerParticipationBridge { get; }
    public LocalMatchGameOasisLifecycle LocalMatchLifecycle { get; }

    public ProtocolResponse<GuiBoardView> GetActiveBoard()
    {
        return BoardController.GetBoard();
    }

    public void Dispose()
    {
        LocalMatchLifecycle.Dispose();
        PlayerParticipationBridge.Dispose();
        SecondaryPlayerParticipationBridge.Dispose();
        PlayingBridge.Dispose();
    }

    public static async ValueTask<GameOasisGuiComposition> CreateAsync(CancellationToken cancellationToken = default)
    {
        var concierge = new GameOasisConcierge();
        Require(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol(), cancellationToken));
        Require(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol(), cancellationToken));
        var client = new GameOasisGuiClient(new GameOasisGuiProtocol(concierge));
        Require(await client.InitializeAsync(cancellationToken));
        var composition = new GameOasisGuiComposition(concierge, client);
        Require(await composition.GameMasterCoordinator.RegisterGameMasterAsync(composition.HumanGameMaster, cancellationToken));
        return composition;
    }

    private static void Require<T>(ProtocolResponse<T> response)
    {
        if (!response.IsSuccess)
            throw new InvalidOperationException($"Game Oasis GUI composition failed: {response.Error?.Code} {response.Error?.Message}");
    }
}
