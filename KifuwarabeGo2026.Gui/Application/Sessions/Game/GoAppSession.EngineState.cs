namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>ローカル対局で使用するGTPエンジンの準備・思考・エラー状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public bool IsEngineThinking { get; private set; }
    public bool IsEngineReady { get; private set; } = true;
    public string EngineErrorMessage { get; private set; } = "";
    private GoStone? _engineErrorStone;

    public GoStone? EngineErrorStone =>
        string.IsNullOrWhiteSpace(EngineErrorMessage) ? null : _engineErrorStone;

    public void SetEngineThinking(bool isThinking) => IsEngineThinking = isThinking;
    public void SetEngineReady(bool isReady) => IsEngineReady = isReady;

    public void ClearEngineError()
    {
        EngineErrorMessage = "";
        _engineErrorStone = null;
    }

    public void SetEngineError(string message, GoStone stone)
    {
        EngineErrorMessage = message;
        _engineErrorStone = stone;
        IsEngineThinking = false;
    }
}
