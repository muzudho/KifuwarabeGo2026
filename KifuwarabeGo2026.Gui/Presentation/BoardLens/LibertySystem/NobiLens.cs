namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{
    /// <summary>NOBI LENS の足と候補点を描画します。</summary>
    private void DrawNobiLens(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        var legColor = RenGraphCellColor(session.CurrentTurn);
        var candidateColor = new Color(126, 255, 188);
        var legThickness = MathHelper.Clamp(cell * 0.045f, 2f, 5f);
        var markerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);

        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            if (ren.Stone != session.CurrentTurn)
                continue;

            var contacts = new List<(GoPoint From, GoPoint To)>();
            foreach (var point in ren.Points)
            {
                AddCandidate(point, point.X - 1, point.Y);
                AddCandidate(point, point.X + 1, point.Y);
                AddCandidate(point, point.X, point.Y - 1);
                AddCandidate(point, point.X, point.Y + 1);
            }

            var markers = new HashSet<GoPoint>();
            foreach (var contact in contacts)
            {
                var from = BoardPoint(start, cell, contact.From.X, contact.From.Y);
                var target = BoardPoint(start, cell, contact.To.X, contact.To.Y);
                DrawLine(from, target, legThickness, legColor);
                markers.Add(contact.To);
            }

            foreach (var marker in markers)
            {
                var center = BoardPoint(start, cell, marker.X, marker.Y);
                DrawCircle(center, markerRadius + 3f, RenGraphCellColor(session.CurrentTurn));
                DrawCircle(center, markerRadius, candidateColor);
            }

            void AddCandidate(GoPoint from, int x, int y)
            {
                if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size ||
                    renParse.GetRen(renParse.GetRenNumber(x, y)).Stone != GoStone.Empty ||
                    !session.IsNobiCandidate(x, y))
                    return;

                contacts.Add((from, new GoPoint(x, y)));
            }
        }
    }
}
