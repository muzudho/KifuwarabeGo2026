namespace KifuwarabeGo2026.Engine;

using System.Text.Json;

internal sealed partial class GtpEngine
{
    private const int OptionsProtocolVersion = 1;
    private const int MaximumOptionTextLength = 10_000;

    private static readonly HashSet<string> PlayerOptionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "RandomMove", "AvoidEyes", "RandomSeed", "EngineTag", "DebugLogFile", "ClearCache",
    };

    private static readonly HashSet<string> PonnukiProviderOptionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "BoardSize", "InitialMoveCount", "RandomSeed",
    };

    private void ExecuteDescribeOptions(string[] tokens, out string response, out string? error)
    {
        response = "";
        if (!TryResolveOptionScope(tokens, "kfw-describe-options", out var app, out var role, out var optionIds, out error))
            return;

        response = JsonSerializer.Serialize(new
        {
            version = OptionsProtocolVersion,
            app,
            role,
            options = CreateOptionDescriptions(optionIds),
        });
    }

    private void ExecuteGetOptions(string[] tokens, out string response, out string? error)
    {
        response = "";
        if (!TryResolveOptionScope(tokens, "kfw-get-options", out var app, out var role, out var optionIds, out error))
            return;

        response = JsonSerializer.Serialize(new
        {
            version = OptionsProtocolVersion,
            app,
            role,
            values = CreateTypedOptionValues(optionIds),
        });
    }

    private void ExecutePatchOptions(string commandLine, string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length < 4 ||
            !TryResolveOptionScope(tokens[..3], "kfw-patch-options", out var app, out var role, out var optionIds, out error))
        {
            error ??= CreateOptionError("invalid-command", "usage: kfw-patch-options app role json");
            return;
        }

        if (!TryGetArgumentRemainder(commandLine, 3, out var json))
        {
            error = CreateOptionError("invalid-command", "usage: kfw-patch-options app role json");
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            error = CreateOptionError("invalid-json", ex.Message);
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("version", out var versionElement) ||
                !versionElement.TryGetInt32(out var version) || version != OptionsProtocolVersion ||
                !root.TryGetProperty("values", out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Object)
            {
                error = CreateOptionError("invalid-document", "version must be 1 and values must be an object");
                return;
            }

            var randomMove = _randomMove;
            var avoidEyes = _avoidEyes;
            var randomSeed = _randomSeed;
            var randomSeedChanged = false;
            var engineTag = _engineTag;
            var debugLogFile = _debugLogFile;
            var ponnukiBoardSize = _ponnukiBoardSize;
            var ponnukiInitialMoveCount = _ponnukiInitialMoveCount;
            var ponnukiRandomSeed = _ponnukiRandomSeed;
            var applied = new Dictionary<string, object?>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validationErrors = new List<object>();

            foreach (var property in valuesElement.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    validationErrors.Add(new { id = property.Name, message = "duplicate option id" });
                    continue;
                }

                if (!optionIds.Contains(property.Name))
                {
                    validationErrors.Add(new { id = property.Name, message = "option is not available for this app and role" });
                    continue;
                }

                switch (property.Name.ToLowerInvariant())
                {
                    case "randommove":
                        if (property.Value.ValueKind != JsonValueKind.String ||
                            !Enum.TryParse(property.Value.GetString(), true, out RandomMoveKind parsedRandomMove) ||
                            parsedRandomMove is not (RandomMoveKind.Normal or RandomMoveKind.ChebyshevDistanceFromStar))
                            validationErrors.Add(new { id = property.Name, message = "must be Normal or ChebyshevDistanceFromStar" });
                        else
                        {
                            randomMove = parsedRandomMove;
                            applied["RandomMove"] = parsedRandomMove.ToString();
                        }
                        break;
                    case "avoideyes":
                        if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                            validationErrors.Add(new { id = property.Name, message = "must be a boolean" });
                        else
                        {
                            avoidEyes = property.Value.GetBoolean();
                            applied["AvoidEyes"] = avoidEyes;
                        }
                        break;
                    case "randomseed":
                        if (property.Value.ValueKind != JsonValueKind.Number ||
                            !property.Value.TryGetInt32(out var parsedSeed) || parsedSeed < 0)
                            validationErrors.Add(new { id = property.Name, message = "must be a non-negative 32-bit integer" });
                        else if (app == "ponnuki" && role == "provider")
                        {
                            ponnukiRandomSeed = parsedSeed;
                            applied["RandomSeed"] = parsedSeed;
                        }
                        else
                        {
                            randomSeed = parsedSeed;
                            randomSeedChanged = true;
                            applied["RandomSeed"] = randomSeed;
                        }
                        break;
                    case "enginetag":
                        ValidateTextOption(property, "EngineTag", ref engineTag, applied, validationErrors);
                        break;
                    case "debuglogfile":
                        ValidateTextOption(property, "DebugLogFile", ref debugLogFile, applied, validationErrors);
                        break;
                    case "clearcache":
                        validationErrors.Add(new { id = property.Name, message = "action options must be invoked with kfw-invoke-option" });
                        break;
                    case "boardsize":
                        if (property.Value.ValueKind != JsonValueKind.Number ||
                            !property.Value.TryGetInt32(out var parsedBoardSize) || parsedBoardSize != 9)
                            validationErrors.Add(new { id = property.Name, message = "Ponnuki v1 BoardSize must be 9" });
                        else
                        {
                            ponnukiBoardSize = parsedBoardSize;
                            applied["BoardSize"] = parsedBoardSize;
                        }
                        break;
                    case "initialmovecount":
                        if (property.Value.ValueKind != JsonValueKind.Number ||
                            !property.Value.TryGetInt32(out var parsedMoveCount) || parsedMoveCount is < 0 or > 200)
                            validationErrors.Add(new { id = property.Name, message = "must be an integer from 0 through 200" });
                        else
                        {
                            ponnukiInitialMoveCount = parsedMoveCount;
                            applied["InitialMoveCount"] = parsedMoveCount;
                        }
                        break;
                }
            }

            if (validationErrors.Count > 0)
            {
                error = JsonSerializer.Serialize(new
                {
                    version = OptionsProtocolVersion,
                    code = "option-validation-failed",
                    message = "No options were changed.",
                    errors = validationErrors,
                });
                return;
            }

            // Commit only after every requested value has passed validation.
            _randomMove = randomMove;
            _avoidEyes = avoidEyes;
            _randomSeed = randomSeed;
            if (randomSeedChanged) _random = new Random(randomSeed);
            _engineTag = engineTag;
            _debugLogFile = debugLogFile;
            _ponnukiBoardSize = ponnukiBoardSize;
            _ponnukiInitialMoveCount = ponnukiInitialMoveCount;
            _ponnukiRandomSeed = ponnukiRandomSeed;

            response = JsonSerializer.Serialize(new
            {
                version = OptionsProtocolVersion,
                app,
                role,
                applied,
            });
            error = null;
        }
    }

    private void ExecuteInvokeOption(string[] tokens, out string response, out string? error)
    {
        response = "";
        error = null;
        if (tokens.Length != 4 ||
            !TryResolveOptionScope(tokens[..3], "kfw-invoke-option", out var app, out var role, out var optionIds, out error))
        {
            error ??= CreateOptionError("invalid-command", "usage: kfw-invoke-option app role option-id");
            return;
        }

        var optionId = tokens[3];
        if (!optionIds.Contains(optionId))
        {
            error = CreateOptionError("option-not-available", $"option {optionId} is not available for this app and role", optionId);
            return;
        }

        if (!optionId.Equals("ClearCache", StringComparison.OrdinalIgnoreCase))
        {
            error = CreateOptionError("option-not-action", $"option {optionId} is not an action", optionId);
            return;
        }

        response = JsonSerializer.Serialize(new
        {
            version = OptionsProtocolVersion,
            app,
            role,
            invoked = "ClearCache",
        });
        error = null;
    }

    private static bool TryResolveOptionScope(
        string[] tokens,
        string command,
        out string app,
        out string role,
        out HashSet<string> optionIds,
        out string? error)
    {
        app = tokens.Length > 1 ? tokens[1].ToLowerInvariant() : "";
        role = tokens.Length > 2 ? tokens[2].ToLowerInvariant() : "";
        optionIds = [];
        error = null;

        if (tokens.Length != 3)
        {
            error = CreateOptionError("invalid-command", $"usage: {command} app role");
            return false;
        }

        if ((app == "play" && role == "player") || (app == "ponnuki" && role == "player"))
        {
            optionIds = PlayerOptionIds;
            return true;
        }

        if (app == "ponnuki" && role == "provider")
        {
            optionIds = PonnukiProviderOptionIds;
            return true;
        }

        error = CreateOptionError("unsupported-app-role", $"unsupported app and role: {app} {role}");
        return false;
    }

    private static object[] CreateOptionDescriptions(HashSet<string> optionIds)
    {
        var descriptions = new List<object>();
        if (optionIds.Contains("RandomMove")) descriptions.Add(new { id = "RandomMove", label = "RandomMove", type = "enum", @default = "ChebyshevDistanceFromStar", values = new[] { "Normal", "ChebyshevDistanceFromStar" }, apply = "immediate" });
        if (optionIds.Contains("AvoidEyes")) descriptions.Add(new { id = "AvoidEyes", label = "AvoidEyes", type = "boolean", @default = true, apply = "immediate" });
        if (optionIds.Contains("RandomSeed")) descriptions.Add(new { id = "RandomSeed", label = "RandomSeed", type = "integer", @default = 0, minimum = 0, maximum = int.MaxValue, apply = "immediate" });
        if (optionIds.Contains("EngineTag")) descriptions.Add(new { id = "EngineTag", label = "EngineTag", type = "string", @default = "", maximumLength = MaximumOptionTextLength, apply = "immediate" });
        if (optionIds.Contains("DebugLogFile")) descriptions.Add(new { id = "DebugLogFile", label = "DebugLogFile", type = "file", @default = "", maximumLength = MaximumOptionTextLength, apply = "restart" });
        if (optionIds.Contains("ClearCache")) descriptions.Add(new { id = "ClearCache", label = "ClearCache", type = "action", apply = "immediate" });
        if (optionIds.Contains("BoardSize")) descriptions.Add(new { id = "BoardSize", label = "BoardSize", type = "integer", @default = 9, minimum = 9, maximum = 9, apply = "next-start" });
        if (optionIds.Contains("InitialMoveCount")) descriptions.Add(new { id = "InitialMoveCount", label = "InitialMoveCount", type = "integer", @default = 20, minimum = 0, maximum = 200, apply = "next-start" });
        return descriptions.ToArray();
    }

    private Dictionary<string, object?> CreateTypedOptionValues(HashSet<string> optionIds)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (optionIds.Contains("RandomMove")) values["RandomMove"] = _randomMove.ToString();
        if (optionIds.Contains("AvoidEyes")) values["AvoidEyes"] = _avoidEyes;
        if (optionIds.Contains("RandomSeed")) values["RandomSeed"] = _randomSeed;
        if (optionIds.Contains("EngineTag")) values["EngineTag"] = _engineTag;
        if (optionIds.Contains("DebugLogFile")) values["DebugLogFile"] = _debugLogFile;
        if (optionIds.Contains("BoardSize")) values["BoardSize"] = _ponnukiBoardSize;
        if (optionIds.Contains("InitialMoveCount")) values["InitialMoveCount"] = _ponnukiInitialMoveCount;
        if (ReferenceEquals(optionIds, PonnukiProviderOptionIds) && optionIds.Contains("RandomSeed")) values["RandomSeed"] = _ponnukiRandomSeed;
        return values;
    }

    private static void ValidateTextOption(
        JsonProperty property,
        string canonicalId,
        ref string target,
        Dictionary<string, object?> applied,
        List<object> errors)
    {
        if (property.Value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new { id = property.Name, message = "must be a string" });
            return;
        }

        var value = property.Value.GetString() ?? "";
        if (value.Length > MaximumOptionTextLength)
        {
            errors.Add(new { id = property.Name, message = $"must be at most {MaximumOptionTextLength} characters" });
            return;
        }

        target = value;
        applied[canonicalId] = value;
    }

    private static string CreateOptionError(string code, string message, string? id = null) =>
        JsonSerializer.Serialize(new
        {
            version = OptionsProtocolVersion,
            code,
            message,
            errors = id is null ? Array.Empty<object>() : new object[] { new { id, message } },
        });

    private static bool TryGetArgumentRemainder(string commandLine, int completedTokenCount, out string remainder)
    {
        var index = 0;
        for (var token = 0; token < completedTokenCount; token++)
        {
            while (index < commandLine.Length && char.IsWhiteSpace(commandLine[index])) index++;
            if (index >= commandLine.Length)
            {
                remainder = "";
                return false;
            }
            while (index < commandLine.Length && !char.IsWhiteSpace(commandLine[index])) index++;
        }

        while (index < commandLine.Length && char.IsWhiteSpace(commandLine[index])) index++;
        remainder = index < commandLine.Length ? commandLine[index..] : "";
        return remainder.Length > 0;
    }
}
