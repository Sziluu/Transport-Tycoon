using System.Collections.Generic;
using System.Drawing;

namespace TransportTycoon_BOSZI.Model;

public class Route
{
    private List<Point> _stops;
    private string _name;

    public List<Point> Stops => _stops;
    public string Name => _name;

    public Route(string name)
    {
        _name = name;
        _stops = new List<Point>();
    }

    public Route(string name, List<Point> stops)
    {
        _name = name;
        _stops = new List<Point>(stops);
    }

    public void AddStop(Point stopPosition)
    {
        _stops.Add(stopPosition);
    }

    public void RemoveStop(int index)
    {
        if (index >= 0 && index < _stops.Count)
            _stops.RemoveAt(index);
    }

    public Point GetNextStop(int currentIndex)
    {
        if (_stops.Count == 0) return Point.Empty;
        return _stops[(currentIndex + 1) % _stops.Count];
    }

    public bool IsValid()
    {
        return _stops.Count >= 2;
    }
}
