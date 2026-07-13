namespace KifuwarabeGo2026.Communication.Cgos;

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

/// <summary>
/// CGOS サーバーとの通信を行うプログラムです。
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        CgosClientOptions options;
        try
        {
            options = CgosClientOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            CgosClientOptions.PrintUsage(Console.Error);
            return 2;
        }

        if (options.ShowHelp)
        {
            CgosClientOptions.PrintUsage(Console.Out);
            return 0;
        }

        Directory.CreateDirectory(options.LogDirectory);

        var accounts = options.Accounts.ToArray();
        if (accounts.Length == 0)
        {
            Console.Error.WriteLine("No CGOS account selected.");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        _ = WatchStandardInputAsync(cancellation);

        var tasks = accounts
            .Select(account => RunClientAsync(options, account, cancellation))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
    }

    private static async Task WatchStandardInputAsync(CancellationTokenSource cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync(cancellation.Token);
                if (line is null)
                {
                    return;
                }

                if (line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    line.Trim().Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    cancellation.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task RunClientAsync(CgosClientOptions options, CgosAccount account, CancellationTokenSource cancellation)
    {
        try
        {
            await new CgosClient(options, account).RunAsync(cancellation.Token);
        }
        catch
        {
            cancellation.Cancel();
            throw;
        }
    }
}

/// <summary>
/// アカウント
/// </summary>
/// <param name="Label"></param>
/// <param name="UserName"></param>
/// <param name="Password"></param>
internal sealed record CgosAccount(string Label, string UserName, string Password);

internal sealed class CgosClientOptions
{
    private const string DefaultEngineCommand = "dotnet run --project KifuwarabeGo2026.Engine\\KifuwarabeGo2026.Engine.csproj";

    private readonly List<CgosAccount> _accounts = new();

    private CgosClientOptions()
    {
    }

    public string Host { get; private set; } = "uec-go.com";

    public int Port { get; private set; } = 6809;

    public string EngineCommand { get; private set; } = DefaultEngineCommand;

    public string LogDirectory { get; private set; } = Path.Combine("Logs", "Cgos");

    public bool ShowHelp { get; private set; }

    public IReadOnlyList<CgosAccount> Accounts => _accounts;

    public static CgosClientOptions Parse(string[] args)
    {
        var options = new CgosClientOptions();
        var selectedAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;
                case "--host":
                    options.Host = ReadValue(args, ref index, arg);
                    break;
                case "--port":
                    var portText = ReadValue(args, ref index, arg);
                    if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
                    {
                        throw new ArgumentException("--port must be an integer from 1 to 65535.");
                    }

                    options.Port = port;
                    break;
                case "--engine-command":
                    options.EngineCommand = ReadValue(args, ref index, arg);
                    break;
                case "--log-directory":
                    options.LogDirectory = ReadValue(args, ref index, arg);
                    break;
                case "--account":
                    selectedAccounts.Add(ReadValue(args, ref index, arg));
                    break;
                case "--both":
                    selectedAccounts.Add("black");
                    selectedAccounts.Add("white");
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {arg}");
            }
        }

        if (selectedAccounts.Count == 0)
        {
            selectedAccounts.Add("black");
        }

        foreach (var account in selectedAccounts)
        {
            options._accounts.Add(CreateAccount(account));
        }

        return options;
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("CGOS communication client for Kifuwarabe Go 2026");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project KifuwarabeGo2026.Communication.Cgos -- [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --account black|white      Login account. Default: black");
        writer.WriteLine("  --both                     Login with both KifuwarabeB and KifuwarabeW.");
        writer.WriteLine("  --host HOST                CGOS host. Default: uec-go.com");
        writer.WriteLine("  --port PORT                CGOS port. Default: 6809");
        writer.WriteLine("  --engine-command COMMAND   GTP engine command line.");
        writer.WriteLine("  --log-directory DIR        Log directory. Default: Logs\\Cgos");
        writer.WriteLine("  -h, --help                 Show help.");
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index];
    }

    private static CgosAccount CreateAccount(string account)
    {
        return account.ToLowerInvariant() switch
        {
            "black" or "b" => new CgosAccount("black", "KifuwarabeB", "KifuwarabeB"),
            "white" or "w" => new CgosAccount("white", "KifuwarabeW", "KifuwarabeW"),
            _ => throw new ArgumentException("--account must be black or white."),
        };
    }
}

internal sealed class CgosClient
{
    private const string ClientIdPrefix = "e1 KifuwarabeGo2026.Cgos";

    private readonly CgosClientOptions _options;
    private readonly CgosAccount _account;
    private readonly object _logLock = new();
    private readonly string _logPath;
    private GtpEngineProcess? _engine;
    private string _engineColor = "black";

    public CgosClient(CgosClientOptions options, CgosAccount account)
    {
        _options = options;
        _account = account;
        _logPath = Path.Combine(options.LogDirectory, $"cgos-{account.Label}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        StreamWriter? writer = null;
        try
        {
            Log($"Connecting to {_options.Host}:{_options.Port} as {_account.UserName}.");

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(_options.Host, _options.Port, cancellationToken);
            await using var stream = tcp.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var cgosWriter = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n", AutoFlush = true };
            writer = cgosWriter;

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    Log("CGOS connection closed.");
                    return;
                }

                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                Log("< " + line);
                if (line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("CGOS error: " + line);
                }

                await HandleLineAsync(line, cgosWriter, cancellationToken);
            }
        }
        finally
        {
            await LogoutAsync(writer);
            await ShutdownEngineAsync();
        }
    }

    private async Task HandleLineAsync(string line, StreamWriter writer, CancellationToken cancellationToken)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();
        var parameters = parts.Length == 2
            ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        switch (command)
        {
            case "protocol":
                var response = GetClientId(parameters.Contains("genmove_analyze", StringComparer.OrdinalIgnoreCase));
                await SendAsync(writer, response);
                return;
            case "username":
                await SendAsync(writer, _account.UserName);
                return;
            case "password":
                await SendAsync(writer, _account.Password, maskInLog: true);
                return;
            case "setup":
                await HandleSetupAsync(parameters, cancellationToken);
                return;
            case "play":
                await RequireEngine().PlayAsync(parameters, cancellationToken);
                return;
            case "genmove":
                var move = await HandleGenMoveAsync(parameters, cancellationToken);
                await SendAsync(writer, move);
                return;
            case "gameover":
                Log("Game over: " + string.Join(' ', parameters));
                await ShutdownEngineAsync();
                await SendAsync(writer, "ready");
                return;
            case "info":
                Log("Info: " + string.Join(' ', parameters));
                return;
            default:
                throw new InvalidOperationException("Unsupported CGOS command: " + command);
        }
    }

    private async Task HandleSetupAsync(string[] parameters, CancellationToken cancellationToken)
    {
        if (parameters.Length < 6)
        {
            throw new InvalidOperationException("CGOS setup requires at least 6 parameters.");
        }

        await ShutdownEngineAsync();
        _engine = new GtpEngineProcess(_options.EngineCommand, _options.LogDirectory, _account.Label);
        await _engine.StartAsync(cancellationToken);

        var boardSize = parameters[1];
        var komi = parameters[2];
        var programA = StripRank(parameters[4]);
        var programB = StripRank(parameters[5]);
        _engineColor = string.Equals(_account.UserName, programA, StringComparison.OrdinalIgnoreCase) ? "white" : "black";

        Log($"Setup game. board={boardSize}, komi={komi}, localColor={_engineColor}, programA={programA}, programB={programB}");

        await _engine.CommandAsync("boardsize " + boardSize, cancellationToken);
        await _engine.CommandAsync("komi " + komi, cancellationToken);
        await _engine.CommandAsync("clear_board", cancellationToken);

        var replayColor = "b";
        for (var index = 6; index + 1 < parameters.Length; index += 2)
        {
            await _engine.PlayAsync(new[] { replayColor, parameters[index], parameters[index + 1] }, cancellationToken);
            replayColor = replayColor == "b" ? "w" : "b";
        }
    }

    private async Task<string> HandleGenMoveAsync(string[] parameters, CancellationToken cancellationToken)
    {
        if (parameters.Length != 2)
        {
            throw new InvalidOperationException("CGOS genmove requires 2 parameters.");
        }

        var engine = RequireEngine();
        var color = parameters[0];
        var response = await engine.CommandAsync("genmove " + color, cancellationToken);
        var move = response.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(move))
        {
            throw new InvalidOperationException("GTP engine returned an empty genmove response.");
        }

        Log($"Generated {_engineColor} move: {move}");
        return move.ToLowerInvariant();
    }

    private GtpEngineProcess RequireEngine()
    {
        return _engine ?? throw new InvalidOperationException("CGOS sent a game command before setup.");
    }

    private async Task ShutdownEngineAsync()
    {
        if (_engine is not null)
        {
            await _engine.DisposeAsync();
            _engine = null;
        }
    }

    /// <summary>
    /// ログアウト。CGOS へ "quit" コマンドを送る。
    /// </summary>
    /// <param name="writer">CGOS への接続ストリームライター</param>
    /// <returns></returns>
    private async Task LogoutAsync(StreamWriter? writer)
    {
        if (writer is null) return;

        try
        {
            await SendAsync(writer, "quit");
        }
        catch (Exception ex)
        {
            Log("Could not send CGOS quit: " + ex.Message);
        }
    }

    private async Task SendAsync(StreamWriter writer, string message, bool maskInLog = false)
    {
        await writer.WriteLineAsync(message);
        Log("> " + (maskInLog ? "(password)" : message));
    }

    private void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{_account.Label}] {message}";
        Console.WriteLine(line);
        lock (_logLock)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static string GetClientId(bool serverSupportsAnalyze)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        return $"{ClientIdPrefix} {version}";
    }

    private static string StripRank(string programName)
    {
        var rankIndex = programName.IndexOf('(');
        return rankIndex < 0 ? programName : programName[..rankIndex];
    }
}

internal sealed class GtpEngineProcess : IAsyncDisposable
{
    private readonly string _commandLine;
    private readonly string _logPath;
    private Process? _process;
    private StreamWriter? _input;
    private StreamReader? _output;

    public GtpEngineProcess(string commandLine, string logDirectory, string accountLabel)
    {
        _commandLine = commandLine;
        _logPath = Path.Combine(logDirectory, $"gtp-{accountLabel}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log("Starting GTP engine: " + _commandLine);

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows() ? "/c " + _commandLine : "-c \"" + _commandLine.Replace("\"", "\\\"") + "\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Log("! " + e.Data);
            }
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Could not start GTP engine.");
        }

        _process.BeginErrorReadLine();
        _input = _process.StandardInput;
        _output = _process.StandardOutput;
        return Task.CompletedTask;
    }

    public async Task PlayAsync(string[] parameters, CancellationToken cancellationToken)
    {
        if (parameters.Length != 3)
        {
            throw new InvalidOperationException("CGOS play requires 3 parameters.");
        }

        await CommandAsync($"play {parameters[0]} {parameters[1]}", cancellationToken);
    }

    public async Task<IReadOnlyList<string>> CommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_process is null || _input is null || _output is null)
        {
            throw new InvalidOperationException("GTP engine has not been started.");
        }

        if (_process.HasExited)
        {
            throw new InvalidOperationException($"GTP engine exited with code {_process.ExitCode}.");
        }

        Log("> " + command);
        await _input.WriteLineAsync(command.AsMemory(), cancellationToken);
        await _input.FlushAsync(cancellationToken);

        var response = new List<string>();
        string? error = null;
        while (true)
        {
            var line = await _output.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                throw new EndOfStreamException("GTP engine closed stdout.");
            }

            Log("< " + line);
            if (line.Length == 0)
            {
                break;
            }

            if (line[0] == '=')
            {
                line = line[1..].Trim();
            }
            else if (line[0] == '?')
            {
                error = line[1..].Trim();
                line = "";
            }
            else
            {
                line = line.Trim();
            }

            if (line.Length > 0)
            {
                response.Add(line);
            }
        }

        if (error is not null)
        {
            throw new InvalidOperationException("GTP command failed: " + error);
        }

        return response;
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                try
                {
                    await CommandAsync("quit", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log("quit failed: " + ex.Message);
                }

                if (!_process.WaitForExit(3000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
    }
}
