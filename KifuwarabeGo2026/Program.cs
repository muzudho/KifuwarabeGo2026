namespace KifuwarabeGo2026;

internal static class Program
{
    [System.STAThread]
    private static void Main()
    {
        using var game = new Game1();
        game.Run();
    }
}
