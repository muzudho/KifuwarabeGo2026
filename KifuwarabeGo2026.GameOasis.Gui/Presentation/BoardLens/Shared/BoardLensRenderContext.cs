namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.Shared;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 個々の Board Lens へ渡す、盤面と共通描画情報です。
/// </summary>
internal readonly record struct BoardLensRenderContext(
    RenParseDisplayMode DisplayMode,
    int BoardSize,
    Func<int, int, GoStone> GetStone,
    Func<GoRenParseResult> ParseRens,
    Action DrawPlacedStones,
    Vector2 Start,
    float Cell);
