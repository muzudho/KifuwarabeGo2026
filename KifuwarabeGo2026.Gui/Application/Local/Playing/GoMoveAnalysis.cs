namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

/// <summary>着手時に思考エンジンが返した解析情報です。</summary>
public sealed record GoMoveAnalysis(
    double? Winrate,
    string PrincipalVariation,
    double? Score,
    long? Visits);
