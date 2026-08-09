namespace KifuwarabeGo2026.Gui.Presentation;

using System;
using System.Linq;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    private static readonly Rectangle InitialPositionBlackCardBounds = new(1144, 190, 668, 310);
    private static readonly Rectangle InitialPositionWhiteCardBounds = new(1144, 516, 668, 310);
    private static readonly Rectangle InitialPositionCancelButtonBounds = new(1144, 916, 320, 66);
    private static readonly Rectangle InitialPositionLogButtonBounds = new(1492, 916, 320, 66);

    public static GoStone? GetInitialPositionEngineCardHit(Point point) =>
        InitialPositionBlackCardBounds.Contains(point) ? GoStone.Black :
        InitialPositionWhiteCardBounds.Contains(point) ? GoStone.White : null;

    public static GoStone? GetInitialPositionTryAnotherButtonHit(Point point) =>
        GetInitialPositionButtonHit(point, tryAnother: true);

    public static GoStone? GetInitialPositionContinueButtonHit(Point point) =>
        GetInitialPositionButtonHit(point, tryAnother: false);

    public static bool GetInitialPositionCancelButtonHit(Point point) =>
        InitialPositionCancelButtonBounds.Contains(point);

    public static bool GetInitialPositionLogButtonHit(Point point) =>
        InitialPositionLogButtonBounds.Contains(point);

    private static GoStone? GetInitialPositionButtonHit(Point point, bool tryAnother)
    {
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
        {
            if (GetInitialPositionActionBounds(stone, tryAnother).Contains(point))
            {
                return stone;
            }
        }

        return null;
    }

    private static Rectangle GetInitialPositionActionBounds(GoStone stone, bool tryAnother)
    {
        var card = stone == GoStone.Black ? InitialPositionBlackCardBounds : InitialPositionWhiteCardBounds;
        return tryAnother
            ? new Rectangle(card.X + 18, card.Bottom - 58, 292, 42)
            : new Rectangle(card.X + 326, card.Bottom - 58, 324, 42);
    }

    private void DrawInitialPositionConcierge(InitialPositionConciergeView view, Point mousePoint)
    {
        DrawDynamicOptionText("指定局面コンシェルジュ", new Rectangle(1144, 98, 668, 44), new Color(255, 230, 160), 0.62f);
        DrawDynamicOptionText("エンジンごとに使える設定方法を試します", new Rectangle(1144, 143, 668, 32), new Color(180, 195, 195), 0.31f);

        foreach (var engine in view.Engines)
        {
            DrawInitialPositionEngineCard(engine, view.SelectedStone == engine.Stone, mousePoint);
        }

        DrawDynamicOptionText("↑↓ 選択  SPACE 別の方法  ENTER 続ける  L ログ  ESC 中止",
            new Rectangle(1144, 850, 668, 32), new Color(137, 158, 164), 0.26f);
        DrawCommandButton(InitialPositionCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.42f);
        DrawCommandButton(InitialPositionLogButtonBounds, "GTP LOG", false, mousePoint, scale: 0.42f);
    }

    private void DrawInitialPositionEngineCard(
        InitialPositionEngineProgressView engine,
        bool selected,
        Point mousePoint)
    {
        var bounds = engine.Stone == GoStone.Black ? InitialPositionBlackCardBounds : InitialPositionWhiteCardBounds;
        FillRect(bounds, selected ? new Color(31, 48, 55) : new Color(25, 32, 39));
        DrawRect(bounds, selected ? 3 : 2, selected ? new Color(99, 223, 185) : new Color(82, 111, 114));

        var colorName = engine.Stone == GoStone.Black ? "BLACK" : "WHITE";
        DrawText(colorName, new Vector2(bounds.X + 18, bounds.Y + 15), new Color(99, 223, 185), 0.38f);
        DrawDynamicOptionText(engine.EngineName, new Rectangle(bounds.X + 116, bounds.Y + 10, 410, 40), Color.White, 0.4f);
        var stateLabel = engine.IsAccepted ? "READY" : engine.IsBusy ? "CHECKING..." : "ACTION NEEDED";
        DrawFittedText(stateLabel, new Rectangle(bounds.Right - 130, bounds.Y + 12, 112, 32),
            engine.IsAccepted ? new Color(177, 255, 215) : new Color(255, 210, 135), 0.27f);

        var attempts = engine.Attempts.TakeLast(3).ToArray();
        if (attempts.Length == 0)
        {
            var message = engine.Diagnostics.LastOrDefault() ?? "能力と対応コマンドを調査中...";
            DrawDynamicOptionText(message, new Rectangle(bounds.X + 20, bounds.Y + 70, bounds.Width - 40, 64),
                engine.Diagnostics.Count == 0 ? new Color(180, 195, 195) : new Color(255, 150, 140), 0.32f);
        }
        else
        {
            for (var i = 0; i < attempts.Length; i++)
            {
                var attempt = attempts[i];
                var y = bounds.Y + 62 + i * 45;
                DrawText(FormatAttemptMark(attempt.Status), new Vector2(bounds.X + 20, y),
                    GetAttemptColor(attempt.Status), 0.34f);
                DrawDynamicOptionText(attempt.MethodDisplayName, new Rectangle(bounds.X + 58, y - 3, 246, 34),
                    Color.White, 0.31f);
                DrawDynamicOptionText(FormatAttemptStatus(attempt.Status), new Rectangle(bounds.X + 320, y - 3, 310, 34),
                    GetAttemptColor(attempt.Status), 0.29f);
            }

            var detail = attempts[^1].Detail ?? engine.Diagnostics.LastOrDefault() ?? string.Empty;
            DrawDynamicOptionText(detail, new Rectangle(bounds.X + 20, bounds.Bottom - 102, bounds.Width - 40, 30),
                new Color(158, 178, 178), 0.25f);
        }

        DrawCommandButton(GetInitialPositionActionBounds(engine.Stone, true), "別の方法を試す", false,
            mousePoint, enabled: engine.CanTryAnotherMethod, scale: 0.32f);
        DrawCommandButton(GetInitialPositionActionBounds(engine.Stone, false),
            engine.IsAccepted ? "確認済み" : "このまま続ける", engine.IsAccepted,
            mousePoint, enabled: engine.CanContinueAsIs, scale: 0.32f);
    }

    private static string FormatAttemptMark(InitialPositionAttemptStatus status) => status switch
    {
        InitialPositionAttemptStatus.VerifiedSuccess => "OK",
        InitialPositionAttemptStatus.UnverifiedSuccess => "?",
        InitialPositionAttemptStatus.NotApplicable or InitialPositionAttemptStatus.Unsupported => "-",
        _ => "NG",
    };

    private static string FormatAttemptStatus(InitialPositionAttemptStatus status) => status switch
    {
        InitialPositionAttemptStatus.VerifiedSuccess => "局面を確認できました",
        InitialPositionAttemptStatus.UnverifiedSuccess => "応答成功・局面は未確認",
        InitialPositionAttemptStatus.NotApplicable => "この局面には不適用",
        InitialPositionAttemptStatus.Unsupported => "コマンド非対応",
        InitialPositionAttemptStatus.CommandRejected => "エンジンが拒否",
        InitialPositionAttemptStatus.PositionMismatch => "設定後の局面が不一致",
        InitialPositionAttemptStatus.InvalidResponse => "想定外の応答",
        InitialPositionAttemptStatus.TransportFailure => "通信に失敗",
        _ => status.ToString(),
    };

    private static Color GetAttemptColor(InitialPositionAttemptStatus status) => status switch
    {
        InitialPositionAttemptStatus.VerifiedSuccess => new Color(99, 223, 185),
        InitialPositionAttemptStatus.UnverifiedSuccess => new Color(255, 210, 135),
        InitialPositionAttemptStatus.NotApplicable or InitialPositionAttemptStatus.Unsupported => new Color(126, 150, 164),
        _ => new Color(255, 150, 140),
    };
}
