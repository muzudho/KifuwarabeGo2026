namespace KifuwarabeGo2026.Reference.Communication.Gtp;

/// <summary>GTPコマンドの配送方法をプロセス管理から分離する境界です。</summary>
public interface IGtpCommandTransport
{
    /// <summary>一つのGTPコマンドを送り、成功可否とペイロードを受け取ります。</summary>
    ValueTask<GtpCommandResponse> SendAsync(
        string command,
        CancellationToken cancellationToken = default);
}

/// <summary>GTP応答の意味的な最小表現です。</summary>
public sealed record GtpCommandResponse(bool IsSuccess, string Payload)
{
    public void ThrowIfError(string command)
    {
        if (!IsSuccess) throw new InvalidOperationException($"GTP command failed: {command}: {Payload}");
    }
}
