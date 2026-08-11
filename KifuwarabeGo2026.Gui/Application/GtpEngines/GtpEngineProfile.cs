namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// GTPプロトコル対応の思考エンジンのプロファイル
/// </summary>
public sealed class GtpEngineProfile
{
    /// <summary>
    /// 永続的なエンジン設定の識別子。表示順や表示名が変わっても PlayerProfile から参照できる。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = "Kifuwarabe Star Random GTP";

    /// <summary>CGOS 接続画面へ初期表示するログイン名です。</summary>
    public string DefaultCgosLoginName { get; set; } = "";

    /// <summary>CGOS 接続画面へ初期表示する平文パスワードです。</summary>
    public string DefaultCgosPlainTextPassword { get; set; } = "";

    public string ExecutablePath { get; set; } = "";

    /// <summary>
    /// 作業ディレクトリー
    /// </summary>
    [JsonIgnore]
    public WorkingDirectoryModel WorkingDirectoryModel { get; set; } = WorkingDirectoryModel.Empty;
    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectoryStr
    {
        get => WorkingDirectoryModel.Value;
        set => WorkingDirectoryModel = WorkingDirectoryModel.FromString(value);
    }

    public string Arguments { get; set; } = "";

    public bool EnableGtpLog { get; set; } = true;

    /// <summary>"auto" or a built-in GTP compatibility profile id.</summary>
    public string InitialPositionProfileId { get; set; } = "auto";

    /// <summary>A user-selected priority. This is never invalidated by an engine version change.</summary>
    public InitialPositionMethod? InitialPositionManualPreferredMethod { get; set; }

    /// <summary>The last method accepted automatically or explicitly by the user.</summary>
    public InitialPositionMethod? InitialPositionDetectedMethod { get; set; }

    public string InitialPositionDetectedEngineName { get; set; } = "";

    public string InitialPositionDetectedEngineVersion { get; set; } = "";

    public string InitialPositionDetectedProfileId { get; set; } = "";

    public Dictionary<string, string> GuiOptions { get; set; } = new()
    {
        [GtpEngineGuiOptions.RandomMoveId] = GtpEngineGuiOptions.ChebyshevDistanceFromStarRandomMove,
    };

    [JsonIgnore]
    public string LogPrefix { get; set; } = "";

    public GtpEngineProfile Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        DefaultCgosLoginName = DefaultCgosLoginName,
        DefaultCgosPlainTextPassword = DefaultCgosPlainTextPassword,
        ExecutablePath = ExecutablePath,
        WorkingDirectoryModel = WorkingDirectoryModel,
        Arguments = Arguments,
        EnableGtpLog = EnableGtpLog,
        InitialPositionProfileId = InitialPositionProfileId,
        InitialPositionManualPreferredMethod = InitialPositionManualPreferredMethod,
        InitialPositionDetectedMethod = InitialPositionDetectedMethod,
        InitialPositionDetectedEngineName = InitialPositionDetectedEngineName,
        InitialPositionDetectedEngineVersion = InitialPositionDetectedEngineVersion,
        InitialPositionDetectedProfileId = InitialPositionDetectedProfileId,
        GuiOptions = new Dictionary<string, string>(GuiOptions ?? []),
        LogPrefix = LogPrefix,
    };

    public string GetGuiOption(string id, string fallback) =>
        GuiOptions.TryGetValue(id, out var value) ? value : fallback;

    public bool HasMatchingInitialPositionDetection(string? engineName, string? engineVersion) =>
        InitialPositionDetectedMethod is not null &&
        string.Equals(InitialPositionDetectedEngineName, engineName ?? "", System.StringComparison.OrdinalIgnoreCase) &&
        string.Equals(InitialPositionDetectedEngineVersion, engineVersion ?? "", System.StringComparison.OrdinalIgnoreCase);

    public bool ClearStaleInitialPositionDetection(string? engineName, string? engineVersion)
    {
        if (InitialPositionDetectedMethod is null || HasMatchingInitialPositionDetection(engineName, engineVersion))
            return false;
        InitialPositionDetectedMethod = null;
        InitialPositionDetectedEngineName = "";
        InitialPositionDetectedEngineVersion = "";
        InitialPositionDetectedProfileId = "";
        return true;
    }

    public void RememberInitialPositionDetection(
        InitialPositionMethod method,
        string? engineName,
        string? engineVersion,
        string profileId)
    {
        InitialPositionDetectedMethod = method;
        InitialPositionDetectedEngineName = engineName ?? "";
        InitialPositionDetectedEngineVersion = engineVersion ?? "";
        InitialPositionDetectedProfileId = profileId;
    }
}

public enum GtpEngineProfileEditField
{
    DisplayName,
    DefaultCgosLoginName,
    DefaultCgosPlainTextPassword,
    ExecutablePath,
    WorkingDirectory,
    Arguments,
}
