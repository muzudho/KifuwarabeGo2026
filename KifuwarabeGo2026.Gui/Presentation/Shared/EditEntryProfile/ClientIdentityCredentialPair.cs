namespace KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using System;

/// <summary>HANDLE と PASSWORD を一組として表示する、Entry Profile 共通行です。</summary>
internal static class ClientIdentityCredentialPair
{
    public const int Top = 470;
    public const int Pitch = 62;
    public static Rectangle RowBounds(int index) => new(548, Top + index * Pitch, 812, 54);
    public static Rectangle HandleBounds(int index) => new(610, Top + index * Pitch + 8, 270, 36);
    public static Rectangle PasswordBounds(int index) => new(900, Top + index * Pitch + 8, 260, 36);
    public static Rectangle VisibilityBounds(int index) => new(1170, Top + index * Pitch + 8, 42, 36);
    public static Rectangle RemoveBounds(int index) => new(1222, Top + index * Pitch + 6, 120, 40);

    public static (int Index, EntryProfileEditField Field)? GetFieldHit(Point point, int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (HandleBounds(index).Contains(point)) return (index, EntryProfileEditField.ClientIdentityHandle);
            if (PasswordBounds(index).Contains(point)) return (index, EntryProfileEditField.ClientIdentityPassword);
        }
        return null;
    }

    public static int GetVisibilityHit(Point point, int count) => GetHit(point, count, VisibilityBounds);
    public static int GetRemoveHit(Point point, int count) => GetHit(point, count, RemoveBounds);

    private static int GetHit(Point point, int count, Func<int, Rectangle> bounds)
    {
        for (var index = 0; index < count; index++) if (bounds(index).Contains(point)) return index;
        return -1;
    }
}
