using System.Collections.Generic;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;
namespace TransportTycoon_BOSZI;

public class Map
{
    private int _size;
    public int Size
    { get { return _size; } set { _size = value; } }
    private int _money;
    public int Money
    { get { return _money; } set { _money = value; } }
    private MapObject[,] _map;
    public MapObject[,] GetMap
    { get { return _map; } set { _map = value; } }
    private readonly Mine _mine = new();
    private readonly Farm _farm = new();
    private readonly Factory _factory = new();
    private readonly Field _field = new();
    private readonly Water _water = new();
    private readonly House _house = new();
    private readonly Empty _empty = new();

    private HashSet<(int x, int y)> _emptyTiles = new();
    private List<City> _cities = new();
    public List<City> Cities { get { return _cities; } set { _cities = value; } }
    private List<industrial_MapObject> _industries = new();
    public List<industrial_MapObject> Industries => _industries;
    public HashSet<(int x, int y)> EmptyTiles
    {
        get { return _emptyTiles; }
        set { _emptyTiles = value; }
    }
    private Random _rand = new Random();

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
            else if (selected is Factory ||selected is Mine || selected is Farm)
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
        PlaceTile(s.x,s.y, _house);
        PlaceTile(s.x+1, s.y, _house);
        PlaceTile(s.x, s.y+1, _house);
        PlaceTile(s.x, s.y-1, _house);
        PlaceTile(s.x-1, s.y, _house);
        PlaceTile(s.x+1, s.y+1, _house);
        PlaceTile(s.x-1, s.y-1, _house);
        PlaceTile(s.x+1, s.y-1, _house);
        PlaceTile(s.x-1, s.y+1, _house);
    }

    private void turnIntoFactoryFarmMine((int x, int y) s, MapObject a)
    {
        PlaceTile(s.x, s.y, a);
        PlaceTile(s.x + 1, s.y, a);
        PlaceTile(s.x + 1, s.y + 1, a);
        PlaceTile(s.x, s.y + 1, a);
    }

    private bool isFactoryFarmMinePlacable((int x, int y) b) =>
        b.x + 1 < _size && b.y + 1 < _size &&
        isSomethingPlacable(_map[b.x + 1, b.y]) &&
        isSomethingPlacable(_map[b.x, b.y + 1]) &&
        isSomethingPlacable(_map[b.x + 1, b.y + 1]) &&
        isSomethingPlacable(_map[b.x, b.y]);

    private bool isCityPlacable((int x, int y) s)
    {
        if (s.x - 1 < 0 || s.y - 1 < 0 || s.x + 1 > _size || s.y + 1 > _size)
        { return false;}
        return
            isSomethingPlacable(_map[s.x, s.y]) &&
            isSomethingPlacable(_map[s.x + 1, s.y]) &&
            isSomethingPlacable(_map[s.x, s.y + 1]) &&
            isSomethingPlacable(_map[s.x, s.y - 1]) &&
            isSomethingPlacable(_map[s.x - 1, s.y]) &&
            isSomethingPlacable(_map[s.x + 1, s.y + 1]) &&
            isSomethingPlacable(_map[s.x - 1, s.y - 1]) &&
            isSomethingPlacable(_map[s.x + 1, s.y - 1]) &&
            isSomethingPlacable(_map[s.x - 1, s.y + 1]);
    }
    public TileType GetTileType(MapObject obj)
    {
        if (obj is Mine)    return TileType.Mine;
        if (obj is Farm)    return TileType.Farm;
        if (obj is Factory) return TileType.Factory;
        if (obj is Field)   return TileType.Field;
        if (obj is Water)   return TileType.Water;
        if (obj is House)   return TileType.House;
        return TileType.Empty;
    }
    public MapObject TileTypeToObject(TileType type)
    {
        if (type == TileType.Mine)    return _mine;
        if (type == TileType.Farm)    return _farm;
        if (type == TileType.Factory) return _factory;
        if (type == TileType.Field)   return _field;
        if (type == TileType.Water)   return _water;
        if (type == TileType.House)   return _house;
        return _empty;
    }
    private bool isSomethingPlacable(MapObject a) => a is Field or Empty;
    public MapObject GetTile(int x, int y)
    {
        if (x < 0 || x >= _size || y < 0 || y >= _size) return null;
        return _map[x, y];
    }

}