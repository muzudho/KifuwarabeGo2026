namespace KifuwarabeGo2026.Reference.Communication.Cgos.Host;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;
using KifuwarabeGo2026.FormalAdapter.Cgos.Client;
using KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine;
using KifuwarabeGo2026.FormalAdapter.Cgos.GameMasterEngine;
using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;

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

            var playerControl = new CgosPlayerControl();
            _ = CgosStandardInputRelay.Start(
                (line, _) =>
                {
                    if (CgosStandardInputRelay.IsExitCommand(line))
                    {
                        cancellation.Cancel();
                    }
                    else if (TryParseResignCommand(line, out var gameId))
                    {
                        playerControl.RequestResign(gameId);
                    }
                    else if (TryParseMoveCommand(line, out gameId, out var vertex))
                    {
                        playerControl.RequestHumanMove(gameId!.Value, vertex);
                    }

                    return Task.CompletedTask;
                },
                ex => Console.Error.WriteLine("# CGOS input watcher failed: " + ex.Message),
                cancellation.Token);

            var tasks = accounts
                .Select(account => RunClientAsync(options, account, playerControl, cancellation))
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

    private static bool TryParseResignCommand(string line, out int? gameId)
    {
        gameId = null;
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && parts[0].Equals("resign", StringComparison.OrdinalIgnoreCase)) return true;
        if (parts.Length == 2 && parts[0].Equals("resign", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1], out var parsedGameId))
        {
            gameId = parsedGameId;
            return true;
        }
        return false;
    }

    private static bool TryParseMoveCommand(string line, out int? gameId, out string vertex)
    {
        gameId = null;
        vertex = "";
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !parts[0].Equals("move", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[1], out var parsedGameId)) return false;
        gameId = parsedGameId;
        vertex = parts[2];
        return true;
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

    private static async Task RunClientAsync(
        CgosClientOptions options,
        CgosAccount account,
        CgosPlayerControl playerControl,
        CancellationTokenSource cancellation)
    {
        try
        {
            await new CgosClient(options, account, playerControl).RunAsync(cancellation.Token);
        }
        catch
        {
            cancellation.Cancel();
            throw;
        }
    }
}

internal sealed class CgosPlayerControl
{
    private readonly object _sync = new();
    private bool _resignRequested;
    private int? _expectedGameId;
    private int? _humanMoveGameId;
    private string? _humanMove;
    private TaskCompletionSource<string>? _humanMoveWaiter;

    public void RequestResign(int? expectedGameId)
    {
        lock (_sync)
        {
            _resignRequested = true;
            _expectedGameId = expectedGameId;
            if (_humanMoveWaiter is not null && (expectedGameId is null || expectedGameId == _humanMoveGameId))
            {
                _humanMoveWaiter.TrySetResult("resign");
                _humanMoveWaiter = null;
            }
        }
    }

    public void RequestHumanMove(int gameId, string vertex)
    {
        lock (_sync)
        {
            if (_humanMoveWaiter is not null && _humanMoveGameId == gameId)
            {
                _humanMoveWaiter.TrySetResult(vertex);
                _humanMoveWaiter = null;
                return;
            }
            _humanMoveGameId = gameId;
            _humanMove = vertex;
        }
    }

    public async Task<string> WaitForHumanMoveAsync(int gameId, CancellationToken cancellationToken)
    {
        Task<string> task;
        lock (_sync)
        {
            if (_resignRequested && (_expectedGameId is null || _expectedGameId == gameId))
            {
                _resignRequested = false;
                _expectedGameId = null;
                return "resign";
            }
            if (_humanMoveGameId == gameId && !string.IsNullOrWhiteSpace(_humanMove))
            {
                var move = _humanMove;
                _humanMove = null;
                return move;
            }
            _humanMoveGameId = gameId;
            _humanMoveWaiter = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            task = _humanMoveWaiter.Task;
        }
        return await task.WaitAsync(cancellationToken);
    }

    public bool ConsumeResignRequest(int currentGameId)
    {
        lock (_sync)
        {
            if (!_resignRequested) return false;
            if (_expectedGameId is not null && _expectedGameId != currentGameId)
            {
                _resignRequested = false;
                _expectedGameId = null;
                return false;
            }
            _resignRequested = false;
            _expectedGameId = null;
            return true;
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
    private const string DefaultEngineCommand = "dotnet run --project KifuwarabeGo2026.Reference.Communication.Gtp.Host\\KifuwarabeGo2026.Reference.Communication.Gtp.Host.csproj";

    private readonly List<CgosAccount> _accounts = new();
    private readonly Dictionary<string, string> _engineOptions = new(StringComparer.Ordinal);

    private CgosClientOptions()
    {
    }

    public string Host { get; private set; } = "uec-go.com";

    public int Port { get; private set; } = 6809;

    public string EngineCommand { get; private set; } = DefaultEngineCommand;

    public string LogDirectory { get; private set; } = Path.Combine("Logs", "Cgos");

    public bool ShowHelp { get; private set; }

    public bool AdminMode { get; private set; }
    public bool HumanMode { get; private set; }

    public int? ParentProcessId { get; private set; }

    public long? ParentProcessStartTimeUtcTicks { get; private set; }

    public IReadOnlyList<CgosAccount> Accounts => _accounts;

    public IReadOnlyDictionary<string, string> EngineOptions => _engineOptions;

    public static CgosClientOptions Parse(string[] args)
    {
        var options = new CgosClientOptions();
        var selectedAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? loginName = null;
        string? password = null;

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
                case "--engine-option":
                    var engineOption = ReadValue(args, ref index, arg);
                    var separator = engineOption.IndexOf('=');
                    if (separator < 1)
                        throw new ArgumentException("--engine-option must use ID=VALUE format.");
                    options._engineOptions[engineOption[..separator]] = engineOption[(separator + 1)..];
                    break;
                case "--log-directory":
                    options.LogDirectory = ReadValue(args, ref index, arg);
                    break;
                case "--admin":
                    options.AdminMode = true;
                    break;
                case "--human":
                    options.HumanMode = true;
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
                case "--login-name":
                    loginName = ReadValue(args, ref index, arg);
                    break;
                case "--password":
                    password = ReadValue(args, ref index, arg);
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
            options._accounts.Add(CreateAccount(account, loginName, password));
        }

        return options;
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("CGOS communication client for Kifuwarabe Go 2026");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project KifuwarabeGo2026.Reference.Communication.Cgos.Host -- [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --account black|white      Login account. Default: black");
        writer.WriteLine("  --login-name NAME          CGOS login name (overrides the account default).");
        writer.WriteLine("  --password PASSWORD        CGOS plain-text password (overrides the account default).");
        writer.WriteLine("  --both                     Login with both KifuwarabeB and KifuwarabeW.");
        writer.WriteLine("  --host HOST                CGOS host. Default: uec-go.com");
        writer.WriteLine("  --port PORT                CGOS port. Default: 6809");
        writer.WriteLine("  --engine-command COMMAND   GTP engine command line.");
        writer.WriteLine("  --engine-option ID=VALUE   GUI option sent to a supporting GTP engine.");
        writer.WriteLine("  --log-directory DIR        Log directory. Default: Logs\\Cgos");
        writer.WriteLine("  --admin                    Login without a GTP engine and relay admin commands from stdin.");
        writer.WriteLine("  --human                    Wait for GUI move commands instead of starting a GTP engine.");
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

    private static CgosAccount CreateAccount(string account, string? loginName, string? password)
    {
        var defaults = account.ToLowerInvariant() switch
        {
            "black" or "b" => new CgosAccount("black", "KifuwarabeB", "KifuwarabeB"),
            "white" or "w" => new CgosAccount("white", "KifuwarabeW", "KifuwarabeW"),
            _ => throw new ArgumentException("--account must be black or white."),
        };
        return defaults with
        {
            UserName = loginName ?? defaults.UserName,
            Password = password ?? defaults.Password,
        };
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
        var session = CreateNetworkSession(_options, _account, Log);
        var admin = new CgosAdminStateMachine();
        await session.RunAsync(
            (message, token) =>
            {
                if (admin.Handle(message))
                {
                    Log("# Admin login accepted. Command input is ready.");
                    _ = CgosStandardInputRelay.Start(
                        (command, relayToken) => RelayAdminCommandAsync(session, admin, command, relayToken),
                        ex => Log("# Admin input relay failed: " + ex.Message),
                        token);
                }

                return Task.CompletedTask;
            },
            passwordSentAsync: null,
            cancellationToken);
    }

    private async Task RelayAdminCommandAsync(
        CgosNetworkSession session,
        CgosAdminStateMachine admin,
        string command,
        CancellationToken cancellationToken)
    {
        if (!admin.TryCreateCommand(command, out var typedCommand) || typedCommand is null)
        {
            Log("# Unsupported admin command ignored: " + command);
            return;
        }
        if (typedCommand is CgosQuit)
        {
            await session.SendQuitAsync();
            return;
        }
        await session.SendAsync(typedCommand);
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

    private static CgosNetworkSession CreateNetworkSession(CgosClientOptions options, CgosAccount account, Action<string> log) =>
        new(
            new CgosConnectionOptions(options.Host, options.Port),
            new CgosCredentials(account.UserName, account.Password),
            log);
}

internal sealed class CgosClient
{
    private readonly HashSet<string> _consumedButtonOptions = new(StringComparer.Ordinal);
    private const string ClientId = "e1";

    private readonly CgosClientOptions _options;
    private readonly CgosAccount _account;
    private readonly CgosPlayerControl _playerControl;
    private readonly object _logLock = new();
    private readonly string _logPath;

    public CgosClient(
        CgosClientOptions options,
        CgosAccount account,
        CgosPlayerControl playerControl)
    {
        _options = options;
        _account = account;
        _playerControl = playerControl;
        _logPath = Path.Combine(options.LogDirectory, $"cgos-{account.Label}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var player = new CgosPlayerStateMachine(
            _account.UserName,
            _options.HumanMode ? null : CreatePlayerEngineAsync,
            _options.HumanMode
                ? async (gameId, color, token) =>
                {
                    Log($"# Waiting for GUI human move. game={gameId} color={color}");
                    var move = await _playerControl.WaitForHumanMoveAsync(gameId, token);
                    Log($"# GUI human move: {move}");
                    return move;
                }
                : null,
            gameId =>
            {
                var requested = _playerControl.ConsumeResignRequest(gameId);
                if (requested) Log("# GUI requested resignation.");
                return requested;
            },
            Log);
        var session = new CgosNetworkSession(
            new CgosConnectionOptions(_options.Host, _options.Port),
            new CgosCredentials(_account.UserName, _account.Password),
            Log);
        await session.RunAsync(
            async (message, token) =>
            {
                var command = await player.HandleAsync(message, session.ServerSupportsAnalyze, token);
                EmitNotification(message, command);
                if (command is not null) await session.SendAsync(command);
            },
            passwordSentAsync: null,
            cancellationToken);
    }

    private void EmitNotification(CgosServerMessage message, CgosClientCommand? command)
    {
        CgosNotification? notification = message switch
        {
            CgosMatchSetup setup => new CgosSetupNotification(
                _account.Label, setup.GameId, setup.BoardSize, setup.Komi, setup.MainTimeMilliseconds,
                setup.WhitePlayer, setup.BlackPlayer, setup.MoveHistory),
            CgosMovePlayed play => new CgosPlayNotification(
                _account.Label, play.Color, play.Vertex, play.TimeLeftMilliseconds),
            CgosGenMoveRequested genmove when command is CgosMove move => new CgosPlayNotification(
                _account.Label, genmove.Color, move.Vertex, null, move.AnalysisJson),
            CgosGameOver gameOver => new CgosGameOverNotification(_account.Label, gameOver.Result),
            _ => null,
        };
        if (notification is not null) Console.WriteLine(CgosNotificationJsonLines.Format(notification));
    }

    private async Task<ICgosPlayerEngine> CreatePlayerEngineAsync(
        CgosPlayerEngineSetup setup,
        CancellationToken cancellationToken)
    {
        var process = new GtpEngineProcess(_options.EngineCommand, _options.LogDirectory, _account.Label, Log);
        try
        {
            await process.StartAsync(cancellationToken);
            await ApplyEngineOptionsAsync(process, cancellationToken);
            var supportsAnalyze = await SupportsCommandAsync(process, "cgos-genmove_analyze", cancellationToken);
            return new CgosGtpPlayerEngineAdapter(process, setup.LocalColor, supportsAnalyze, Log);
        }
        catch
        {
            await process.DisposeAsync();
            throw;
        }
    }

    private static async Task<bool> SupportsCommandAsync(GtpEngineProcess engine, string command, CancellationToken cancellationToken)
    {
        try
        {
            var commands = await engine.CommandAsync("list_commands", cancellationToken);
            return commands.Any(value => value.Equals(command, StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// 対応するGTPエンジンへGUIオプションを送信します。
    /// </summary>
    private async Task ApplyEngineOptionsAsync(GtpEngineProcess engine, CancellationToken cancellationToken)
    {
        if (_options.EngineOptions.Count == 0) return;

        if (await TryApplyJsonEngineOptionsAsync(engine, cancellationToken)) return;

        IReadOnlyList<string> known;
        var optionsCommand = "kfw-options";
        var setOptionCommand = "kfw-set-option";
        try
        {
            known = await engine.CommandAsync($"known_command {optionsCommand}", cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (!known.Any(value => value.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            optionsCommand = "gui_options";
            setOptionCommand = "gui_setoption";
            known = await engine.CommandAsync($"known_command {optionsCommand}", cancellationToken);
            if (!known.Any(value => value.Equals("true", StringComparison.OrdinalIgnoreCase))) return;
        }

        var optionsJson = await engine.CommandAsync(optionsCommand, cancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(string.Join('\n', optionsJson));
        if (!document.RootElement.TryGetProperty("version", out var version) || version.GetInt32() != 1)
            throw new InvalidOperationException($"Unsupported {optionsCommand} version.");
        if (!document.RootElement.TryGetProperty("options", out var definitions)) return;

        foreach (var option in _options.EngineOptions)
        {
            var definition = definitions.EnumerateArray().FirstOrDefault(candidate =>
                candidate.TryGetProperty("id", out var id) && id.GetString() == option.Key);
            if (definition.ValueKind == System.Text.Json.JsonValueKind.Undefined) continue;

            var type = definition.TryGetProperty("type", out var typeProperty)
                ? typeProperty.GetString()
                : null;
            if (type?.Equals("button", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (bool.TryParse(option.Value, out var queued) &&
                    queued &&
                    _consumedButtonOptions.Add(option.Key))
                {
                    await engine.CommandAsync($"{setOptionCommand} {option.Key}", cancellationToken);
                }
                continue;
            }

            await engine.CommandAsync($"{setOptionCommand} {option.Key} {option.Value}", cancellationToken);
        }
    }

    private async Task<bool> TryApplyJsonEngineOptionsAsync(GtpEngineProcess engine, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> known;
        try
        {
            known = await engine.CommandAsync("known_command kfw-describe-options", cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (!known.Any(value => value.Equals("true", StringComparison.OrdinalIgnoreCase))) return false;

        var schemaLines = await engine.CommandAsync("kfw-describe-options play player", cancellationToken);
        using var schema = System.Text.Json.JsonDocument.Parse(string.Join('\n', schemaLines));
        var root = schema.RootElement;
        if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1)
            throw new InvalidOperationException("Unsupported kfw-describe-options version.");
        if (!root.TryGetProperty("app", out var app) || app.GetString() != "play" ||
            !root.TryGetProperty("role", out var role) || role.GetString() != "player")
            throw new InvalidOperationException("kfw-describe-options returned a different app or role.");
        if (!root.TryGetProperty("options", out var definitions) || definitions.ValueKind != System.Text.Json.JsonValueKind.Array)
            throw new InvalidOperationException("kfw-describe-options returned no options array.");

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var actions = new List<string>();
        foreach (var option in _options.EngineOptions)
        {
            var definition = definitions.EnumerateArray().FirstOrDefault(candidate =>
                candidate.TryGetProperty("id", out var id) && id.GetString() == option.Key);
            if (definition.ValueKind == System.Text.Json.JsonValueKind.Undefined) continue;

            var type = definition.TryGetProperty("type", out var typeProperty)
                ? typeProperty.GetString()?.ToLowerInvariant()
                : null;
            if (type == "action")
            {
                if (bool.TryParse(option.Value, out var queued) && queued && _consumedButtonOptions.Add(option.Key))
                    actions.Add(option.Key);
                continue;
            }

            if (TryConvertJsonOption(definition, type, option.Value, out var typedValue))
                values[option.Key] = typedValue;
        }

        if (values.Count > 0)
        {
            var request = System.Text.Json.JsonSerializer.Serialize(new { version = 1, values });
            await engine.CommandAsync($"kfw-patch-options play player {request}", cancellationToken);
        }

        foreach (var action in actions)
            await engine.CommandAsync($"kfw-invoke-option play player {action}", cancellationToken);
        return true;
    }

    private static bool TryConvertJsonOption(
        System.Text.Json.JsonElement definition,
        string? type,
        string value,
        out object? typedValue)
    {
        typedValue = null;
        switch (type)
        {
            case "boolean":
                if (!bool.TryParse(value, out var booleanValue)) return false;
                typedValue = booleanValue;
                return true;
            case "integer":
                if (!int.TryParse(value, out var integerValue)) return false;
                if (definition.TryGetProperty("minimum", out var minimum) && integerValue < minimum.GetInt32()) return false;
                if (definition.TryGetProperty("maximum", out var maximum) && integerValue > maximum.GetInt32()) return false;
                typedValue = integerValue;
                return true;
            case "enum":
                if (!definition.TryGetProperty("values", out var enumValues) ||
                    !enumValues.EnumerateArray().Any(candidate => candidate.GetString() == value)) return false;
                typedValue = value;
                return true;
            case "string":
            case "file":
                if (definition.TryGetProperty("maximumLength", out var maximumLength) && value.Length > maximumLength.GetInt32()) return false;
                typedValue = value;
                return true;
            default:
                return false;
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
        return serverSupportsAnalyze ? ClientId + " genmove_analyze" : ClientId;
    }

    private static string StripRank(string programName)
    {
        var rankIndex = programName.IndexOf('(');
        return rankIndex < 0 ? programName : programName[..rankIndex];
    }
}

internal sealed class CgosGtpPlayerEngineAdapter : ICgosPlayerEngine
{
    private readonly GtpEngineProcess _process;
    private readonly string _localColor;
    private readonly Action<string> _log;

    public CgosGtpPlayerEngineAdapter(
        GtpEngineProcess process,
        string localColor,
        bool supportsAnalyze,
        Action<string> log)
    {
        _process = process;
        _localColor = localColor;
        SupportsAnalyze = supportsAnalyze;
        _log = log;
    }

    public bool SupportsAnalyze { get; }

    public async Task ConfigureAsync(int boardSize, decimal komi, CancellationToken cancellationToken = default)
    {
        await _process.CommandAsync("boardsize " + boardSize.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await _process.CommandAsync("komi " + komi.ToString(CultureInfo.InvariantCulture), cancellationToken);
        await _process.CommandAsync("clear_board", cancellationToken);
    }

    public Task PlayAsync(
        string color,
        string vertex,
        long timeLeftMilliseconds,
        CancellationToken cancellationToken = default) =>
        _process.PlayAsync(
            [color, vertex, timeLeftMilliseconds.ToString(CultureInfo.InvariantCulture)],
            cancellationToken);

    public async Task<CgosGeneratedMove> GenerateMoveAsync(
        string color,
        bool includeAnalysis,
        CancellationToken cancellationToken = default)
    {
        var response = await _process.CommandAsync(
            (includeAnalysis ? "cgos-genmove_analyze " : "genmove ") + color,
            cancellationToken);
        if (!includeAnalysis)
        {
            var move = response.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(move))
                throw new InvalidOperationException("GTP engine returned an empty genmove response.");
            _log($"# Generated {_localColor} move: {move}");
            return new CgosGeneratedMove(move.ToLowerInvariant());
        }

        var json = response.FirstOrDefault(line => line.StartsWith('{'));
        var play = response.FirstOrDefault(line => line.StartsWith("play ", StringComparison.OrdinalIgnoreCase));
        if (json is null || play is null)
            throw new InvalidOperationException("GTP engine returned an invalid cgos-genmove_analyze response.");
        using var document = System.Text.Json.JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new InvalidOperationException("GTP engine returned non-object analysis JSON.");
        var analyzedMove = play[5..].Trim();
        if (analyzedMove.Length == 0)
            throw new InvalidOperationException("GTP engine returned an empty analyzed move.");
        _log($"# Generated {_localColor} move: {analyzedMove} {json}");
        return new CgosGeneratedMove(analyzedMove.ToLowerInvariant(), json);
    }

    public ValueTask DisposeAsync() => _process.DisposeAsync();
}

internal sealed class GtpEngineProcess : IAsyncDisposable
{
    private readonly string _commandLine;
    private readonly string _logPath;
    private readonly Action<string> _progressLog;
    private Process? _process;
    private StreamWriter? _input;
    private StreamReader? _output;

    public GtpEngineProcess(string commandLine, string logDirectory, string accountLabel, Action<string> progressLog)
    {
        _commandLine = commandLine;
        _logPath = Path.Combine(logDirectory, $"gtp-{accountLabel}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        _progressLog = progressLog;
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
        var stopwatch = Stopwatch.StartNew();
        _progressLog("# GTP response wait started: " + command);
        try
        {
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
        }
        finally
        {
            stopwatch.Stop();
            _progressLog($"# GTP response wait completed in {stopwatch.Elapsed.TotalSeconds:0.000} seconds: {command}");
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
