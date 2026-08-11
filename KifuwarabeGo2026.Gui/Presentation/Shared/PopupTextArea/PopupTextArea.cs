namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

/// <summary>複数行コメントを編集するモーダル入力欄です。</summary>
public sealed partial class GoScreenRenderer
{
    private static Rectangle TextAreaDialogBounds => new(320, 150, 1280, 780);
    private static Rectangle TextAreaTextBounds => new(390, 330, 1140, 400);
    // ポップアップ共通の慣例に合わせ、閉じる・反映する操作は見出し右上へまとめる。
    private static Rectangle TextAreaCloseButtonBounds => new(1230, 172, 150, 54);
    private static Rectangle TextAreaApplyButtonBounds => new(1410, 172, 150, 54);

    public static bool GetTextAreaDialogCancelButtonHit(Point point) => TextAreaCloseButtonBounds.Contains(point);
    public static bool GetTextAreaDialogApplyButtonHit(Point point) => TextAreaApplyButtonBounds.Contains(point);

    public void DrawTextAreaDialog(
        Point mousePosition,
        string title,
        string text,
        int caretIndex,
        string message,
        TextCompositionState composition = default,
        TextCompositionDiagnostics compositionDiagnostics = default,
        bool showCompositionDiagnostics = false)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 145));
        FillRect(new Rectangle(TextAreaDialogBounds.X + 14, TextAreaDialogBounds.Y + 16, TextAreaDialogBounds.Width, TextAreaDialogBounds.Height), new Color(0, 0, 0, 155));
        FillRect(TextAreaDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(TextAreaDialogBounds, 2, new Color(116, 145, 146));
        DrawText("COMMENT EDITOR", new Vector2(TextAreaDialogBounds.X + 34, TextAreaDialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText(title, new Rectangle(TextAreaDialogBounds.X + 36, TextAreaDialogBounds.Y + 96, TextAreaDialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);
        if (showCompositionDiagnostics)
        {
            // Windows 版だけの診断ランプ。右上の CLOSE ボタンの左に集め、本文の場所を取らない。
            DrawCompositionLamp(TextAreaDialogBounds, "SDL", 1100, compositionDiagnostics.IsSdlWindowResolved, new Color(99, 223, 185));
            DrawCompositionLamp(TextAreaDialogBounds, "HOOK", 1146, compositionDiagnostics.IsWindowProcedureAttached, new Color(99, 223, 185));
            DrawCompositionLamp(TextAreaDialogBounds, "IME", 1192, composition.IsActive, new Color(255, 225, 128));
        }
        FillRect(TextAreaTextBounds, new Color(15, 20, 26));
        DrawRect(TextAreaTextBounds, 2, new Color(99, 223, 185));
        DrawTextAreaContent(text, TextAreaTextBounds);
        var caret = GetTextAreaCaretPosition(text, caretIndex);
        if (composition.IsActive && !string.IsNullOrEmpty(composition.Text))
        {
            var compositionWidth = DrawDynamicCompositionText(composition.Text, caret, new Color(255, 225, 128), 0.52f);
            DrawLine(caret + new Vector2(0, 29), caret + new Vector2(compositionWidth, 29), 2, new Color(255, 225, 128));
        }
        FillRect(new Rectangle((int)caret.X, (int)caret.Y, 2, 29), composition.IsActive ? new Color(255, 225, 128) : new Color(147, 244, 200));
        DrawDynamicOptionText(message, new Rectangle(TextAreaDialogBounds.X + 70, 752, 820, 34), new Color(180, 195, 195), 0.34f);
        DrawFittedText("ENTER: NEW LINE   CTRL+ENTER: APPLY   ESC: CLOSE WITHOUT APPLY", new Rectangle(TextAreaDialogBounds.X + 70, 786, 800, 28), new Color(147, 201, 190), 0.27f);
        DrawCommandButton(TextAreaCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.38f);
        DrawCommandButton(TextAreaApplyButtonBounds, "APPLY", false, mousePoint, scale: 0.40f);
        _spriteBatch.End();
    }

    private void DrawTextAreaContent(string text, Rectangle bounds)
    {
        if (string.IsNullOrEmpty(text))
        {
            DrawFittedText("(EMPTY COMMENT)", new Rectangle(bounds.X + 18, bounds.Y + 18, bounds.Width - 36, 34), new Color(112, 132, 136), 0.34f);
            return;
        }
        var key = $"popup-text-area:{text.GetHashCode(StringComparison.Ordinal)}:{text.Length}:{bounds.Width}:{bounds.Height}";
        if (!_dynamicOptionTextTextures.TryGetValue(key, out var texture))
        {
            var png = _textRasterizer.RasterizeWrappedPagePng(text, bounds.Width - 36, bounds.Height - 36, 26, 5, 0);
            using var stream = new MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[key] = texture;
        }
        _spriteBatch.Draw(texture, new Rectangle(bounds.X + 18, bounds.Y + 18, bounds.Width - 36, bounds.Height - 36), new Color(226, 232, 225));
    }

    private Vector2 GetTextAreaCaretPosition(string text, int caretIndex)
    {
        var safeIndex = Math.Clamp(caretIndex, 0, text.Length);
        var beforeCaret = text[..safeIndex];
        var lastLineStart = beforeCaret.LastIndexOf('\n') + 1;
        var lineText = beforeCaret[lastLineStart..];
        var lineNumber = 0;
        foreach (var character in beforeCaret)
        {
            if (character == '\n') lineNumber++;
        }

        // 本文を描く WindowsTextRasterizer と同じ Meiryo の字幅を使う。SpriteFont で別に
        // 測ると「～」など全角文字の幅が異なり、キャレットだけが横にずれてしまう。
        var x = TextAreaTextBounds.X + 18 +
            (int)MathF.Round(_textRasterizer.MeasureTextWidth(lineText, pixelHeight: 26, bold: false));
        var y = TextAreaTextBounds.Y + 18 + lineNumber * 31;
        return new Vector2(
            Math.Clamp(x, TextAreaTextBounds.X + 18, TextAreaTextBounds.Right - 22),
            Math.Clamp(y, TextAreaTextBounds.Y + 18, TextAreaTextBounds.Bottom - 48));
    }
}
