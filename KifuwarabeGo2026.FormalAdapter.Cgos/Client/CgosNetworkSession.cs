namespace KifuwarabeGo2026.FormalAdapter.Cgos.Client;

using System.Net.Sockets;
using System.Text;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

/// <summary>Owns one CGOS TCP connection, login exchange, and typed line transport.</summary>
public sealed class CgosNetworkSession
{
    private readonly CgosConnectionOptions _options;
    private readonly CgosCredentials _credentials;
    private readonly Action<string> _log;
    private StreamWriter? _writer;
    private bool _quitSent;

    public CgosNetworkSession(CgosConnectionOptions options, CgosCredentials credentials, Action<string>? log = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _log = log ?? (_ => { });
        if (string.IsNullOrWhiteSpace(options.Host)) throw new ArgumentException("A CGOS host is required.", nameof(options));
        if (options.Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(options), options.Port, "CGOS port is invalid.");
    }

    public bool ServerSupportsAnalyze { get; private set; }

    public async Task RunAsync(
        Func<CgosServerMessage, CancellationToken, Task> handleMessageAsync,
        Func<CancellationToken, Task>? passwordSentAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handleMessageAsync);
        var connectTimeout = _options.ConnectTimeout ?? TimeSpan.FromSeconds(15);
        var firstLineTimeout = _options.FirstServerLineTimeout ?? TimeSpan.FromSeconds(15);
        using var tcp = new TcpClient();
        _log($"# Connecting to {_options.Host}:{_options.Port}.");
        await tcp.ConnectAsync(_options.Host, _options.Port, cancellationToken).AsTask().WaitAsync(connectTimeout, cancellationToken);
        await using var stream = tcp.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n", AutoFlush = true };
        _writer = writer;
        var receivedAnyLine = false;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readTask = reader.ReadLineAsync(cancellationToken).AsTask();
                var line = receivedAnyLine ? await readTask : await readTask.WaitAsync(firstLineTimeout, cancellationToken);
                if (line is null) return;
                line = line.Trim();
                if (line.Length == 0) continue;
                receivedAnyLine = true;
                _log("> " + line);
                var message = CgosServerMessageParser.Parse(line);
                if (message is CgosServerError error) throw new InvalidOperationException("CGOS error: " + error.Message);
                if (await HandleLoginAsync(message, passwordSentAsync, cancellationToken)) continue;
                await handleMessageAsync(message, cancellationToken);
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try { await SendQuitAsync(); }
                catch (Exception exception) when (exception is IOException or InvalidOperationException or ObjectDisposedException)
                {
                    _log("# Could not send CGOS quit: " + exception.Message);
                }
            }
            _writer = null;
        }
    }

    public async Task SendAsync(CgosClientCommand command)
    {
        var writer = _writer ?? throw new InvalidOperationException("CGOS is not connected.");
        await writer.WriteLineAsync(CgosClientCommandFormatter.Format(command));
        _log("< " + CgosClientCommandFormatter.FormatForLog(command));
    }

    public async Task SendQuitAsync()
    {
        if (_writer is null || _quitSent) return;
        await SendAsync(new CgosQuit());
        _quitSent = true;
    }

    private async Task<bool> HandleLoginAsync(
        CgosServerMessage message,
        Func<CancellationToken, Task>? passwordSentAsync,
        CancellationToken cancellationToken)
    {
        switch (message)
        {
            case CgosProtocolAdvertised protocol:
                ServerSupportsAnalyze = protocol.SupportsGenMoveAnalyze;
                await SendAsync(new CgosClientIdentity("e1", ServerSupportsAnalyze));
                return true;
            case CgosUsernameRequested:
                await SendAsync(new CgosUsername(_credentials.Username));
                return true;
            case CgosPasswordRequested:
                await SendAsync(new CgosPassword(_credentials.Password));
                if (passwordSentAsync is not null) await passwordSentAsync(cancellationToken);
                return true;
            default:
                return false;
        }
    }
}
