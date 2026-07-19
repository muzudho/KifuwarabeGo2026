namespace KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// ［囲碁盤の交点］
/// </summary>
/// <param name="X">交点のX座標</param>
/// <param name="Y">交点のY座標</param>
public readonly record struct GoPoint(int X, int Y);
