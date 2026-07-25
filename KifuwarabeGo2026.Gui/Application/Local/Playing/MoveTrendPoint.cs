namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// CGOS の一着分の評価値を、黒有利を正とする表示座標へ正規化したものです。
/// </summary>
public readonly record struct MoveTrendPoint(
    int MoveNumber,
    GoStone Reporter,
    double? BlackPerspectiveScore,
    double? BlackPerspectiveWinAdvantage);
