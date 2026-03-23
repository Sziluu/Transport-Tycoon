namespace TransportTycoon_BOSZI;

public class coord
{
    public int X { get; set; }
    public int Y { get; set; }

    public coord()
    {
        X = 0;
        Y = 0;
    }

    public coord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        if (obj is coord other)
            return X == other.X && Y == other.Y;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(X, Y);

}