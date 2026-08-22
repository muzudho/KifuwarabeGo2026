namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.Reference.PlaySpace.Go;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>現行GUIがGame Oasis参照実装へ接続するための段階移行用コンポジションです。</summary>
public sealed class GameOasisGuiComposition
{
    private GameOasisGuiComposition(GameOasisConcierge concierge, GameOasisGuiClient client)
    {
        Concierge = concierge;
        Client = client;
        BoardController = new(client);
    }

    public GameOasisConcierge Concierge { get; }
    public GameOasisGuiClient Client { get; }
    public GameOasisBoardController BoardController { get; }

    public ProtocolResponse<GuiBoardView> GetActiveBoard()
    {
        return BoardController.GetBoard();
    }

    public static async ValueTask<GameOasisGuiComposition> CreateAsync(CancellationToken cancellationToken = default)
    {
        var concierge = new GameOasisConcierge();
        Require(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol(), cancellationToken));
        Require(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol(), cancellationToken));
        var client = new GameOasisGuiClient(new GameOasisGuiProtocol(concierge));
        Require(await client.InitializeAsync(cancellationToken));
        return new(concierge, client);
    }

    private static void Require<T>(ProtocolResponse<T> response)
    {
        if (!response.IsSuccess)
            throw new InvalidOperationException($"Game Oasis GUI composition failed: {response.Error?.Code} {response.Error?.Message}");
    }
}
