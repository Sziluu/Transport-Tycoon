using System.Drawing;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.MapObjects.BridgeTypes;

namespace TransportTycoon_BOSZI.Model.Vehicles;

public record VehicleSpec(int Speed, int Capacity, int Cost);

public static class VehicleCatalog
{

    static readonly Dictionary<Size, int> CapacityValues = new()
    {
        { Size.Small, 50 },
        { Size.Medium, 100 },
        { Size.Big, 200 }
    };

    static readonly Dictionary<Material, int> CostModifier = new()
    {
        { Material.iron, 200 },
        { Material.ironOre, 100 },
        { Material.wood, 30 },
        { Material.paper, 60 },
        { Material.wheat, 70 },
        { Material.coal, 100 },
        { Material.bread, 150 },
        { Material.nothing, 0 }
    };

    public static VehicleSpec GetSpec(Speed speed, Size size, Material material)
    {
        int vehicleSpeed = (int)speed; 
        int capacity = CapacityValues[size];
        int baseCost = vehicleSpeed * 2 + capacity * 5;
        int finalCost = baseCost + CostModifier[material];

        return new VehicleSpec(vehicleSpeed, capacity, finalCost);
    }
}

public abstract class Vehicle
{
    private Speed _speed;
    private int _capacity;
    private int _cost;
    private Size _size;
    private Material _material;
    private Point _start;
    private Point _target;
    private List<Point> _pathToFollow;

    //útvonal kezelés
    private Route _assignedRoute;
    private int _currentRouteStopIndex;
    private int _currentPathIndex;
    private bool _isMoving;
    private int _loadedAmount;
    private int _maintenanceCost;
    private Point _currentPosition;

    public Speed VehicleSpeed => _speed;
    public int Capacity => _capacity;
    public int Cost => _cost;
    public Material CarriedMaterial => _material;
    public Size VehicleSize => _size;

    public Point Start
    {
        get { return _start; }
        set
        {
            if (value != _start)
            {
                _start = value;
            }
        }
    }

    public Point Target
    {
        get { return _target; }
        set
        {
            if (value != _target)
            {
                _target = value;
            }
        }
    }

    public Point CurrentPosition
    {
        get => _currentPosition;
        set => _currentPosition = value;
    }

    public Route AssignedRoute
    {
        get => _assignedRoute;
        set => _assignedRoute = value;
    }

    public bool IsMoving => _isMoving;
    public int LoadedAmount => _loadedAmount;
    public int MaintenanceCost => _maintenanceCost;
    public List<Point> PathToFollow => _pathToFollow;
    public int CurrentPathIndex => _currentPathIndex;

    public Vehicle(Speed speed, Size size, Material material)
    {
        VehicleSpec spec = VehicleCatalog.GetSpec(speed, size, material);
        _speed = speed;
        _capacity = spec.Capacity;
        _cost = spec.Cost;

        _size = size;
        _material = material;

        _pathToFollow = new List<Point>();
        _isMoving = false;
        _loadedAmount = 0;
        _currentRouteStopIndex = 0;
        _currentPathIndex = 0;
        _maintenanceCost = spec.Cost / 10; //fenntartási költség a vételár 10%-a
    }

    public void PlaceAtStop(Point stopPosition)
    {
        _currentPosition = stopPosition;
        _start = stopPosition;
        _isMoving = false;
        _currentPathIndex = 0;
    }

    public void AssignRouteToVehicle(Route route)
    {
        _assignedRoute = route;
        _currentRouteStopIndex = 0;
    }

    //következõ megálló felé indulás
    public void MoveToNextStop()
    {
        if (_assignedRoute == null || _assignedRoute.Stops.Count < 2) return;

        _currentRouteStopIndex = (_currentRouteStopIndex + 1) % _assignedRoute.Stops.Count;
        Point nextStop = _assignedRoute.Stops[_currentRouteStopIndex];

        _start = _currentPosition;
        _target = nextStop;
        _isMoving = true;
        _currentPathIndex = 0;
    }

    // egy lépés megtétele az útvonalon
    public bool StepAlongPath()
    {
        if (!_isMoving || _pathToFollow.Count == 0) return false;

        if (_currentPathIndex < _pathToFollow.Count - 1)
        {
            _currentPathIndex++;
            _currentPosition = _pathToFollow[_currentPathIndex];
            return true;
        }
        else
        {
            //megérkeztünk
            _isMoving = false;
            return false;
        }
    }

    public bool HasArrivedAtStop()
    {
        return !_isMoving && _assignedRoute != null &&
               _currentPosition == _assignedRoute.Stops[_currentRouteStopIndex];
    }

    //rakodás megállónál
    public int LoadGoods(int available)
    {
        int canLoad = _capacity - _loadedAmount;
        int toLoad = Math.Min(canLoad, available);
        _loadedAmount += toLoad;
        return toLoad;
    }

    public int UnloadGoods()
    {
        int unloaded = _loadedAmount;
        _loadedAmount = 0;
        return unloaded;
    }

    public void OptimalPathFind(Point start, Point target, MapObject[,] map)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        int[,] d = new int[width, height];
        Point?[,] p = new Point?[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                d[i, j] = int.MaxValue;
                p[i, j] = null;
            }
        }

        d[start.X, start.Y] = 0;
        PriorityQueue<Point, int> Q = new();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Q.Enqueue(new Point(i, j), d[i, j]);
            }
        }

        Point u = start;
        while (d[u.X, u.Y] < int.MaxValue && Q.Count > 0)
        {
            u = Q.Dequeue();
            if (u == target)
            {
                break;
            }

            foreach (Point v in GetNeighbours(u, map))
            {
                if (!IsWalkable(map[v.X, v.Y])) continue;

                int weight = GetMovementCost(map[v.X, v.Y]);

                if (d[v.X, v.Y] > d[u.X, u.Y] + weight)
                {
                    d[v.X, v.Y] = d[u.X, u.Y] + weight;
                    p[v.X, v.Y] = u;
                    Q.Enqueue(v, d[v.X, v.Y]);
                }
            }
        }

        _pathToFollow.Clear();

        if (d[target.X, target.Y] == int.MaxValue)
        {
            return; //nem található útvonal
        }

        Point current = target;
        while (current != start)
        {
            _pathToFollow.Add(current);
            current = p[current.X, current.Y].Value;
        }
        _pathToFollow.Add(start);
        _pathToFollow.Reverse();
    }

    private bool IsWalkable(MapObject tile)
    {
        return tile is Road or Stop or House or Bridge or Garage;
    }

    private int GetMovementCost(MapObject tile)
    {
        if (tile is Bridge) return 2;
        return 1;
    }

    private List<Point> GetNeighbours(Point u, MapObject[,] map)
    {
        List<Point> neighbours = new List<Point>();

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Point[] directions =
        {
            new Point(1, 0),
            new Point(-1, 0),
            new Point(0, 1),
            new Point(0, -1)
        };

        foreach (Point dir in directions)
        {
            int newX = u.X + dir.X;
            int newY = u.Y + dir.Y;

            if (newX >= 0 && newX < width && newY >= 0 && newY < height)
            {
                neighbours.Add(new Point(newX, newY));
            }
        }

        return neighbours;
    }

    public void MatchSpeed(Point aktual)
    {
        //mozgási irány felvétele - majd implementálni
    }
}
