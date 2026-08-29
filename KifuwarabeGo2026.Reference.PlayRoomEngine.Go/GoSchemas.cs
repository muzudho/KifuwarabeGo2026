namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go;

/// <summary>通常囲碁参照実装が公開する文書スキーマIDです。</summary>
public static class GoSchemas
{
    public const string Configuration = GameOasis.Contracts.Common.GameOasisOfficialNames.Go + ".configuration.v1";
    public const string Action = GameOasis.Contracts.Common.GameOasisOfficialNames.Go + ".action.v1";
    public const string State = GameOasis.Contracts.Common.GameOasisOfficialNames.Go + ".state.v1";
    public const string Event = GameOasis.Contracts.Common.GameOasisOfficialNames.Go + ".event.v1";
    public const string Outcome = GameOasis.Contracts.Common.GameOasisOfficialNames.Go + ".outcome.v1";
}
