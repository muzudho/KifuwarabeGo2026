namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

public static class GtpEngineProfilePolicy
{
    public static GtpEngineProfile Normalize(GtpEngineProfile profile, string baseDirectory)
    {
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "Unnamed GTP Engine" : normalized.DisplayName.Trim();
        normalized.DefaultCgosLoginName = normalized.DefaultCgosLoginName?.Trim() ?? "";
        normalized.DefaultCgosPlainTextPassword ??= "";
        normalized.InitialPositionProfileId = string.IsNullOrWhiteSpace(normalized.InitialPositionProfileId) ? "auto" : normalized.InitialPositionProfileId.Trim();
        normalized.InitialPositionDetectedEngineName ??= "";
        normalized.InitialPositionDetectedEngineVersion ??= "";
        normalized.InitialPositionDetectedProfileId ??= "";
        normalized.ExecutablePath = ResolvePath(normalized.ExecutablePath, baseDirectory);
        normalized.WorkingDirectoryModel = normalized.WorkingDirectoryModel.IsEmpty
            ? WorkingDirectoryModel.FromString(Path.GetDirectoryName(normalized.ExecutablePath) ?? baseDirectory)
            : WorkingDirectoryModel.FromString(ResolvePath(normalized.WorkingDirectoryModel.Value, baseDirectory));
        normalized.GuiOptions ??= [];
        foreach (var option in GtpEngineGuiOptions.Specs) normalized.GuiOptions.TryAdd(option.Id, option.DefaultValue);
        return normalized;
    }

    private static string ResolvePath(string path, string baseDirectory) =>
        Path.IsPathFullyQualified(path) || !HasDirectoryPart(path) ? path : Path.GetFullPath(Path.Combine(baseDirectory, path));

    public static bool HasDirectoryPart(string path) =>
        path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar);
}
