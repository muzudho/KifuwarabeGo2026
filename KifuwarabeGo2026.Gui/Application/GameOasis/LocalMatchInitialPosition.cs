namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;

/// <summary>Game Oasisセッション開始時だけ使う、現行GUIから独立した初期局面です。</summary>
public sealed record LocalMatchInitialPosition(
    int BoardSize,
    GoStone StartingTurn,
    IReadOnlyList<LocalMatchSetupStone> SetupStones);

public sealed record LocalMatchSetupStone(GoStone Stone, GoPoint Point);
