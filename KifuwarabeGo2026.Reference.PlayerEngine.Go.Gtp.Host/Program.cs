namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host;

using KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp;

/// <summary>互換実行ファイル名でGTPサーバーを標準入出力へ接続します。</summary>
internal static class Program
{
    public static void Main()
    {
        var engine = new GtpEngine();
        engine.Run(Console.In, Console.Out);
    }
}
