namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>通信方式に依存しない、成功値またはエラーを返す応答です。</summary>
/// <typeparam name="T">成功時の値。</typeparam>
public sealed record ProtocolResponse<T>
{
    private ProtocolResponse(T? value, ProtocolError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>成功時の値です。</summary>
    public T? Value { get; }

    /// <summary>失敗時のエラーです。</summary>
    public ProtocolError? Error { get; }

    /// <summary>成功応答か。</summary>
    public bool IsSuccess => Error is null;

    /// <summary>成功応答を作ります。</summary>
    public static ProtocolResponse<T> Success(T value) => new(value, null);

    /// <summary>失敗応答を作ります。</summary>
    public static ProtocolResponse<T> Failure(ProtocolError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, error);
    }
}
