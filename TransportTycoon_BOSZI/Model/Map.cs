using System.Collections.Generic;
using System.Globalization;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;
namespace TransportTycoon_BOSZI;

public class Map
{
    private int _size;
    private int _money;
    private MapObject[,] _map;
    private Random _rand = new Random();

    private List<City> _cities = new();
    private List<industrial_MapObject> _industries = new();
    private List<(int x, int y)> _industryPositions = new();

    public int Size => _size;
    public int Money { get => _money; set => _money = value; }
    public MapObject[,] GetMap => _map;
    public List<City> Cities => _cities;
    public List<industrial_MapObject> Industries => _industries;

    public Map(int size, int money) { _size = size; _money = money; }

    public MapObject[,] CreateMap()
    {
        _map = new MapObject[_size, _size];
        _cities.Clear(); _industries.Clear(); _industryPositions.Clear();

        double[,] noise = GenerateNoise();

        for (int i = 0; i < _size; i++)
            for (int j = 0; j < _size; j++)
            {
                double val = noise[i, j];
                if (val < 0.10)
                    _map[i, j] = new Water();
                else
                {
                    var f = new Field();
                    if (val > 0.5 && _rand.Next(100) < 30)
                        f.NumberOfTrees = _rand.Next(1, 5);
                    _map[i, j] = f;
                }
            }

        int cityCount = Math.Max(5, _size / 10);
        for (int i = 0; i < cityCount; i++) TryPlaceCity();

        int industryCount = Math.Max(8, _size / 10);
        for (int i = 0; i < industryCount; i++) TryPlaceIndustry();

        BuildRoadNetwork();

        PlaceAutoStops();

        return _map;
    }

    private double[,] GenerateNoise()
    {
        double[,] result = new double[_size, _size];
        int octaves = 3;
        double persistence = 0.5;

       
        int gridSize = 16;
        double[,] gradients = new double[gridSize + 1, gridSize + 1];
        for (int i = 0; i <= gridSize; i++)
            for (int j = 0; j <= gridSize; j++)
                gradients[i, j] = _rand.NextDouble();

        for (int x = 0; x < _size; x++)
            for (int y = 0; y < _size; y++)
            {
                double total = 0;
                double amplitude = 1;
                double frequency = 1;
                double maxVal = 0;

                for (int oct = 0; oct < octaves; oct++)
                {
                    double sampleX = x * frequency / _size * gridSize;
                    double sampleY = y * frequency / _size * gridSize;

                    int x0 = (int)sampleX % gridSize;
                    int y0 = (int)sampleY % gridSize;
                    int x1 = (x0 + 1) % (gridSize + 1);
                    int y1 = (y0 + 1) % (gridSize + 1);
                    double fx = sampleX - Math.Floor(sampleX);
                    double fy = sampleY - Math.Floor(sampleY);

                    fx = fx * fx * (3 - 2 * fx);
                    fy = fy * fy * (3 - 2 * fy);

                    double v00 = gradients[x0, y0];
                    double v10 = gradients[x1, y0];
                    double v01 = gradients[x0, y1];
                    double v11 = gradients[x1, y1];

                    double i0 = v00 + fx * (v10 - v00);
                    double i1 = v01 + fx * (v11 - v01);
                    double val = i0 + fy * (i1 - i0);

                    total += val * amplitude;
                    maxVal += amplitude;
                    amplitude *= persistence;
                    frequency *= 2;
                }
                result[x, y] = total / maxVal;
            }
        return result;
    }
    
    private void TryPlaceCity()
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int cx = _rand.Next(5, _size - 5);
            int cy = _rand.Next(5, _size - 5);
            if (!IsAreaLand(cx - 1, cy - 1, 5, 5)) continue;
            if (TooClose(cx, cy, 15)) continue;
            _map[cx, cy] = new House { IsPartOfCity = true };
            _map[cx + 1, cy] = new Road { IsPartOfCity = true };
            _map[cx + 2, cy] = new House { IsPartOfCity = true };
            _map[cx, cy + 1] = new Road { IsPartOfCity = true };
            _map[cx + 1, cy + 1] = new Road { IsPartOfCity = true };
            _map[cx + 2, cy + 1] = new Road { IsPartOfCity = true };
            _map[cx, cy + 2] = new House { IsPartOfCity = true };
            _map[cx + 1, cy + 2] = new Road { IsPartOfCity = true };
            _map[cx + 2, cy + 2] = new House { IsPartOfCity = true };

            _cities.Add(new City("City_" + (_cities.Count + 1), new Coord(cx, cy), 3));
            return;
        }
    }

    private void TryPlaceIndustry()
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int ix = _rand.Next(3, _size - 3);
            int iy = _rand.Next(3, _size - 3);
            if (!IsAreaLand(ix, iy, 2, 2)) continue;
            if (TooClose(ix, iy, 8)) continue;

            industrial_MapObject industry = (_rand.Next(4)) switch
            {
                0 => new Mine(),
                1 => new Factory(),
                2 => new Factory(Material.wheat, Material.bread),
                _ => new Farm()
            };

            _map[ix, iy] = industry;
            _map[ix + 1, iy] = industry;
            _map[ix, iy + 1] = industry;
            _map[ix + 1, iy + 1] = industry;

            _industries.Add(industry);
            _industryPositions.Add((ix, iy));
            return;
        }
    }

    private bool IsAreaLand(int x, int y, int w, int h)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= _size || ny < 0 || ny >= _size) return false;
                if (_map[nx, ny] is Water) return false;
                if (!(_map[nx, ny] is Field)) return false;
            }
        return true;
    }

    private bool TooClose(int x, int y, int minDist)
    {
        foreach (var city in _cities)
            if (Math.Max(Math.Abs(city.TopLeft.X - x), Math.Abs(city.TopLeft.Y - y)) < minDist) return true;
        foreach (var ip in _industryPositions)
            if (Math.Max(Math.Abs(ip.x - x), Math.Abs(ip.y - y)) < minDist) return true;
        return false;
    }
    
    private void BuildRoadNetwork()
    {
        List<(int x, int y)> nodes = new();
        foreach (var city in _cities) nodes.Add((city.TopLeft.X + 1, city.TopLeft.Y + 1));
        foreach (var ip in _industryPositions) nodes.Add((ip.x - 1, ip.y));
        if (nodes.Count < 2) return;

        HashSet<int> connected = new() { 0 };
        while (connected.Count < nodes.Count)
        {
            int bestFrom = -1, bestTo = -1, bestDist = int.MaxValue;
            foreach (int from in connected)
                for (int to = 0; to < nodes.Count; to++)
                {
                    if (connected.Contains(to)) continue;
                    int dist = Math.Abs(nodes[from].x - nodes[to].x) + Math.Abs(nodes[from].y - nodes[to].y);
                    if (dist < bestDist) { bestDist = dist; bestFrom = from; bestTo = to; }
                }
            if (bestTo == -1) break;
            BuildRoadPath(nodes[bestFrom].x, nodes[bestFrom].y, nodes[bestTo].x, nodes[bestTo].y);
            connected.Add(bestTo);
        }
    }

    private void BuildRoadPath(int x1, int y1, int x2, int y2)
    {
        int x = x1, y = y1;
        int sx = x2 > x1 ? 1 : -1;
        while (x != x2) { PlaceRoadIfField(x, y); x += sx; }
        int sy = y2 > y1 ? 1 : -1;
        while (y != y2) { PlaceRoadIfField(x, y); y += sy; }
        PlaceRoadIfField(x, y);
    }

    private void PlaceRoadIfField(int x, int y)
    {
        if (x >= 0 && x < _size && y >= 0 && y < _size && _map[x, y] is Field)
            _map[x, y] = new Road();
    }

    private void PlaceAutoStops()
    {
        foreach (var city in _cities)
        {
            int cx = city.TopLeft.X, cy = city.TopLeft.Y;
            if (!TryPlaceAutoStop(cx + 1, cy - 1))
                TryPlaceAutoStop(cx + 1, cy + 3);
        }
        foreach (var ip in _industryPositions)
        {
            
            if (!TryPlaceAutoStop(ip.x - 1, ip.y) &&
                !TryPlaceAutoStop(ip.x, ip.y - 1) &&
                !TryPlaceAutoStop(ip.x + 2, ip.y) &&
                !TryPlaceAutoStop(ip.x, ip.y + 2))
            {  }
        }
    }

    private bool TryPlaceAutoStop(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;
        if (!(_map[x, y] is Field)) return false;
        if (!HasAdjacentRoadOrCity(x, y)) return false;
        _map[x, y] = new Stop(Material.nothing) { Coord = new Coord(x, y) };
        return true;
    }

    public bool BuildRoad(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;
        if (!(_map[x, y] is Field)) return false;
        int cost = (_map[x, y] is Field f && f.NumberOfTrees > 0) ? 50 + f.NumberOfTrees * 20 : 50;
        if (_money < cost) return false;
        _map[x, y] = new Road { Coord = new Coord(x, y) };
        _money -= cost;
        return true;
    }

    public bool PlaceStop(int x, int y, Material material)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;
        if (!(_map[x, y] is Field)) return false;
        if (!HasAdjacentRoadOrCity(x, y)) return false;
        if (_money < 100) return false;
        _map[x, y] = new Stop(material) { Coord = new Coord(x, y) };
        _money -= 100;
        return true;
    }

    private bool HasAdjacentRoadOrCity(int x, int y)
    {
        int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };
        for (int k = 0; k < 4; k++)
        {
            int nx = x + dx[k], ny = y + dy[k];
            if (nx >= 0 && nx < _size && ny >= 0 && ny < _size)
                if (_map[nx, ny] is Road or House or industrial_MapObject) return true;
        }
        return false;
    }

    public bool PlaceGarage(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;
        if (!(_map[x, y] is Field)) return false;
        if (_money < 200) return false;
        _map[x, y] = new Garage { Coord = new Coord(x, y) };
        _money -= 200;
        return true;
    }

    public MapObject GetTile(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return null;
        return _map[x, y];
    }

    public List<(int x, int y)> GetAllStops()
    {
        var stops = new List<(int x, int y)>();
        for (int i = 0; i < _size; i++)
            for (int j = 0; j < _size; j++)
                if (_map[i, j] is Stop) stops.Add((i, j));
        return stops;
    }

    public List<(int x, int y)> GetValidStartStops(Material material)
    {
        var result = new List<(int x, int y)>();
        for (int i = 0; i < _size; i++)
            for (int j = 0; j < _size; j++)
                if (_map[i, j] is Stop && IsAdjacentToProducer(i, j, material))
                    result.Add((i, j));
        return result;
    }

    public List<(int x, int y)> GetValidDestinationStops(Material material)
    {
        var result = new List<(int x, int y)>();
        for (int i = 0; i < _size; i++)
            for (int j = 0; j < _size; j++)
                if (_map[i, j] is Stop && IsAdjacentToConsumer(i, j, material))
                    result.Add((i, j));
        return result;
    }

    private bool IsAdjacentToProducer(int x, int y, Material material)
    {
        if (material == Material.nothing) return true;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int k = 0; k < 4; k++)
        {
            int nx = x + dx[k], ny = y + dy[k];
            if (nx < 0 || nx >= _size || ny < 0 || ny >= _size) continue;
            var tile = _map[nx, ny];
            if (tile is Mine mine && mine.Kind == material) return true;
            if (tile is Farm farm && farm.Kind == material) return true;
            if (tile is Factory fac && fac.Kind == material) return true;
        }
        return false;
    }

    private bool IsAdjacentToConsumer(int x, int y, Material material)
    {
        if (material == Material.nothing) return true;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int k = 0; k < 4; k++)
        {
            int nx = x + dx[k], ny = y + dy[k];
            if (nx < 0 || nx >= _size || ny < 0 || ny >= _size) continue;
            var tile = _map[nx, ny];
            if (tile is Factory fac && fac.Using == material) return true;
            if (tile is House && IsFinishedGood(material)) return true;
        }
        return false;
    }

    private static bool IsFinishedGood(Material material) =>
        material == Material.iron || material == Material.bread ||
        material == Material.paper || material == Material.coal;
}
