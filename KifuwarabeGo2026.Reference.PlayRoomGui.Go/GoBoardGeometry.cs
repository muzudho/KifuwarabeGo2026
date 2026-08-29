namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>GUIフレームワークに依存しない画面上の点です。</summary>
public readonly record struct GoBoardScreenPoint(float X, float Y);

/// <summary>GUIフレームワークに依存しない盤領域です。</summary>
public readonly record struct GoBoardViewport(float X, float Y, float Width, float Height);

/// <summary>囲碁盤の交点配置と画面入力から盤座標への変換を所有します。</summary>
public readonly record struct GoBoardGeometry(
    int BoardSize,
    GoBoardViewport Viewport,
    GoBoardScreenPoint Start,
    float Cell)
{
    public static GoBoardGeometry Create(int boardSize, GoBoardViewport viewport)
    {
        if (boardSize < 2)
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be at least 2.");
        if (viewport.Width <= 0 || viewport.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport, "Viewport must have positive dimensions.");

        var margin = boardSize switch
        {
            <= 9 => 82f,
            <= 13 => 68f,
            _ => 50f,
        };
        var playable = Math.Min(viewport.Width, viewport.Height) - (margin * 2);
        if (playable <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewport), viewport, "Viewport is too small for the board margin.");

        return new GoBoardGeometry(
            boardSize,
            viewport,
            new GoBoardScreenPoint(viewport.X + margin, viewport.Y + margin),
            playable / (boardSize - 1));
    }

    public GoBoardScreenPoint GetScreenPoint(GoPoint intersection)
    {
        if (intersection.X < 0 || intersection.X >= BoardSize ||
            intersection.Y < 0 || intersection.Y >= BoardSize)
            throw new ArgumentOutOfRangeException(nameof(intersection), intersection, "Intersection is outside the board.");

        return new GoBoardScreenPoint(
            Start.X + (Cell * intersection.X),
            Start.Y + (Cell * intersection.Y));
    }

    public bool TryGetIntersection(GoBoardScreenPoint screenPoint, out GoPoint intersection)
    {
        var nearestX = (int)MathF.Round((screenPoint.X - Start.X) / Cell);
        var nearestY = (int)MathF.Round((screenPoint.Y - Start.Y) / Cell);
        if (nearestX < 0 || nearestX >= BoardSize || nearestY < 0 || nearestY >= BoardSize)
        {
            intersection = default;
            return false;
        }

        var center = GetScreenPoint(new GoPoint(nearestX, nearestY));
        var distanceX = screenPoint.X - center.X;
        var distanceY = screenPoint.Y - center.Y;
        var hitRadius = Math.Max(16f, Cell * 0.42f);
        if ((distanceX * distanceX) + (distanceY * distanceY) > hitRadius * hitRadius)
        {
            intersection = default;
            return false;
        }

        intersection = new GoPoint(nearestX, nearestY);
        return true;
    }
}
