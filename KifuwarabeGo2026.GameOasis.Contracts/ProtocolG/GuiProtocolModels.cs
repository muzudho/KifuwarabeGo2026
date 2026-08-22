namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>GUIへ公開するプレイスペースのカタログ項目です。</summary>
public sealed record GuiPlaySpaceEntry(
    PlaySpaceTypeId TypeId,
    string DisplayName,
    string ImplementationName,
    string ImplementationVersion,
    IReadOnlyList<string> Capabilities);

/// <summary>Game OasisセッションのGUI向けスナップショットです。</summary>
public sealed record GuiGameSnapshot(
    GameOasisSessionId SessionId,
    PlaySpaceTypeId PlaySpaceTypeId,
    long Revision,
    ContractDocument State,
    bool IsTerminal,
    ContractDocument? Outcome);

/// <summary>GUIによるセッション開始要求です。</summary>
public sealed record GuiOpenSessionRequest(
    PlaySpaceTypeId PlaySpaceTypeId,
    ContractDocument Configuration);

/// <summary>GUIへ返すセッション開始結果です。</summary>
public sealed record GuiSessionOpened(GuiGameSnapshot InitialSnapshot);

/// <summary>GUIによる現在状態の取得要求です。</summary>
public sealed record GuiGetSnapshotRequest(GameOasisSessionId SessionId);

/// <summary>GUIから意味のあるゲーム行動を送る要求です。</summary>
public sealed record GuiSubmitActionRequest(
    GameOasisSessionId SessionId,
    ContractDocument Action,
    long ExpectedRevision);

/// <summary>GUIへ返す行動適用結果です。</summary>
public sealed record GuiActionSubmitted(
    bool IsAccepted,
    GuiGameSnapshot Snapshot,
    IReadOnlyList<ContractDocument> Events,
    ProtocolError? Rejection);

/// <summary>GUIによるセッション終了要求です。</summary>
public sealed record GuiCloseSessionRequest(GameOasisSessionId SessionId);

/// <summary>GUIへ返すセッション終了結果です。</summary>
public sealed record GuiSessionClosed(GameOasisSessionId SessionId);
