using System.Drawing;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.MapObjects.Industrial;
using TransportTycoon_BOSZI.Model.Vehicles;

namespace TransportTycoon_BOSZI.Model;

public class GameModel
{
    private Map _map;
    private Time _time;
    private List<Vehicle> _vehicles;
    private List<Route> _routes;

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
        _map = new Map(mapSize, startMoney);
        _time = new Time();
        _vehicles = new List<Vehicle>();
        _routes = new List<Route>();
    }

    public void StartNewGame()
    {
        _map.createMap();
        _time.Normal();
        MapChanged?.Invoke(this, EventArgs.Empty);
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
        MapObject? tile = _map.GetTile(stopX, stopY);
        if (tile is not Stop) return false;

        vehicle.PlaceAtStop(new Point(stopX, stopY));
        return true;
    }

    public Route? CreateRoute(string name, List<Point> stopPositions)
    {
        foreach (var pos in stopPositions)
        {
            MapObject? tile = _map.GetTile(pos.X, pos.Y);
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
        if (_time.CurrentSpeed == GameSpeed.Stopped) return;

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
        foreach (var vehicle in _vehicles)
        {
            if (vehicle.AssignedRoute == null) continue;

            if (vehicle.IsMoving)
            {
                bool moved = vehicle.StepAlongPath();
                if (moved) VehicleMoved?.Invoke(this, vehicle);
                if (vehicle.HasArrivedAtStop()) HandleStopArrival(vehicle);
            }
            else if (vehicle.AssignedRoute.IsValid())
            {
                vehicle.MoveToNextStop();
                vehicle.OptimalPathFind(vehicle.Start, vehicle.Target, _map.MapGrid);
            }
        }
    }

    private void HandleStopArrival(Vehicle vehicle)
    {
        Point pos = vehicle.CurrentPosition;
        MapObject? tile = _map.GetTile(pos.X, pos.Y);
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

            MapObject? neighbor = _map.GetTile(nx, ny);
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
}
