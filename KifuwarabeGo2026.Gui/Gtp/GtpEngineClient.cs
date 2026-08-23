namespace KifuwarabeGo2026.Gui.Gtp;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Reference.Communication.Gtp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class GtpEngineClient : IAsyncDisposable, IGtpCommandTransport
{
    private readonly GtpEngineSettings _settings;
    private readonly TimeSpan _commandTimeout;
    private Process? _process;
    private StreamWriter? _logWriter;

    public GtpEngineClient(GtpEngineSettings settings, TimeSpan? commandTimeout = null)
    {
        _settings = settings;
        _commandTimeout = commandTimeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process is not null)
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _settings.ExecutablePath,
            Arguments = _settings.Arguments,
            WorkingDirectory = _settings.WorkingDirectory.Value,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start GTP engine: {_settings.ExecutablePath}");

        if (_settings.EnableGtpLog)
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            var logStream = new FileStream(
                Path.Combine(AppContext.BaseDirectory, "logs", "gtp.log"),
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            _logWriter = new StreamWriter(logStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true,
            };
            await _logWriter.WriteLineAsync(FormatLogLine($"# start {_settings.Name} {DateTimeOffset.Now:O}")).WaitAsync(_commandTimeout, cancellationToken);
        }

        await ApplyGuiOptionsAsync(cancellationToken);
    }

    public async Task<GtpResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_process is null)
        {
            throw new InvalidOperationException("GTP engine has not been started.");
        }

        if (_process.HasExited)
        {
            throw new InvalidOperationException($"GTP engine exited with code {_process.ExitCode}.");
        }

        await LogAsync($"-> {command}", cancellationToken);
        await _process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken).WaitAsync(_commandTimeout, cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken).WaitAsync(_commandTimeout, cancellationToken);

        var response = await ReadResponseAsync(_process.StandardOutput, cancellationToken);
        await LogAsync($"<- {(response.IsSuccess ? "=" : "?")} {response.Payload}".TrimEnd(), cancellationToken);
        return response;
    }

    async ValueTask<GtpCommandResponse> IGtpCommandTransport.SendAsync(
        string command,
        CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync(command, cancellationToken);
        return new(response.IsSuccess, response.Payload);
    }

    public async Task SendCommandExpectSuccessAsync(string command, CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync(command, cancellationToken);
        response.ThrowIfError(command);
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is not null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    await SendCommandAsync("quit");
                }
            }
            catch
            {
                // Shutdown should not hide the original GUI-side failure.
            }

            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
            _process = null;
        }

        if (_logWriter is not null)
        {
            await _logWriter.DisposeAsync();
            _logWriter = null;
        }
    }

    private async Task<GtpResponse> ReadResponseAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).AsTask().WaitAsync(_commandTimeout, cancellationToken);
            if (line is null)
            {
                throw new EndOfStreamException("GTP engine closed stdout.");
            }

            if (line.Length == 0 && lines.Count == 0) continue;
            if (line.Length == 0) break;
            lines.Add(line);
        }

        if (lines.Count == 0) throw new InvalidOperationException("GTP engine returned an empty response.");

        var firstLine = lines[0];
        var success = firstLine.StartsWith("=", StringComparison.Ordinal);
        if (!success && !firstLine.StartsWith("?", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Invalid GTP response: {firstLine}");
        }

        var payloadLines = new List<string> { firstLine[1..].Trim() };
        payloadLines.AddRange(lines.Skip(1));
        return new GtpResponse(success, string.Join('\n', payloadLines).Trim());
    }

    /// <summary>
    /// 対応エンジンへ、プロファイルに保存されたGUIオプションを送信します。
    /// </summary>
    private async Task ApplyGuiOptionsAsync(CancellationToken cancellationToken)
    {
        if (_settings.GuiOptions is null || _settings.GuiOptions.Count == 0) return;

        var describeCommand = "kfw-describe-options";
        var knownDescribe = await SendCommandAsync($"known_command {describeCommand}", cancellationToken);
        if (knownDescribe.IsSuccess && knownDescribe.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            await ApplyJsonOptionsAsync(cancellationToken);
            return;
        }

        var optionsCommand = "kfw-options";
        var setOptionCommand = "kfw-set-option";
        var knownCommand = await SendCommandAsync($"known_command {optionsCommand}", cancellationToken);
        if (!knownCommand.IsSuccess || !knownCommand.Payload.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            optionsCommand = "gui_options";
            setOptionCommand = "gui_setoption";
            knownCommand = await SendCommandAsync($"known_command {optionsCommand}", cancellationToken);
            if (!knownCommand.IsSuccess || !knownCommand.Payload.Equals("true", StringComparison.OrdinalIgnoreCase)) return;
        }

        var optionsResponse = await SendCommandAsync(optionsCommand, cancellationToken);
        optionsResponse.ThrowIfError(optionsCommand);
        var document = GtpGuiOptionsDocument.Parse(optionsResponse.Payload);
        if (document.Version != 1) throw new InvalidOperationException($"Unsupported {optionsCommand} version: {document.Version}");

        foreach (var savedOption in _settings.GuiOptions)
        {
            var definition = document.Options.FirstOrDefault(option => option.Id.Equals(savedOption.Key, StringComparison.Ordinal));
            if (definition is null) continue;
            if (definition.Type.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(savedOption.Value, out var queued) && queued)
                    await SendCommandExpectSuccessAsync($"{setOptionCommand} {savedOption.Key}", cancellationToken);
                continue;
            }
            if (definition.Type.Equals("combo", StringComparison.OrdinalIgnoreCase) &&
                !definition.Vars.Contains(savedOption.Value, StringComparer.Ordinal))
                continue;
            if (definition.Type.Equals("check", StringComparison.OrdinalIgnoreCase) &&
                !bool.TryParse(savedOption.Value, out _)) continue;
            if (definition.Type.Equals("spin", StringComparison.OrdinalIgnoreCase) &&
                (!int.TryParse(savedOption.Value, out var number) ||
                 number < (definition.Min ?? int.MinValue) || number > (definition.Max ?? int.MaxValue))) continue;

            await SendCommandExpectSuccessAsync($"{setOptionCommand} {savedOption.Key} {savedOption.Value}", cancellationToken);
        }
    }

    private async Task ApplyJsonOptionsAsync(CancellationToken cancellationToken)
    {
        var app = _settings.AppId.ToLowerInvariant();
        var role = _settings.Role.ToLowerInvariant();
        var describeCommand = $"kfw-describe-options {app} {role}";
        var describeResponse = await SendCommandAsync(describeCommand, cancellationToken);
        describeResponse.ThrowIfError(describeCommand);
        var schema = GtpOptionSchemaDocument.Parse(describeResponse.Payload);
        if (schema.Version != 1) throw new InvalidOperationException($"Unsupported kfw-describe-options version: {schema.Version}");
        if (!schema.App.Equals(app, StringComparison.OrdinalIgnoreCase) ||
            !schema.Role.Equals(role, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("kfw-describe-options returned a different app or role.");

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var actions = new List<string>();
        int? boundBoardSize = null;
        foreach (var savedOption in _settings.GuiOptions!)
        {
            var definition = schema.Options.FirstOrDefault(option => option.Id.Equals(savedOption.Key, StringComparison.Ordinal));
            if (definition is null) continue;
            if (definition.Type.Equals("action", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(savedOption.Value, out var queued) && queued)
                    actions.Add(definition.Id);
                continue;
            }

            if (TryConvertSavedOption(definition, savedOption.Value, out var typedValue))
            {
                values[definition.Id] = typedValue;
                if (definition.Binding.Equals(GtpEngineGuiOptions.GtpBoardSizeBinding, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(savedOption.Value, out var boardSize))
                    boundBoardSize = boardSize;
            }
        }

        if (values.Count > 0)
        {
            var request = JsonSerializer.Serialize(new { version = 1, values });
            var patchCommand = $"kfw-patch-options {app} {role} {request}";
            await SendCommandExpectSuccessAsync(patchCommand, cancellationToken);
        }

        if (boundBoardSize is { } selectedBoardSize)
        {
            if (!GtpEngineGuiOptions.IsSupportedBoardSize(selectedBoardSize))
                throw new InvalidOperationException($"Board size {selectedBoardSize} is not supported by this GUI.");
            await SendCommandExpectSuccessAsync($"boardsize {selectedBoardSize}", cancellationToken);
        }

        foreach (var action in actions)
            await SendCommandExpectSuccessAsync($"kfw-invoke-option {app} {role} {action}", cancellationToken);
    }

    private static bool TryConvertSavedOption(
        GtpOptionSchemaDefinition definition,
        string value,
        out object? typedValue)
    {
        typedValue = null;
        switch (definition.Type.ToLowerInvariant())
        {
            case "boolean":
                if (!bool.TryParse(value, out var booleanValue)) return false;
                typedValue = booleanValue;
                return true;
            case "integer":
                if (!int.TryParse(value, out var integerValue) ||
                    integerValue < (definition.Minimum ?? int.MinValue) ||
                    integerValue > (definition.Maximum ?? int.MaxValue)) return false;
                typedValue = integerValue;
                return true;
            case "enum":
                if (!definition.Values.Contains(value, StringComparer.Ordinal)) return false;
                typedValue = value;
                return true;
            case "string":
            case "file":
                if (value.Length > (definition.MaximumLength ?? int.MaxValue)) return false;
                typedValue = value;
                return true;
            default:
                return false;
        }
    }

    private async Task LogAsync(string line, CancellationToken cancellationToken)
    {
        if (_logWriter is not null)
        {
            await _logWriter.WriteLineAsync(FormatLogLine(line).AsMemory(), cancellationToken).WaitAsync(_commandTimeout, cancellationToken);
        }
    }

    private string FormatLogLine(string line) => string.IsNullOrWhiteSpace(_settings.LogPrefix)
        ? line
        : $"{_settings.LogPrefix} {line}";
}
