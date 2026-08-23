namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Capabilities;

using System.Collections.ObjectModel;

/// <summary>
/// Contains an engine identity and an immutable snapshot of probed GTP capabilities.
/// </summary>
public sealed class GtpCapabilitySet
{
    private readonly ReadOnlyDictionary<string, GtpCommandCapability> _commands;
    private readonly ReadOnlyCollection<string> _diagnostics;

    public GtpCapabilitySet(
        string? engineName,
        string? engineVersion,
        IEnumerable<GtpCommandCapability> commands,
        IEnumerable<string>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(commands);

        EngineName = NormalizeIdentity(engineName);
        EngineVersion = NormalizeIdentity(engineVersion);
        var commandDictionary = new Dictionary<string, GtpCommandCapability>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command.Command))
            {
                throw new ArgumentException("A capability command name cannot be empty.", nameof(commands));
            }

            if (!commandDictionary.TryAdd(command.Command, command))
            {
                throw new ArgumentException($"Capability command '{command.Command}' is duplicated.", nameof(commands));
            }
        }

        _commands = new ReadOnlyDictionary<string, GtpCommandCapability>(commandDictionary);
        _diagnostics = Array.AsReadOnly(diagnostics?.ToArray() ?? []);
    }

    public string? EngineName { get; }

    public string? EngineVersion { get; }

    public IReadOnlyDictionary<string, GtpCommandCapability> Commands => _commands;

    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public GtpCommandCapability Get(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        return _commands.TryGetValue(command, out var capability)
            ? capability
            : new GtpCommandCapability(
                command,
                GtpCommandSupport.Unknown,
                GtpCapabilityEvidence.Unavailable,
                "The command was not included in this capability probe.");
    }

    private static string? NormalizeIdentity(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
