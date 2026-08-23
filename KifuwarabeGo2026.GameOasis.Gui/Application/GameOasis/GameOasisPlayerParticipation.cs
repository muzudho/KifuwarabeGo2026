namespace KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;
using KifuwarabeGo2026.Reference.Gui;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Protocol Gで開かれた同じ不透明なセッションへProtocol Pプレイヤーを参加させます。
/// ゲーム状態は保持せず、Conciergeの参加者バインディングだけを扱います。
/// </summary>
public sealed class GameOasisPlayerParticipation(
    GameOasisPlayerCoordinator coordinator,
    GameOasisGuiClient guiClient)
{
    private readonly GameOasisPlayerCoordinator _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    private readonly GameOasisGuiClient _guiClient = guiClient ?? throw new ArgumentNullException(nameof(guiClient));

    public async ValueTask<ProtocolResponse<PlayerBound>> RegisterAndBindAsync(
        IPlayerProtocol protocol,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        if (string.IsNullOrWhiteSpace(roleId))
            return Failure<PlayerBound>("empty-player-role", "The player role ID must not be empty.");

        var sessionId = _guiClient.State.ActiveSnapshot?.SessionId;
        if (sessionId is null)
            return Failure<PlayerBound>("gui-session-not-open", "Open a Game Oasis session before binding a player.");

        var registered = await _coordinator.RegisterPlayerAsync(protocol, cancellationToken);
        if (!registered.IsSuccess || registered.Value is null)
            return Forward<PlayerBound>(registered.Error, "player-registration-failed");

        return await _coordinator.BindPlayerAsync(
            registered.Value.Descriptor.EngineId,
            sessionId.Value,
            roleId,
            cancellationToken);
    }

    public ValueTask<ProtocolResponse<PlayerTurnCompleted>> RequestAndApplyActionAsync(
        PlayerBindingId bindingId,
        CancellationToken cancellationToken = default) =>
        _coordinator.RequestAndApplyActionAsync(bindingId, cancellationToken);

    public ValueTask<ProtocolResponse<PlayerUnbound>> UnbindAsync(
        PlayerBindingId bindingId,
        string reason,
        CancellationToken cancellationToken = default) =>
        _coordinator.UnbindPlayerAsync(bindingId, reason, cancellationToken);

    private static ProtocolResponse<T> Forward<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new(fallbackCode, "The player operation returned an invalid failure response."));

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new(code, message));
}
