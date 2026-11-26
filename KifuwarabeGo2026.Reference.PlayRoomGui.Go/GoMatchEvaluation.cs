namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

/// <summary>Engine estimates for a completed move, expressed from Black's perspective.</summary>
public sealed record GoMatchEvaluation(int MoveNumber, double? BlackWinRate, double? BlackScore);
