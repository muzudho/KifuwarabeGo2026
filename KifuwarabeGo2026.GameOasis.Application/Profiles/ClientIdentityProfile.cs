namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

using System;

/// <summary>エントリーが用途・接続先ごとに使う常用の識別・認証情報です。</summary>
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
