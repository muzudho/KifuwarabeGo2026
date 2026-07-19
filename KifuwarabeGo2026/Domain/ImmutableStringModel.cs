namespace KifuwarabeGo2026.Domain;

/// <summary>
/// 不変文字列モデル
/// </summary>
internal abstract class ImmutableStringModel
{
    // ========================================
    // 生成
    // ========================================


    /// <summary>
    /// サブクラスでファクトリーメソッドを作って呼出してください。
    /// </summary>
    /// <param name="value"></param>
    protected internal ImmutableStringModel(string value)
    {
        Value = value;
    }


    // ========================================
    // 窓口データメンバー
    // ========================================


    internal bool IsEmpty => string.IsNullOrEmpty(Value);

    internal string Value { get; init; }


    // ========================================
    // 窓口メソッド
    // ========================================


}
