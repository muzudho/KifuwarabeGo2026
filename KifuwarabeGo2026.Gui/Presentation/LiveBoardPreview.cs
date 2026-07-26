namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>
/// ホワイトボード中に表示する、進行中の本対局を読み取るための小型盤モデルです。
/// </summary>
public sealed record LiveBoardPreview(
    int BoardSize,
    Func<int, int, GoStone> GetStone,
    GoGameMove? LatestMove,
    int MoveCount,
    string BlackPlayerName,
    string WhitePlayerName);
