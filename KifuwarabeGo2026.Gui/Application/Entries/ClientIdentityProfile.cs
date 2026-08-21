namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>
/// Player が用途・接続先ごとに使う識別情報。認証情報は Player ごとに独立し、
/// EngineProfile や接続先プロファイルには保存しない。
/// </summary>
public sealed class ClientIdentityProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "New Client Identity";
    public string ConnectionProfileId { get; set; } = "";
    public string LoginName { get; set; } = "";
    public string LoginPass { get; set; } = "";
    public string Comment { get; set; } = "";

    public ClientIdentityProfile Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        ConnectionProfileId = ConnectionProfileId,
        LoginName = LoginName,
        LoginPass = LoginPass,
        Comment = Comment,
    };
}
