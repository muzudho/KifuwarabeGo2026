namespace KifuwarabeGo2026.FormalAdapter.Cgos.Observability;

using System.Text.Json;
using System.Text.Json.Serialization;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

public abstract record CgosNotification(string AccountLabel);

public sealed record CgosSetupNotification(
    string AccountLabel,
    int GameId,
    int BoardSize,
    decimal Komi,
    long MainTimeMilliseconds,
    string WhitePlayer,
    string BlackPlayer,
    IReadOnlyList<CgosHistoricalMove> MoveHistory) : CgosNotification(AccountLabel);

public sealed record CgosPlayNotification(
    string AccountLabel,
    string Color,
    string Vertex,
    long? TimeLeftMilliseconds,
    string? AnalysisJson = null,
    bool IsGenerated = false) : CgosNotification(AccountLabel);

public sealed record CgosGameOverNotification(string AccountLabel, string Result) : CgosNotification(AccountLabel);

public enum CgosRuntimeState
{
    Connecting,
    Connected,
    Protocol,
    Login,
    Ready,
    GtpWait,
    Running,
    Closed,
    Error,
}

public sealed record CgosRuntimeNotification(
    string AccountLabel,
    CgosRuntimeState State,
    string? Detail = null) : CgosNotification(AccountLabel);

/// <summary>Versioned, one-record-per-line transport for CGOS machine notifications.</summary>
public static class CgosNotificationJsonLines
{
    public const string Prefix = "@kifuwarabe-cgos-v1 ";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Format(CgosNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        object payload = notification switch
        {
            CgosSetupNotification value => new
            {
                version = 1, kind = "setup", value.AccountLabel, value.GameId, value.BoardSize, value.Komi,
                value.MainTimeMilliseconds, value.WhitePlayer, value.BlackPlayer, value.MoveHistory,
            },
            CgosPlayNotification value => new
            {
                version = 1, kind = "play", value.AccountLabel, value.Color, value.Vertex,
                value.TimeLeftMilliseconds, value.AnalysisJson, value.IsGenerated,
            },
            CgosGameOverNotification value => new
            {
                version = 1, kind = "gameover", value.AccountLabel, value.Result,
            },
            CgosRuntimeNotification value => new
            {
                version = 1, kind = "runtime", value.AccountLabel,
                state = value.State.ToString().ToLowerInvariant(), value.Detail,
            },
            _ => throw new ArgumentException("Unsupported CGOS notification type.", nameof(notification)),
        };
        return Prefix + JsonSerializer.Serialize(payload, Options);
    }

    public static bool TryParse(string? line, out CgosNotification? notification)
    {
        notification = null;
        if (string.IsNullOrWhiteSpace(line)) return false;
        var marker = line.IndexOf(Prefix, StringComparison.Ordinal);
        if (marker < 0) return false;
        try
        {
            using var document = JsonDocument.Parse(line[(marker + Prefix.Length)..]);
            var root = document.RootElement;
            if (root.GetProperty("version").GetInt32() != 1) return false;
            var kind = root.GetProperty("kind").GetString();
            var accountLabel = root.GetProperty("accountLabel").GetString() ?? "";
            notification = kind switch
            {
                "setup" => new CgosSetupNotification(
                    accountLabel,
                    root.GetProperty("gameId").GetInt32(),
                    root.GetProperty("boardSize").GetInt32(),
                    root.GetProperty("komi").GetDecimal(),
                    root.GetProperty("mainTimeMilliseconds").GetInt64(),
                    root.GetProperty("whitePlayer").GetString() ?? "",
                    root.GetProperty("blackPlayer").GetString() ?? "",
                    JsonSerializer.Deserialize<List<CgosHistoricalMove>>(root.GetProperty("moveHistory"), Options) ?? []),
                "play" => new CgosPlayNotification(
                    accountLabel,
                    root.GetProperty("color").GetString() ?? "",
                    root.GetProperty("vertex").GetString() ?? "",
                    root.TryGetProperty("timeLeftMilliseconds", out var time) ? time.GetInt64() : null,
                    root.TryGetProperty("analysisJson", out var analysis) ? analysis.GetString() : null,
                    root.TryGetProperty("isGenerated", out var generated) && generated.GetBoolean()),
                "gameover" => new CgosGameOverNotification(
                    accountLabel,
                    root.GetProperty("result").GetString() ?? ""),
                "runtime" when Enum.TryParse<CgosRuntimeState>(root.GetProperty("state").GetString(), true, out var state) =>
                    new CgosRuntimeNotification(
                        accountLabel,
                        state,
                        root.TryGetProperty("detail", out var detail) ? detail.GetString() : null),
                _ => null,
            };
            return notification is not null;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            return false;
        }
    }
}
