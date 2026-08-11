namespace KifuwarabeGo2026.Gui.Application;

/// <summary>
/// OS のフォント描画機能を使い、文字列を透明背景の PNG へ変換します。
/// </summary>
public interface ITextRasterizer
{
    byte[] RasterizePng(string text, int pixelHeight, bool bold);

    /// <summary>ラスタライズ本文と同じフォント設定で、1 行テキストの幅を測ります。</summary>
    float MeasureTextWidth(string text, int pixelHeight, bool bold);

    int GetWrappedPageCount(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing);

    byte[] RasterizeWrappedPagePng(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing,
        int requestedPage);
}
