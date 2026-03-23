using System.Collections.Generic;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.MapObjects.Industrial;

namespace TransportTycoon_BOSZI;

public class Map
{
    private int _size;
    private int _money;
    private MapObject[,] _map;

    private readonly Mine _mine = new();
    private readonly Farm _farm = new();
    private readonly Factory _factory = new();
    private readonly Field _field = new();
    private readonly Water _water = new();
    private readonly House _house = new();
    private readonly Empty _empty = new();

    private HashSet<(int x, int y)> _emptyTiles = new();
    private Random _rand = new Random();

    private List<City> _cities = new();
    private List<industrial_MapObject> _industries = new();

    public int Size => _size;
    public int Money
    {
        get => _money;
        set => _money = value;
    }
    public MapObject[,] MapGrid => _map;
    public List<City> Cities => _cities;
    public List<industrial_MapObject> Industries => _industries;

    public Map(int size, int money)
    {
        _size = size;
        _money = money;
    }

    public MapObject[,] createMap()
    {
        _map = new MapObject[_size, _size];
        _emptyTiles.Clear();
        _cities.Clear();
        _industries.Clear();

        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                _map[i, j] = _empty;
                _emptyTiles.Add((i, j));
            }
        }

        int startX = _rand.Next(1, _size - 1);
        int startY = _rand.Next(1, _size - 1);
        PlaceTile(startX, startY, _house);

        while (_emptyTiles.Count > 0)
        {
            var smallest = GetBestCandidate();

            var (coord, unusable) = GetConstraints(smallest.x, smallest.y);
            List<MapObject> usable = GetUsableObjects(unusable);

            if (usable.Count == 0)
            {
                PlaceTile(smallest.x, smallest.y, _field);
                continue;
            }

            MapObject selected = usable[_rand.Next(usable.Count)];

            if (selected is House && isCityPlacable(smallest))
            {
                turnIntoCity(smallest);
            }
            else if (selected is Factory or Mine or Farm)
            {
                if (isFactoryFarmMinePlacable(smallest))
                    turnIntoFactoryFarmMine(smallest, selected);
                else
                    PlaceTile(smallest.x, smallest.y, _field);
            }
            else
            {
                PlaceTile(smallest.x, smallest.y, selected);
            }
        }

        return _map;
    }

    private (int x, int y) GetBestCandidate()
    {
        return _emptyTiles.First();
    }

    private void PlaceTile(int x, int y, MapObject obj)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return;
        _map[x, y] = obj;
        _emptyTiles.Remove((x, y));
    }

    private List<MapObject> GetUsableObjects(HashSet<Type> unusable)
    {
        List<MapObject> usable = new();
        if (!unusable.Contains(typeof(Mine))) usable.Add(_mine);
        if (!unusable.Contains(typeof(Factory))) usable.Add(_factory);
        if (!unusable.Contains(typeof(House))) usable.Add(_house);
        if (!unusable.Contains(typeof(Farm))) usable.Add(_farm);
        if (!unusable.Contains(typeof(Field))) usable.Add(_field);
        if (!unusable.Contains(typeof(Water))) usable.Add(_water);
        return usable;
    }

    private ((int x, int y), HashSet<Type>) GetConstraints(int i, int j)
    {
        HashSet<Type> unusable = new();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int k = 0; k < 4; k++)
        {
            int ni = i + dx[k];
            int nj = j + dy[k];

            if (ni >= 0 && ni < _size && nj >= 0 && nj < _size)
            {
                var neighbor = _map[ni, nj];
                if (neighbor is House)
                {
                    unusable.Add(typeof(Mine));
                    unusable.Add(typeof(Farm));
                    unusable.Add(typeof(Factory));
                }
                else if (neighbor is Factory or Mine or Farm)
                {
                    unusable.Add(typeof(House));
                }
            }
        }
        return ((i, j), unusable);
    }

    private void turnIntoCity((int x, int y) s)
    {
        PlaceTile(s.x, s.y, _house);
        PlaceTile(s.x + 1, s.y, _house);
        PlaceTile(s.x, s.y + 1, _house);
        PlaceTile(s.x, s.y - 1, _house);
        PlaceTile(s.x - 1, s.y, _house);
        PlaceTile(s.x + 1, s.y + 1, _house);
        PlaceTile(s.x - 1, s.y - 1, _house);
        PlaceTile(s.x + 1, s.y - 1, _house);
        PlaceTile(s.x - 1, s.y + 1, _house);

        //város rögzítése
        string name = "City_" + (_cities.Count + 1);
        City city = new City(name, new coord(s.x - 1, s.y - 1), 3);
        _cities.Add(city);
    }

    private void turnIntoFactoryFarmMine((int x, int y) s, MapObject a)
    {
        PlaceTile(s.x, s.y, a);
        PlaceTile(s.x + 1, s.y, a);
        PlaceTile(s.x + 1, s.y + 1, a);
        PlaceTile(s.x, s.y + 1, a);

        if (a is industrial_MapObject industry)
        {
            _industries.Add(industry);
        }
    }

    private bool isFactoryFarmMinePlacable((int x, int y) b) =>
        b.x + 1 < _size && b.y + 1 < _size &&
        isSomethingPlacable(_map[b.x + 1, b.y]) &&
        isSomethingPlacable(_map[b.x, b.y + 1]) &&
        isSomethingPlacable(_map[b.x + 1, b.y + 1]) &&
        isSomethingPlacable(_map[b.x, b.y]);

    private bool isCityPlacable((int x, int y) s)
    {
        //határ ellenőrzés
        if (s.x - 1 < 0 || s.x + 1 >= _size || s.y - 1 < 0 || s.y + 1 >= _size)
            return false;

        //mind a 9 mező ellenőrzése (3x3)
        return
            isSomethingPlacable(_map[s.x, s.y]) &&
            isSomethingPlacable(_map[s.x + 1, s.y]) &&
            isSomethingPlacable(_map[s.x - 1, s.y]) &&
            isSomethingPlacable(_map[s.x, s.y + 1]) &&
            isSomethingPlacable(_map[s.x, s.y - 1]) &&
            isSomethingPlacable(_map[s.x + 1, s.y + 1]) &&
            isSomethingPlacable(_map[s.x - 1, s.y - 1]) &&
            isSomethingPlacable(_map[s.x + 1, s.y - 1]) &&
            isSomethingPlacable(_map[s.x - 1, s.y + 1]);
    }

    private bool isSomethingPlacable(MapObject a) => a is Field or Empty;

    // ---------- Játékos akciók ----------

    public bool BuildRoad(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;

        MapObject current = _map[x, y];
        int cost = 50;

        //erdős mezőre drágább
        if (current is Field f && f.NumberOfTrees > 0)
            cost = 50 + f.NumberOfTrees * 20;
        else if (!(current is Empty || current is Field))
            return false; //nem lehet ide építeni

        if (_money < cost) return false;

        Road road = new Road();
        road.Coord = new coord(x, y);
        _map[x, y] = road;
        _money -= cost;
        return true;
    }

    public bool PlaceStop(int x, int y, Material material)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;

        //megálló csak üres mezőre vagy field-re, és szomszédos kell legyen úttal/várossal/iparral
        MapObject current = _map[x, y];
        if (!(current is Empty || current is Field)) return false;

        if (!HasAdjacentRoadOrCity(x, y)) return false;

        int cost = 100;
        if (_money < cost) return false;

        Stop stop = new Stop(material);
        stop.Coord = new coord(x, y);
        _map[x, y] = stop;
        _money -= cost;
        return true;
    }

    private bool HasAdjacentRoadOrCity(int x, int y)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int k = 0; k < 4; k++)
        {
            int nx = x + dx[k];
            int ny = y + dy[k];

            if (nx >= 0 && nx < _size && ny >= 0 && ny < _size)
            {
                MapObject neighbor = _map[nx, ny];
                if (neighbor is Road || neighbor is House || neighbor is industrial_MapObject)
                    return true;
            }
        }
        return false;
    }

    public bool PlaceGarage(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return false;

        MapObject current = _map[x, y];
        if (!(current is Empty || current is Field)) return false;

        int cost = 200;
        if (_money < cost) return false;

        Garage garage = new Garage();
        garage.Coord = new coord(x, y);
        _map[x, y] = garage;
        _money -= cost;
        return true;
    }

    public MapObject GetTile(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return null;
        return _map[x, y];
    }
}
