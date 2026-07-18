namespace KifuwarabeGo2026.Communication.Cgos;

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
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

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        var parentWatcher = WatchParentProcessAsync(options, cancellation);

        try
        {
            if (options.AdminMode)
            {
                await new CgosAdminClient(options).RunAsync(cancellation.Token);
                return cancellation.IsCancellationRequested ? 130 : 0;
            }

            var accounts = options.Accounts.ToArray();
            if (accounts.Length == 0)
            {
                Console.Error.WriteLine("No CGOS account selected.");
                return 2;
            }

            _ = CgosStandardInputRelay.Start(
                (line, _) =>
                {
                    if (CgosStandardInputRelay.IsExitCommand(line))
                    {
                        cancellation.Cancel();
                    }

                    return Task.CompletedTask;
                },
                ex => Console.Error.WriteLine("# CGOS input watcher failed: " + ex.Message),
                cancellation.Token);

            var tasks = accounts
                .Select(account => RunClientAsync(options, account, cancellation))
                .ToArray();

            await Task.WhenAll(tasks);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        finally
        {
            cancellation.Cancel();
            await parentWatcher;
        }
    }

    private static async Task WatchParentProcessAsync(CgosClientOptions options, CancellationTokenSource cancellation)
    {
        if (options.ParentProcessId is null || options.ParentProcessStartTimeUtcTicks is null)
        {
            return;
        }

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (IsExpectedParentProcessRunning(options.ParentProcessId.Value, options.ParentProcessStartTimeUtcTicks.Value) &&
                   await timer.WaitForNextTickAsync(cancellation.Token))
            {
            }

            if (!cancellation.IsCancellationRequested)
            {
                const string message = "# Parent GUI process exited. Stopping CGOS communication process.";
                Console.Error.WriteLine(message);
                AppendProcessLifecycleLog(options.LogDirectory, message);
                cancellation.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void AppendProcessLifecycleLog(string logDirectory, string message)
    {
        try
        {
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, $"process-lifecycle-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(
                logPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool IsExpectedParentProcessRunning(int processId, long startTimeUtcTicks)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == startTimeUtcTicks;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
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

internal static class CgosStandardInputRelay
{
    public static Task Start(
        Func<string, CancellationToken, Task> handleLineAsync,
        Action<Exception> logError,
        CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await Console.In.ReadLineAsync(cancellationToken);
                    if (line is null)
                    {
                        return;
                    }

                    line = line.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    await handleLineAsync(line, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logError(ex);
            }
        }, CancellationToken.None);

    public static bool IsExitCommand(string line) =>
        line.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        line.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        line.Equals("cancel", StringComparison.OrdinalIgnoreCase);
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

    public bool AdminMode { get; private set; }

    public int? ParentProcessId { get; private set; }

    public long? ParentProcessStartTimeUtcTicks { get; private set; }

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
                case "--admin":
                    options.AdminMode = true;
                    break;
                case "--parent-process-id":
                    var parentProcessIdText = ReadValue(args, ref index, arg);
                    if (!int.TryParse(parentProcessIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var parentProcessId) || parentProcessId < 1)
                    {
                        throw new ArgumentException("--parent-process-id must be a positive integer.");
                    }

                    options.ParentProcessId = parentProcessId;
                    break;
                case "--parent-process-start-time":
                    var parentStartTimeText = ReadValue(args, ref index, arg);
                    if (!long.TryParse(parentStartTimeText, NumberStyles.None, CultureInfo.InvariantCulture, out var parentStartTime) || parentStartTime < 1)
                    {
                        throw new ArgumentException("--parent-process-start-time must be a positive UTC ticks value.");
                    }

                    options.ParentProcessStartTimeUtcTicks = parentStartTime;
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

        if (options.ParentProcessId.HasValue != options.ParentProcessStartTimeUtcTicks.HasValue)
        {
            throw new ArgumentException("--parent-process-id and --parent-process-start-time must be specified together.");
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
        writer.WriteLine("  --admin                    Login without a GTP engine and relay admin commands from stdin.");
        writer.WriteLine("  --parent-process-id PID    Exit when the parent GUI process exits.");
        writer.WriteLine("  --parent-process-start-time TICKS");
        writer.WriteLine("                             Parent process UTC start time ticks (required with PID).");
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

internal static class CgosTcpConnector
{
    public static async Task<TcpClient> ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        log($"# TCP connect timeout is {timeout.TotalSeconds:0} seconds.");
        var stopwatch = Stopwatch.StartNew();

        IPAddress[] addresses;
        try
        {
            addresses = await Task.Run(() => Dns.GetHostAddresses(host), cancellationToken).WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            log($"# DNS lookup timed out after {timeout.TotalSeconds:0} seconds: {host}.");
            throw new InvalidOperationException($"Could not resolve {host} within {timeout.TotalSeconds:0} seconds.", ex);
        }

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException($"Could not resolve {host}.");
        }

        log("# Resolved " + host + " to " + string.Join(", ", addresses.Select(address => address.ToString())) + ".");

        Exception? lastException = null;
        foreach (var address in addresses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var endpoint = new IPEndPoint(address, port);
            var tcp = new TcpClient(address.AddressFamily);
            try
            {
                await Task.Run(() => tcp.Connect(endpoint), cancellationToken).WaitAsync(timeout, cancellationToken);
                stopwatch.Stop();
                log($"# TCP connect completed in {stopwatch.Elapsed.TotalSeconds:0.000} seconds: {endpoint}.");
                return tcp;
            }
            catch (TimeoutException ex)
            {
                lastException = ex;
                break;
            }
            catch (SocketException ex)
            {
                tcp.Dispose();
                lastException = ex;
                log($"# TCP connect failed: {endpoint} {ex.SocketErrorCode} {ex.Message}");
            }
            catch
            {
                tcp.Dispose();
                throw;
            }
        }

        throw new InvalidOperationException($"Could not connect to {host}:{port} within {timeout.TotalSeconds:0} seconds.", lastException);
    }
}

internal sealed class CgosConnectionSession
{
    private static readonly TimeSpan FirstServerLineTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan TcpConnectTimeout = TimeSpan.FromSeconds(15);

    private readonly CgosClientOptions _options;
    private readonly CgosAccount _account;
    private readonly Action<string> _log;
    private readonly string _connectionPurpose;
    private StreamWriter? _writer;
    private bool _quitSent;

    public CgosConnectionSession(CgosClientOptions options, CgosAccount account, Action<string> log, string connectionPurpose = "")
    {
        _options = options;
        _account = account;
        _log = log;
        _connectionPurpose = connectionPurpose;
    }

    public async Task RunAsync(
        Func<string, CancellationToken, Task> handleServerLineAsync,
        Func<CancellationToken, Task>? passwordSentAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            _log($"# Connecting to {_options.Host}:{_options.Port} as {_account.UserName}{_connectionPurpose}.");

            using var tcp = await CgosTcpConnector.ConnectAsync(_options.Host, _options.Port, TcpConnectTimeout, _log, cancellationToken);
            _log($"# Connected to {_options.Host}:{_options.Port}.");

            await using var stream = tcp.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n", AutoFlush = true };
            _writer = writer;

            var receivedAnyLine = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    var readTask = reader.ReadLineAsync(cancellationToken).AsTask();
                    line = receivedAnyLine
                        ? await readTask
                        : await readTask.WaitAsync(FirstServerLineTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    _log($"# CGOS did not send the first protocol line within {FirstServerLineTimeout.TotalSeconds:0} seconds after TCP connect.");
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (IOException ex)
                {
                    _log("# CGOS connection read failed: " + ex.Message);
                    return;
                }

                if (line is null)
                {
                    _log("# CGOS connection closed.");
                    return;
                }

                line = line.Trim();
                receivedAnyLine = true;
                if (line.Length == 0)
                {
                    continue;
                }

                _log("> " + line);
                if (line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("CGOS error: " + line);
                }

                if (await HandleLoginLineAsync(line, cancellationToken, passwordSentAsync))
                {
                    continue;
                }

                await handleServerLineAsync(line, cancellationToken);
            }
        }
        finally
        {
            try
            {
                await SendQuitAsync();
            }
            catch (Exception ex)
            {
                _log("# Could not send CGOS quit: " + ex.Message);
            }

            _writer = null;
        }
    }

    public async Task SendAsync(string message, bool maskInLog = false)
    {
        var writer = _writer ?? throw new InvalidOperationException("CGOS is not connected.");
        await writer.WriteLineAsync(message);
        _log("< " + (maskInLog ? "(password)" : message));
    }

    public async Task SendQuitAsync()
    {
        if (_writer is null || _quitSent)
        {
            return;
        }

        await SendAsync("quit");
        _quitSent = true;
    }

    private async Task<bool> HandleLoginLineAsync(
        string line,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task>? passwordSentAsync)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        switch (parts[0].ToLowerInvariant())
        {
            case "protocol":
                await SendAsync(CgosClient.GetClientId(parts.Length == 2 && parts[1].Contains("genmove_analyze", StringComparison.OrdinalIgnoreCase)));
                return true;
            case "username":
                await SendAsync(_account.UserName);
                return true;
            case "password":
                await SendAsync(_account.Password, maskInLog: true);
                if (passwordSentAsync is not null)
                {
                    await passwordSentAsync(cancellationToken);
                }

                return true;
            default:
                return false;
        }
    }
}

internal sealed class CgosAdminClient
{
    private readonly CgosClientOptions _options;
    private readonly CgosAccount _account;
    private readonly object _logLock = new();
    private readonly string _logPath;

    public CgosAdminClient(CgosClientOptions options)
    {
        _options = options;
        _account = new CgosAccount("admin", "admin", "admin");
        _logPath = Path.Combine(options.LogDirectory, $"cgos-admin-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var session = new CgosConnectionSession(_options, _account, Log, " for admin");
        var adminInputStarted = false;
        await session.RunAsync(
            (line, token) =>
            {
                if (!adminInputStarted && line.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    adminInputStarted = true;
                    Log("# Admin login accepted. Command input is ready.");
                    _ = CgosStandardInputRelay.Start(
                        (command, relayToken) => RelayAdminCommandAsync(session, command, relayToken),
                        ex => Log("# Admin input relay failed: " + ex.Message),
                        token);
                }

                return Task.CompletedTask;
            },
            passwordSentAsync: null,
            cancellationToken);
    }

    private async Task RelayAdminCommandAsync(
        CgosConnectionSession session,
        string command,
        CancellationToken cancellationToken)
    {
        if (CgosStandardInputRelay.IsExitCommand(command))
        {
            await session.SendQuitAsync();
            return;
        }

        if (!command.Equals("who", StringComparison.OrdinalIgnoreCase) &&
            !command.Equals("match", StringComparison.OrdinalIgnoreCase) &&
            !command.StartsWith("match ", StringComparison.OrdinalIgnoreCase))
        {
            Log("# Unsupported admin command ignored: " + command);
            return;
        }

        await session.SendAsync(
            command.StartsWith("match ", StringComparison.OrdinalIgnoreCase)
                ? "match " + command[6..]
                : command.ToLowerInvariant());
    }

    private void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [admin] {message}";
        Console.WriteLine(line);
        lock (_logLock)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}

internal sealed class CgosClient
{
    private const string ClientId = "e1";

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
        var session = new CgosConnectionSession(_options, _account, Log);
        try
        {
            await session.RunAsync(
                (line, token) => HandleLineAsync(line, session, token),
                passwordSentAsync: null,
                cancellationToken);
        }
        finally
        {
            await ShutdownEngineAsync();
        }
    }

    private async Task HandleLineAsync(string line, CgosConnectionSession session, CancellationToken cancellationToken)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();
        var parameters = parts.Length == 2
            ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();

        switch (command)
        {
            case "setup":
                await HandleSetupAsync(parameters, cancellationToken);
                return;
            case "play":
                await RequireEngine().PlayAsync(parameters, cancellationToken);
                return;
            case "genmove":
                var move = await HandleGenMoveAsync(parameters, cancellationToken);
                await session.SendAsync(move);
                return;
            case "gameover":
                Log("# Game over: " + string.Join(' ', parameters));
                await ShutdownEngineAsync();
                await session.SendAsync("ready");
                return;
            case "info":
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

        Log($"# Setup game. board={boardSize}, komi={komi}, localColor={_engineColor}, programA={programA}, programB={programB}");

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

        Log($"# Generated {_engineColor} move: {move}");
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

    private void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{_account.Label}] {message}";
        Console.WriteLine(line);
        lock (_logLock)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    public static string GetClientId(bool serverSupportsAnalyze)
    {
        return ClientId;
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
                Log("# [StandardError] " + e.Data);
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
