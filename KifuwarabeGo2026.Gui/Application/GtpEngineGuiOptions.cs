namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// GUIからGTPエンジンへ送るオプションの既知値です。
/// </summary>
public static class GtpEngineGuiOptions
{
    public const string RandomMoveId = "RandomMove";
    public const string NormalRandomMove = "Normal";
    public const string ChebyshevDistanceFromStarRandomMove = "ChebyshevDistanceFromStar";

    public static readonly string[] RandomMoveValues =
    [
        NormalRandomMove,
        ChebyshevDistanceFromStarRandomMove,
    ];
}
