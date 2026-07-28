namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui;
using System;

internal static class Program
{
    private static void Main()
    {
        Func<Game1> composition = CreateGame;
        _ = composition;
        System.Console.WriteLine(
            "Portable platform composition compiled without Windows APIs.");
    }

    /// <summary>
    /// OS 別起動プロジェクトでの Game1 の組み立て例です。
    /// スモーク実行時はウィンドウを開かず、コンパイル可能性だけを確認します。
    /// </summary>
    private static Game1 CreateGame()
    {
        var platform = new PortablePlatformServices();
        return new Game1(
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform);
    }
}
