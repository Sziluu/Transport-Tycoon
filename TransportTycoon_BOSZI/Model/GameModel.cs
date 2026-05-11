using System.Drawing;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.Vehicles;

namespace TransportTycoon_BOSZI.Model;

public class GameModel
{
    private Map _map;
    private Time _time;
    private List<Vehicle> _vehicles;
    private List<Route> _routes;

    private readonly int _mapSize;
    private readonly int _startMoney;

    public Map Map => _map;
    public Time Time => _time;
    public List<Vehicle> Vehicles => _vehicles;
    public List<Route> Routes => _routes;
    public int Money => _map.Money;

    public event EventHandler? MapChanged;
    public event EventHandler? MoneyChanged;
    public event EventHandler<Vehicle>? VehicleMoved;
    public event EventHandler? GameOver;

    public GameModel(int mapSize, int startMoney)
    {
        _mapSize = mapSize;
        _startMoney = startMoney;
        _map = new Map(mapSize, startMoney);
        _time = new Time();
        _vehicles = new List<Vehicle>();
        _routes = new List<Route>();
    }

    public void StartNewGame()
    {
        _map = new Map(_mapSize, _startMoney);
        _map.CreateMap();
        _time = new Time();
        _time.Normal();
        _vehicles.Clear();
        _routes.Clear();
        MapChanged?.Invoke(this, EventArgs.Empty);
        MoneyChanged?.Invoke(this, EventArgs.Empty);
    }

    public Vehicle? BuyVehicle(string vehicleType, Size size)
    {
        Vehicle? vehicle = vehicleType switch
        {
            "Bus" => new Bus(Speed.mid, size),
            "IronTransporter" => new IronTransporter(size),
            "IronOreTransporter" => new IronOreTransporter(size),
            "CoalTransporter" => new CoalTransporter(size),
            "WoodTransporter" => new WoodTransporter(size),
            "PaperTransporter" => new PaperTransporter(size),
            "WheatTransporter" => new WheatTransporter(size),
            "BreadTransporter" => new BreadTransporter(size),
            _ => null
        };

        if (vehicle == null) return null;
        if (_map.Money < vehicle.Cost) return null;

        _map.Money -= vehicle.Cost;
        _vehicles.Add(vehicle);
        MoneyChanged?.Invoke(this, EventArgs.Empty);
        return vehicle;
    }

    public bool PlaceVehicleAtStop(Vehicle vehicle, int stopX, int stopY)
    {
        MapObject tile = _map.GetTile(stopX, stopY);
        if (tile is not Stop) return false;

        vehicle.PlaceAtStop(new Point(stopX, stopY));
        return true;
    }

    public Route? CreateRoute(string name, List<Point> stopPositions)
    {
        foreach (var pos in stopPositions)
        {
            MapObject tile = _map.GetTile(pos.X, pos.Y);
            if (tile is not Stop) return null;
        }

        Route route = new Route(name, stopPositions);
        if (!route.IsValid()) return null;

        _routes.Add(route);
        return route;
    }

    public bool AssignRouteToVehicle(Vehicle vehicle, Route route)
    {
        if (!_vehicles.Contains(vehicle)) return false;
        if (!_routes.Contains(route)) return false;

        vehicle.AssignRouteToVehicle(route);
        return true;
    }

    public bool BuildRoad(int x, int y)
    {
        bool success = _map.BuildRoad(x, y);
        if (success)
        {
            MapChanged?.Invoke(this, EventArgs.Empty);
            MoneyChanged?.Invoke(this, EventArgs.Empty);
        }
        return success;
    }

    public bool PlaceStop(int x, int y, Material material)
    {
        bool success = _map.PlaceStop(x, y, material);
        if (success)
        {
            MapChanged?.Invoke(this, EventArgs.Empty);
            MoneyChanged?.Invoke(this, EventArgs.Empty);
        }
        return success;
    }

    public void Tick()
    {
        if (_time.CurrentSpeed == GameSpeed.stopped) return;

        int multiplier = _time.GetSpeedMultiplier();
        for (int i = 0; i < multiplier; i++)
        {
            _time.Tick();
            UpdateVehicles();
            UpdateIndustries();
            CheckBankruptcy();
        }
    }

    private void UpdateVehicles()
    {
        bool anyMoved = false;

        foreach (var vehicle in _vehicles)
        {
            if (vehicle.IsMoving)
            {
                vehicle.MatchSpeed(_vehicles);
            }
        }

        foreach (var vehicle in _vehicles)
        {
            if (vehicle.IsMoving)
            {
                if (vehicle.PathToFollow.Count == 0)
                {
                    vehicle.OptimalPathFind(vehicle.Start, vehicle.Target, _map.GetMap);
                }

                if (vehicle.PathToFollow.Count > 0)
                {
                    for (int step = 0; step < vehicle.EffectiveStepsPerTick; step++)
                    {
                        if(vehicle.PathToFollow.Count > 0 && vehicle.CurrentPathIndex < vehicle.PathToFollow.Count - 1)
                        {
                            Point nextPos = vehicle.PathToFollow[vehicle.CurrentPathIndex + 1];
                            if (!CanEnterTitle(vehicle, nextPos)) break;
                        }


                        bool moved = vehicle.StepAlongPath();
                        if (moved) anyMoved = true;
                        else break;
                    }
                }

                if (vehicle.HasArrivedAtStop())
                    HandleStopArrival(vehicle);
            }
            else if (vehicle.AssignedRoute != null && vehicle.AssignedRoute.IsValid())
            {
                vehicle.MoveToNextStop();
            }
        }
        if (anyMoved)
            VehicleMoved?.Invoke(this, null!);
    }

    private void HandleStopArrival(Vehicle vehicle)
    {
        Point pos = vehicle.CurrentPosition;
        MapObject tile = _map.GetTile(pos.X, pos.Y);
        if (tile is not Stop) return;

        if (vehicle.LoadedAmount > 0)
        {
            int unloaded = vehicle.UnloadGoods();
            int revenue = CalculateRevenue(vehicle.CarriedMaterial, unloaded);
            _map.Money += revenue;
            MoneyChanged?.Invoke(this, EventArgs.Empty);
        }

        TryLoadFromNearbyIndustry(vehicle, pos);
    }

    private void TryLoadFromNearbyIndustry(Vehicle vehicle, Point stopPos)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int k = 0; k < 4; k++)
        {
            int nx = stopPos.X + dx[k];
            int ny = stopPos.Y + dy[k];
            if (nx < 0 || nx >= _map.Size || ny < 0 || ny >= _map.Size)
                continue;
            MapObject neighbor = _map.GetMap[nx, ny];
            if (neighbor is industrial_MapObject industry)
            {
                Material producedMat = GetProducedMaterial(industry);
                if (producedMat == vehicle.CarriedMaterial || vehicle.CarriedMaterial == Material.nothing)
                {
                    int taken = industry.TakeGoods(vehicle.Capacity - vehicle.LoadedAmount);
                    vehicle.LoadGoods(taken);
                    break;
                }
            }
        }
    }

    private Material GetProducedMaterial(industrial_MapObject industry)
    {
        if (industry is Mine m) return m.Kind;
        if (industry is Farm f) return f.Kind;
        if (industry is Factory fac) return fac.Kind;
        return Material.nothing;
    }

    private int CalculateRevenue(Material material, int amount)
    {
        int pricePerUnit = material switch
        {
            Material.iron => 20, Material.ironOre => 8, Material.coal => 10,
            Material.wood => 6, Material.paper => 12, Material.wheat => 5,
            Material.bread => 15, Material.nothing => 3, _ => 1
        };
        return pricePerUnit * amount;
    }

    private void UpdateIndustries()
    {
        if (_time.TickCounter % 10 != 0) return;

        foreach (var industry in _map.Industries)
            industry.Produce();

        if (_time.TickCounter == 0)
        {
            foreach (var vehicle in _vehicles)
                _map.Money -= vehicle.MaintenanceCost;
            MoneyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CheckBankruptcy()
    {
        if (_map.Money < -1000)
        {
            _time.Stop();
            GameOver?.Invoke(this, EventArgs.Empty);
        }
    }

    public List<string> GetAvailableVehicleTypes()
    {
        return new List<string>
        {
            "Bus", "IronTransporter", "IronOreTransporter", "CoalTransporter",
            "WoodTransporter", "PaperTransporter", "WheatTransporter", "BreadTransporter"
        };
    }

    public int GetVehiclePrice(string type, Size size)
    {
        Vehicle? temp = type switch
        {
            "Bus" => new Bus(Speed.mid, size),
            "IronTransporter" => new IronTransporter(size),
            "IronOreTransporter" => new IronOreTransporter(size),
            "CoalTransporter" => new CoalTransporter(size),
            "WoodTransporter" => new WoodTransporter(size),
            "PaperTransporter" => new PaperTransporter(size),
            "WheatTransporter" => new WheatTransporter(size),
            "BreadTransporter" => new BreadTransporter(size),
            _ => null
        };
        return temp?.Cost ?? 0;
    }

    private bool IsIntersection(Point pos)
    {
        int walkableNeighbors = 0;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int k = 0; k < 4; k++)
        {
            int nx = pos.X + dx[k];
            int ny = pos.Y + dy[k];
            if (nx < 0 || nx >= _mapSize || ny < 0 || ny >= _mapSize)
                continue;
            MapObject title = _map.GetMap[nx, ny];
            if (title is Road or Stop or House or Bridge or Garage)
            {
                walkableNeighbors++;
            }
        }
        return walkableNeighbors >= 0;
    }

    private bool AreDirectionsCrossing(Point dirA, Point dirB)
    {
        if (dirA == Point.Empty ||  dirB == Point.Empty) return false;

        bool aHorizontal = dirA.X != 0 && dirA.Y == 0;
        bool aVertical = dirA.X == 0 && dirA.Y != 0;
        bool bHorizontal = dirB.X != 0 && dirB.Y == 0;
        bool bVertical = dirB.X == 0 && dirB.Y != 0;

        return (aHorizontal && bVertical) || (aVertical && bHorizontal);
    }

    private bool CanEnterTitle(Vehicle vehicle, Point nextPos)
    {
        if (!IsIntersection(nextPos)) return true;

        Point myDir = vehicle.GetMovementDirection();

        foreach (var other in _vehicles)
        {
            if (other == vehicle) continue;
            if (!other.IsMoving) continue;

            if(other.CurrentPosition == nextPos)
            {
                Point otherDir = other.GetMovementDirection();
                if(AreDirectionsCrossing(myDir, otherDir))
                {
                    return false;
                }
            }
        }
        return true;
    }
}