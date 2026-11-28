using System.Text;

// GUIの日本語オプション値も、GTPの入出力もUTF-8で扱う。
Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);
try { return await new SlotOptionProxy().RunAsync(); }
catch (Exception error) when (SwitchControl.IsControlError(error) || error is TimeoutException)
{
    Console.Error.WriteLine($"GTP engine switch: {error.Message}");
    return 1;
}
