namespace KifuwarabeGo2026.Gui.Domain;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class WorkingDirectoryModel
    : ImmutableStringModel
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　固定インスタンス］

    internal static WorkingDirectoryModel Empty { get; } = new (string.Empty);

    #endregion

    #region ［生成　＞　ファクトリーメソッド］

    internal static WorkingDirectoryModel FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Empty;
        return new WorkingDirectoryModel(value);
    }

    #endregion

    #region ［生成　＞　コンストラクター］

    /// <summary>
    /// デフォルトコンストラクター
    /// </summary>
    /// <param name="value"></param>
    internal WorkingDirectoryModel(string value)
        : base(value)
    {
    }

    #endregion


    // ========================================
    // ドメインデータメンバー
    // ========================================


    /// <summary>
    ///     <pre>
    ///         表示時文字列
    ///         
    ///             - 未指定ならハイフン表示
    ///     </pre>
    /// </summary>
    internal string DisplayValue => IsEmpty ? "-" : Value;
}
