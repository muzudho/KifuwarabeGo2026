namespace KifuwarabeGo2026.GtpExtensions.Capabilities;

using KifuwarabeGo2026.GtpExtensions.Protocol;

/// <summary>
/// Probes command support while preserving unsupported, contradictory, and unavailable results.
/// </summary>
public sealed class GtpCapabilityProbe
{
    public static IReadOnlyList<string> InitialPositionCommands { get; } =
    [
        "fixed_handicap",
        "set_free_handicap",
        "place_free_handicap",
        "loadsgf",
        "begin_position",
        "add_black",
        "add_white",
        "set_to_play",
        "commit_position",
        "abort_position",
    ];

    public Task<GtpCapabilitySet> ProbeInitialPositionAsync(
        IGtpCommandSession session,
        CancellationToken cancellationToken = default) =>
        ProbeAsync(session, InitialPositionCommands, cancellationToken);

    public async Task<GtpCapabilitySet> ProbeAsync(
        IGtpCommandSession session,
        IEnumerable<string> commands,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(commands);

        var requestedCommands = NormalizeCommands(commands);
        var diagnostics = new List<string>();
        var engineName = await QueryIdentityAsync(session, "name", diagnostics, cancellationToken);
        var engineVersion = await QueryIdentityAsync(session, "version", diagnostics, cancellationToken);
        var knownResults = new Dictionary<string, KnownCommandResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in requestedCommands)
        {
            knownResults[command] = await QueryKnownCommandAsync(session, command, diagnostics, cancellationToken);
        }

        var listedCommands = await QueryListCommandsAsync(session, diagnostics, cancellationToken);
        var capabilities = requestedCommands
            .Select(command => ResolveCapability(command, knownResults[command], listedCommands))
            .ToArray();

        return new GtpCapabilitySet(engineName, engineVersion, capabilities, diagnostics);
    }

    private static string[] NormalizeCommands(IEnumerable<string> commands)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("A command name cannot be empty.", nameof(commands));
            }

            var trimmed = command.Trim();
            if (trimmed.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"A command name cannot contain whitespace: '{command}'.", nameof(commands));
            }

            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
    }

    private static async Task<string?> QueryIdentityAsync(
        IGtpCommandSession session,
        string command,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        var attempt = await TrySendAsync(session, command, cancellationToken);
        if (attempt.Exception is not null)
        {
            diagnostics.Add($"{command}: {DescribeException(attempt.Exception)}");
            return null;
        }

        if (attempt.Response is not { IsSuccess: true } response || string.IsNullOrWhiteSpace(response.Payload))
        {
            diagnostics.Add($"{command}: the engine did not return a usable identity.");
            return null;
        }

        return response.Payload.Trim();
    }

    private static async Task<KnownCommandResult> QueryKnownCommandAsync(
        IGtpCommandSession session,
        string command,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        var fullCommand = $"known_command {command}";
        var attempt = await TrySendAsync(session, fullCommand, cancellationToken);
        if (attempt.Exception is not null)
        {
            var detail = DescribeException(attempt.Exception);
            diagnostics.Add($"{fullCommand}: {detail}");
            return new KnownCommandResult(null, detail);
        }

        if (attempt.Response is not { IsSuccess: true } response)
        {
            var detail = string.IsNullOrWhiteSpace(attempt.Response?.Payload)
                ? "The engine rejected known_command."
                : $"The engine rejected known_command: {attempt.Response.Payload}";
            diagnostics.Add($"{fullCommand}: {detail}");
            return new KnownCommandResult(null, detail);
        }

        if (response.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return new KnownCommandResult(true, null);
        }

        if (response.Payload.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return new KnownCommandResult(false, null);
        }

        var invalidDetail = $"Unexpected known_command response: '{response.Payload}'.";
        diagnostics.Add($"{fullCommand}: {invalidDetail}");
        return new KnownCommandResult(null, invalidDetail);
    }

    private static async Task<ListCommandsResult> QueryListCommandsAsync(
        IGtpCommandSession session,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        var attempt = await TrySendAsync(session, "list_commands", cancellationToken);
        if (attempt.Exception is not null)
        {
            var detail = DescribeException(attempt.Exception);
            diagnostics.Add($"list_commands: {detail}");
            return new ListCommandsResult(null, detail);
        }

        if (attempt.Response is not { IsSuccess: true } response)
        {
            var detail = string.IsNullOrWhiteSpace(attempt.Response?.Payload)
                ? "The engine rejected list_commands."
                : $"The engine rejected list_commands: {attempt.Response.Payload}";
            diagnostics.Add($"list_commands: {detail}");
            return new ListCommandsResult(null, detail);
        }

        var listedCommands = response.Payload
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new ListCommandsResult(listedCommands, null);
    }

    private static GtpCommandCapability ResolveCapability(
        string command,
        KnownCommandResult known,
        ListCommandsResult listed)
    {
        bool? listedSupport = listed.Commands is null ? null : listed.Commands.Contains(command);
        if (known.Support is { } knownSupport && listedSupport is { } listSupport)
        {
            if (knownSupport != listSupport)
            {
                return new GtpCommandCapability(
                    command,
                    GtpCommandSupport.Unknown,
                    GtpCapabilityEvidence.ContradictoryResponses,
                    $"known_command returned {knownSupport.ToString().ToLowerInvariant()}, but list_commands " +
                    $"{(listSupport ? "included" : "did not include")} the command.");
            }

            return new GtpCommandCapability(
                command,
                knownSupport ? GtpCommandSupport.Supported : GtpCommandSupport.Unsupported,
                GtpCapabilityEvidence.ConsistentResponses);
        }

        if (known.Support is { } knownOnly)
        {
            return new GtpCommandCapability(
                command,
                knownOnly ? GtpCommandSupport.Supported : GtpCommandSupport.Unsupported,
                GtpCapabilityEvidence.KnownCommand,
                listed.Detail);
        }

        if (listedSupport is { } listedOnly)
        {
            return new GtpCommandCapability(
                command,
                listedOnly ? GtpCommandSupport.Supported : GtpCommandSupport.Unsupported,
                GtpCapabilityEvidence.ListCommands,
                known.Detail);
        }

        return new GtpCommandCapability(
            command,
            GtpCommandSupport.Unknown,
            GtpCapabilityEvidence.Unavailable,
            JoinDetails(known.Detail, listed.Detail));
    }

    private static async Task<CommandAttempt> TrySendAsync(
        IGtpCommandSession session,
        string command,
        CancellationToken cancellationToken)
    {
        try
        {
            return new CommandAttempt(await session.SendAsync(command, cancellationToken), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new CommandAttempt(null, ex);
        }
    }

    private static string DescribeException(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";

    private static string? JoinDetails(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return $"{first} {second}";
    }

    private sealed record CommandAttempt(GtpCommandResult? Response, Exception? Exception);

    private sealed record KnownCommandResult(bool? Support, string? Detail);

    private sealed record ListCommandsResult(HashSet<string>? Commands, string? Detail);
}
