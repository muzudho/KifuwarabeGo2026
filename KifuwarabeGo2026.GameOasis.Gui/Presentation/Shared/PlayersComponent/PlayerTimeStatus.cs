namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.PlayersComponent;

using System;

/// <summary>プレイヤー行に出す持ち時間と現在計時の文言を組み立てます。</summary>
public sealed class PlayerTimeStatus
{
    public string Build(TimeSpan? elapsed, TimeSpan? mainTime, int agehama, bool minimal, Func<TimeSpan, string> format) =>
        minimal
            ? $"USED {Format(elapsed, format)} / LIMIT {Format(mainTime, format)}"
            : $"USED {Format(elapsed, format)} / LIMIT {Format(mainTime, format)}    AGEHAMA {agehama}";

    public string BuildLive(TimeSpan elapsed, Func<TimeSpan, string> format) => $"NOW  {format(elapsed)}";

    private static string Format(TimeSpan? value, Func<TimeSpan, string> format) => value is { } time ? format(time) : "--:--";
}
