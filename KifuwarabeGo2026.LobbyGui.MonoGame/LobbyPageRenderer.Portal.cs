namespace KifuwarabeGo2026.LobbyGui.MonoGame;

using KifuwarabeGo2026.LobbyGui.Application;
using Microsoft.Xna.Framework;

public sealed partial class LobbyPageRenderer
{
    private void DrawPortal(LobbyHomePresentation home, Point mouse)
    {
        DrawMenu(LobbyPortalLayout.Engine, "エンジン登録", mouse, 0);
        DrawMenu(LobbyPortalLayout.Entry, "エントリー登録", mouse, 1);
        DrawMenu(LobbyPortalLayout.Settings, "設定", mouse, 2);
        DrawMenu(LobbyPortalLayout.UpdateInstaller, "インストーラーを更新", mouse, 3);
        DrawMenu(LobbyPortalLayout.OpenInstaller, "インストーラーを起動", mouse, 4);
        DrawText("PLAY ROOM", new Vector2(132, 260), new Color(99, 223, 185), 0.48f);
        DrawDynamicOptionText(home.Guidance, new Rectangle(400, 252, 1388, 42), new Color(180, 195, 195), 0.36f);
        var entries = home.VisibleItems;
        for (var slot = 0; slot < entries.Count; slot++)
        {
            var item = entries[slot];
            var bounds = LobbyPortalLayout.Card(slot);
            var hover = bounds.Contains(mouse);
            var accent = ToColor(item.Accent);
            FillRect(new Rectangle(bounds.X + 5, bounds.Y + 7, bounds.Width, bounds.Height), new Color(0, 0, 0, 80));
            FillRect(bounds, hover ? new Color(36, 50, 58) : new Color(25, 33, 41));
            DrawRect(bounds, 2, hover ? accent : new Color(72, 89, 99));
            FillRect(new Rectangle(bounds.X, bounds.Y, 6, bounds.Height), accent);
            DrawRoomIcon(new Rectangle(bounds.X + 30, bounds.Y + 30, 58, 58), item.Target, accent);
            DrawDynamicOptionText(item.Title, new Rectangle(bounds.X + 110, bounds.Y + 28, bounds.Width - 142, 58), Color.White, 0.59f);
            DrawDynamicOptionText(item.Caption, new Rectangle(bounds.X + 32, bounds.Y + 118, bounds.Width - 64, 46), new Color(199, 213, 217), 0.43f);
            DrawText(item.Target == LobbyHomeTarget.ReferenceGo ? "SAMPLE" : item.Target == LobbyHomeTarget.CaptureGame ? "CAPTURE GAME" : "GTP", new Vector2(bounds.X + 32, bounds.Bottom - 61), accent, 0.32f);
            var open = new Rectangle(bounds.Right - 176, bounds.Bottom - 80, 144, 52);
            FillRect(open, hover ? accent : new Color(44, 60, 69));
            DrawDynamicOptionText("開く  >", new Rectangle(open.X + 22, open.Y + 7, open.Width - 44, 36), hover ? new Color(21, 25, 32) : Color.White, 0.42f);
        }
        if (entries.Count == 0)
            DrawDynamicOptionText("利用できるプレイルームはありません。", new Rectangle(420, 450, 1080, 80), Color.White, 0.5f);
        DrawLine(new Vector2(132, 894), new Vector2(1788, 894), 1, new Color(72, 89, 99));
        DrawPager(LobbyPortalLayout.Previous, "<  前へ", home.CanPrevious, mouse);
        DrawPager(LobbyPortalLayout.Next, "次へ  >", home.CanNext, mouse);
        DrawDynamicOptionText($"{home.PageIndex + 1} / {home.PageCount}    ·    {home.Catalog.Count} プレイルーム", new Rectangle(680, 920, 560, 46), new Color(199, 213, 217), 0.42f);
    }

    private void DrawPager(Rectangle bounds, string text, bool enabled, Point mouse)
    {
        var hover = enabled && bounds.Contains(mouse);
        FillRect(bounds, hover ? new Color(44, 64, 71) : new Color(25, 33, 41));
        DrawRect(bounds, 1, enabled ? new Color(99, 223, 185) : new Color(58, 70, 79));
        DrawDynamicOptionText(text, new Rectangle(bounds.X + 30, bounds.Y + 9, bounds.Width - 60, 38),
            enabled ? Color.White : new Color(106, 121, 132), 0.4f);
    }

    private void DrawMenu(Rectangle bounds, string text, Point mouse, int icon)
    {
        var hover = bounds.Contains(mouse);
        var color = hover ? new Color(147, 244, 200) : new Color(199, 213, 217);
        FillRect(bounds, hover ? new Color(40, 58, 65) : new Color(28, 37, 45));
        DrawRect(bounds, 1, hover ? color : new Color(65, 83, 94));
        var center = new Vector2(bounds.X + 32, bounds.Center.Y);
        if (icon == 0) _drawingContext.DrawEngineIcon(center);
        else if (icon == 1) _drawingContext.DrawEntryIcon(center);
        else if (icon == 2)
        {
            for (var i = 0; i < 12; i++)
            {
                var angle = i * MathF.PI / 6;
                var next = (i + 1) * MathF.PI / 6;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                DrawLine(center + direction * 13, center + new Vector2(MathF.Cos(next), MathF.Sin(next)) * 13, 3, color);
                if (i % 2 == 0) DrawLine(center + direction * 13, center + direction * 20, 5, color);
            }
            DrawRect(new Rectangle((int)center.X - 4, (int)center.Y - 4, 8, 8), 2, color);
        }
        else
        {
            var box = new Rectangle(bounds.X + 16, bounds.Y + 15, 31, 35);
            DrawRect(box, 2, color);
            if (icon == 3)
            {
                DrawLine(center + new Vector2(0, -12), center + new Vector2(0, 9), 3, color);
                DrawLine(center + new Vector2(-7, 2), center + new Vector2(0, 9), 3, color);
                DrawLine(center + new Vector2(7, 2), center + new Vector2(0, 9), 3, color);
            }
            else
            {
                DrawLine(center + new Vector2(-8, 8), center + new Vector2(10, -10), 3, color);
                DrawLine(center + new Vector2(-2, -10), center + new Vector2(10, -10), 3, color);
                DrawLine(center + new Vector2(10, 2), center + new Vector2(10, -10), 3, color);
            }
        }
        DrawDynamicOptionText(text, new Rectangle(bounds.X + 64, bounds.Y + 12, bounds.Width - 78, 42), color, 0.41f);
    }

    private void DrawRoomIcon(Rectangle bounds, LobbyHomeTarget target, Color color)
    {
        if (target == LobbyHomeTarget.ReferenceGo)
        {
            DrawLine(new(bounds.X + 19, bounds.Y + 10), new(bounds.X + 4, bounds.Center.Y), 4, color);
            DrawLine(new(bounds.X + 4, bounds.Center.Y), new(bounds.X + 19, bounds.Bottom - 10), 4, color);
            DrawLine(new(bounds.Right - 19, bounds.Y + 10), new(bounds.Right - 4, bounds.Center.Y), 4, color);
            DrawLine(new(bounds.Right - 4, bounds.Center.Y), new(bounds.Right - 19, bounds.Bottom - 10), 4, color);
            DrawLine(new(bounds.Center.X + 5, bounds.Y + 8), new(bounds.Center.X - 5, bounds.Bottom - 8), 3, color);
            return;
        }
        DrawRect(bounds, 2, color);
        for (var i = 1; i < 4; i++)
        {
            DrawLine(new(bounds.X + i * 14, bounds.Y), new(bounds.X + i * 14, bounds.Bottom), 1, color);
            DrawLine(new(bounds.X, bounds.Y + i * 14), new(bounds.Right, bounds.Y + i * 14), 1, color);
        }
        _drawingContext.DrawStone(new(bounds.X + 14, bounds.Y + 28), 8, true);
        _drawingContext.DrawStone(new(bounds.X + 42, bounds.Y + 14), 8, false);
        if (target == LobbyHomeTarget.CaptureGame) _drawingContext.DrawStone(new(bounds.X + 28, bounds.Y + 42), 8, true);
        if (target == LobbyHomeTarget.OnlineMatch)
            DrawLine(new(bounds.Right - 8, bounds.Bottom - 8), new(bounds.Right + 8, bounds.Bottom + 8), 3, color);
    }
}
