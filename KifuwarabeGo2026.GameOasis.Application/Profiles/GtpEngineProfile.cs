namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using System.Text.Json.Serialization;

public sealed class GtpEngineProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "Kifuwarabe Star Random GTP";
    public string DefaultCgosLoginName { get; set; } = "";
    public string DefaultCgosPlainTextPassword { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    [JsonIgnore] public WorkingDirectoryModel WorkingDirectoryModel { get; set; } = WorkingDirectoryModel.Empty;
    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectoryStr { get => WorkingDirectoryModel.Value; set => WorkingDirectoryModel = WorkingDirectoryModel.FromString(value); }
    public string Arguments { get; set; } = "";
    public bool EnableGtpLog { get; set; } = true;
    public string InitialPositionProfileId { get; set; } = "auto";
    public InitialPositionMethod? InitialPositionManualPreferredMethod { get; set; }
    public InitialPositionMethod? InitialPositionDetectedMethod { get; set; }
    public string InitialPositionDetectedEngineName { get; set; } = "";
    public string InitialPositionDetectedEngineVersion { get; set; } = "";
    public string InitialPositionDetectedProfileId { get; set; } = "";
    public Dictionary<string, string> GuiOptions { get; set; } = new() { [GtpEngineGuiOptions.RandomMoveId] = GtpEngineGuiOptions.ChebyshevDistanceFromStarRandomMove };
    [JsonIgnore] public string LogPrefix { get; set; } = "";
    public GtpEngineProfile Clone() => new() { Id=Id, DisplayName=DisplayName, DefaultCgosLoginName=DefaultCgosLoginName,
        DefaultCgosPlainTextPassword=DefaultCgosPlainTextPassword, ExecutablePath=ExecutablePath, WorkingDirectoryModel=WorkingDirectoryModel,
        Arguments=Arguments, EnableGtpLog=EnableGtpLog, InitialPositionProfileId=InitialPositionProfileId,
        InitialPositionManualPreferredMethod=InitialPositionManualPreferredMethod, InitialPositionDetectedMethod=InitialPositionDetectedMethod,
        InitialPositionDetectedEngineName=InitialPositionDetectedEngineName, InitialPositionDetectedEngineVersion=InitialPositionDetectedEngineVersion,
        InitialPositionDetectedProfileId=InitialPositionDetectedProfileId, GuiOptions=new(GuiOptions ?? []), LogPrefix=LogPrefix };
    public string GetGuiOption(string id, string fallback) => GuiOptions.TryGetValue(id, out var value) ? value : fallback;
    public bool HasMatchingInitialPositionDetection(string? name, string? version) => InitialPositionDetectedMethod is not null &&
        string.Equals(InitialPositionDetectedEngineName, name ?? "", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(InitialPositionDetectedEngineVersion, version ?? "", StringComparison.OrdinalIgnoreCase);
    public bool ClearStaleInitialPositionDetection(string? name, string? version)
    {
        if (InitialPositionDetectedMethod is null || HasMatchingInitialPositionDetection(name, version)) return false;
        InitialPositionDetectedMethod = null; InitialPositionDetectedEngineName = InitialPositionDetectedEngineVersion = InitialPositionDetectedProfileId = ""; return true;
    }
    public void RememberInitialPositionDetection(InitialPositionMethod method, string? name, string? version, string profileId)
    { InitialPositionDetectedMethod=method; InitialPositionDetectedEngineName=name ?? ""; InitialPositionDetectedEngineVersion=version ?? ""; InitialPositionDetectedProfileId=profileId; }
}
