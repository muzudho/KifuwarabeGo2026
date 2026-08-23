namespace KifuwarabeGo2026.Reference.Gui;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>共通盤面上のGUI入力を、公式プレイスペースの意味的な行動文書へ変換します。</summary>
public static class GameBoardActionFactory
{
    public static ProtocolResponse<ContractDocument> CreatePlay(GuiBoardView board, int x, int y) =>
        GameBoardActionAdapters.Official.CreatePlay(board, x, y);

    public static ProtocolResponse<ContractDocument> CreatePass(GuiBoardView board) =>
        GameBoardActionAdapters.Official.CreatePass(board);

    public static ProtocolResponse<ContractDocument> CreateResign(GuiBoardView board) =>
        GameBoardActionAdapters.Official.CreateResign(board);
}
