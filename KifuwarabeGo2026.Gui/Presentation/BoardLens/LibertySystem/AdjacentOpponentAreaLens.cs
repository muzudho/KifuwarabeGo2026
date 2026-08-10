namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    /// <summary>隣接する連への足と接続マーカーを描画します。</summary>
    private void DrawAdjacentRenRelationships(GoRenParseResult renParse, List<(GoPoint From, GoPoint To)> contacts, HashSet<int> adjacentRenNumbers, Color legColor, float legThickness, Vector2 start, float cell)
    {
        var originalMarkerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
        var innerHalfSize = Math.Max(2, (int)MathF.Round(originalMarkerRadius - legThickness));
        var outerHalfSize = Math.Max(innerHalfSize + 2, (int)MathF.Round(originalMarkerRadius + 3f - legThickness));
        var sortedRenNumbers = new List<int>(adjacentRenNumbers);
        sortedRenNumbers.Sort();
        foreach (var adjacentRenNumber in sortedRenNumbers)
        {
            var targetRen = renParse.GetRen(adjacentRenNumber);
            var selectedContact = FindFirstContact(adjacentRenNumber);
            var sourceCenter = BoardPoint(start, cell, selectedContact.From.X, selectedContact.From.Y);
            var boundaryCenter = BoardPoint(start, cell, selectedContact.To.X, selectedContact.To.Y);
            var sourceDirection = new Vector2(selectedContact.From.X - selectedContact.To.X, selectedContact.From.Y - selectedContact.To.Y);
            sourceDirection.Normalize();
            var markerCenter = boundaryCenter + (new Vector2(-sourceDirection.Y, sourceDirection.X) * outerHalfSize * 2f);
            DrawLine(sourceCenter, markerCenter, legThickness, legColor);
            var outerBounds = new Rectangle((int)MathF.Round(markerCenter.X) - outerHalfSize, (int)MathF.Round(markerCenter.Y) - outerHalfSize, outerHalfSize * 2, outerHalfSize * 2);
            var innerBounds = new Rectangle((int)MathF.Round(markerCenter.X) - innerHalfSize, (int)MathF.Round(markerCenter.Y) - innerHalfSize, innerHalfSize * 2, innerHalfSize * 2);
            FillRect(outerBounds, legColor);
            FillRect(innerBounds, RenGraphCellColor(targetRen.Stone));
        }

        (GoPoint From, GoPoint To) FindFirstContact(int targetRenNumber)
        {
            (GoPoint From, GoPoint To)? selected = null;
            foreach (var contact in contacts)
                if (renParse.GetRenNumber(contact.To.X, contact.To.Y) == targetRenNumber && (selected is null || ComesFirst(contact, selected.Value))) selected = contact;
            return selected ?? throw new InvalidOperationException("Adjacent ren has no boundary contact.");
        }

        static bool ComesFirst((GoPoint From, GoPoint To) candidate, (GoPoint From, GoPoint To) current) =>
            candidate.To.Y < current.To.Y ||
            (candidate.To.Y == current.To.Y && candidate.To.X < current.To.X) ||
            (candidate.To == current.To && (candidate.From.Y < current.From.Y || (candidate.From.Y == current.From.Y && candidate.From.X < current.From.X)));
    }

    private static int SumAdjacentRenAreas(GoRenParseResult renParse, HashSet<int> adjacentRenNumbers)
    {
        var area = 0;
        foreach (var renNumber in adjacentRenNumbers) area += renParse.GetRen(renNumber).Points.Count;
        return area;
    }
}
