
namespace TransportTycoon_BOSZI.Model;

public class Coord
{
    public int X { get; }
    public int Y { get; }

    public Coord()
    {
        X = 0;
        Y = 0;
    }

    public Coord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (obj is Coord other)
            return X == other.X && Y == other.Y;
        return false;
    }
    public override int GetHashCode() => HashCode.Combine(X, Y);

}