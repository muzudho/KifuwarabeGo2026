namespace KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;

using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

/// <summary>エンジンの初期局面設定を確認し、操作を案内するページです。</summary>
public sealed class InitialPositionConcierge
{
    #region Layout

    private static readonly Rectangle BlackCardBounds = new(1144, 190, 668, 310);
    private static readonly Rectangle WhiteCardBounds = new(1144, 516, 668, 310);

    #endregion

    #region Buttons

    /// <summary>案内を中止します。</summary>
    public static Rectangle CancelButtonBounds { get; } = new(1144, 916, 320, 66);

    /// <summary>GTP のログを表示します。</summary>
    public static Rectangle LogButtonBounds { get; } = new(1492, 916, 320, 66);

    #endregion

    #region Hit testing

    public static GoStone? GetEngineCardHit(Point point) => BlackCardBounds.Contains(point) ? GoStone.Black : WhiteCardBounds.Contains(point) ? GoStone.White : null;
    public static GoStone? GetTryAnotherButtonHit(Point point) => GetActionButtonHit(point, true);
    public static GoStone? GetContinueButtonHit(Point point) => GetActionButtonHit(point, false);
    public static bool IsCancelButtonHit(Point point) => CancelButtonBounds.Contains(point);
    public static bool IsLogButtonHit(Point point) => LogButtonBounds.Contains(point);

    #endregion

    #region Drawing

    /// <summary>初期局面の確認ページを描画します。</summary>
    public void Draw(InitialPositionConciergeView view, Point mousePoint, InitialPositionConciergeDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawDynamicText("INITIAL POSITION CONCIERGE", new Rectangle(1144, 98, 668, 44), new Color(255, 230, 160), 0.62f);
        draw.DrawDynamicText("Checking whether each engine can use the configured initial position.", new Rectangle(1144, 143, 668, 32), new Color(180, 195, 195), 0.31f);
        foreach (var engine in view.Engines) DrawEngineCard(engine, view.SelectedStone == engine.Stone, mousePoint, draw);
        draw.DrawDynamicText("Arrow keys: select   SPACE: try another   ENTER: continue   L: log   ESC: cancel", new Rectangle(1144, 850, 668, 32), new Color(137, 158, 164), 0.26f);
        draw.DrawButton(CancelButtonBounds, "CANCEL", false, mousePoint, true, 0.42f);
        draw.DrawButton(LogButtonBounds, "GTP LOG", false, mousePoint, true, 0.42f);
    }

    private static void DrawEngineCard(InitialPositionEngineProgressView engine, bool selected, Point mousePoint, InitialPositionConciergeDrawingCallbacks draw)
    {
        var bounds = engine.Stone == GoStone.Black ? BlackCardBounds : WhiteCardBounds;
        draw.FillRectangle(bounds, selected ? new Color(31, 48, 55) : new Color(25, 32, 39));
        draw.DrawRectangle(bounds, selected ? 3 : 2, selected ? new Color(99, 223, 185) : new Color(82, 111, 114));
        draw.DrawText(engine.Stone == GoStone.Black ? "BLACK" : "WHITE", new Vector2(bounds.X + 18, bounds.Y + 15), new Color(99, 223, 185), 0.38f);
        draw.DrawDynamicText(engine.EngineName, new Rectangle(bounds.X + 116, bounds.Y + 10, 410, 40), Color.White, 0.4f);
        var stateLabel = engine.IsAccepted ? "READY" : engine.IsBusy ? "CHECKING..." : "ACTION NEEDED";
        draw.DrawFittedText(stateLabel, new Rectangle(bounds.Right - 130, bounds.Y + 12, 112, 32), engine.IsAccepted ? new Color(177, 255, 215) : new Color(255, 210, 135), 0.27f);

        var attempts = engine.Attempts.TakeLast(3).ToArray();
        if (attempts.Length == 0)
        {
            var message = engine.Diagnostics.LastOrDefault() ?? "Waiting for the engine to check the command...";
            draw.DrawDynamicText(message, new Rectangle(bounds.X + 20, bounds.Y + 70, bounds.Width - 40, 64), engine.Diagnostics.Count == 0 ? new Color(180, 195, 195) : new Color(255, 150, 140), 0.32f);
        }
        else
        {
            for (var index = 0; index < attempts.Length; index++)
            {
                var attempt = attempts[index];
                var y = bounds.Y + 62 + index * 45;
                draw.DrawText(FormatAttemptMark(attempt.Status), new Vector2(bounds.X + 20, y), GetAttemptColor(attempt.Status), 0.34f);
                draw.DrawDynamicText(attempt.MethodDisplayName, new Rectangle(bounds.X + 58, y - 3, 246, 34), Color.White, 0.31f);
                draw.DrawDynamicText(FormatAttemptStatus(attempt.Status), new Rectangle(bounds.X + 320, y - 3, 310, 34), GetAttemptColor(attempt.Status), 0.29f);
            }
            var detail = attempts[^1].Detail ?? engine.Diagnostics.LastOrDefault() ?? string.Empty;
            draw.DrawDynamicText(detail, new Rectangle(bounds.X + 20, bounds.Bottom - 102, bounds.Width - 40, 30), new Color(158, 178, 178), 0.25f);
        }

        draw.DrawButton(GetActionBounds(engine.Stone, true), "TRY ANOTHER METHOD", false, mousePoint, engine.CanTryAnotherMethod, 0.32f);
        draw.DrawButton(GetActionBounds(engine.Stone, false), engine.IsAccepted ? "CONTINUE" : "CONTINUE AS IS", engine.IsAccepted, mousePoint, engine.CanContinueAsIs, 0.32f);
    }

    #endregion

    #region Helpers

    private static GoStone? GetActionButtonHit(Point point, bool tryAnother)
    {
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
            if (GetActionBounds(stone, tryAnother).Contains(point)) return stone;
        return null;
    }

    private static Rectangle GetActionBounds(GoStone stone, bool tryAnother)
    {
        var card = stone == GoStone.Black ? BlackCardBounds : WhiteCardBounds;
        return tryAnother ? new Rectangle(card.X + 18, card.Bottom - 58, 292, 42) : new Rectangle(card.X + 326, card.Bottom - 58, 324, 42);
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
        InitialPositionAttemptStatus.VerifiedSuccess => "POSITION VERIFIED",
        InitialPositionAttemptStatus.UnverifiedSuccess => "POSITION NOT VERIFIED",
        InitialPositionAttemptStatus.NotApplicable => "NOT APPLICABLE",
        InitialPositionAttemptStatus.Unsupported => "COMMAND UNSUPPORTED",
        InitialPositionAttemptStatus.CommandRejected => "COMMAND REJECTED",
        InitialPositionAttemptStatus.PositionMismatch => "POSITION MISMATCH",
        InitialPositionAttemptStatus.InvalidResponse => "INVALID RESPONSE",
        InitialPositionAttemptStatus.TransportFailure => "TRANSPORT FAILURE",
        _ => status.ToString(),
    };

    private static Color GetAttemptColor(InitialPositionAttemptStatus status) => status switch
    {
        InitialPositionAttemptStatus.VerifiedSuccess => new Color(99, 223, 185),
        InitialPositionAttemptStatus.UnverifiedSuccess => new Color(255, 210, 135),
        InitialPositionAttemptStatus.NotApplicable or InitialPositionAttemptStatus.Unsupported => new Color(126, 150, 164),
        _ => new Color(255, 150, 140),
    };

    #endregion
}

/// <summary>InitialPositionConcierge に渡す描画機能です。</summary>
public sealed record InitialPositionConciergeDrawingCallbacks(
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<string, Vector2, Color, float> DrawText,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<Rectangle, string, bool, Point, bool, float> DrawButton);
