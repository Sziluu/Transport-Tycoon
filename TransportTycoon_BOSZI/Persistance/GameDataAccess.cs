using System.Drawing;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.Vehicles;
using Size = TransportTycoon_BOSZI.Model.Size;

namespace TransportTycoon_BOSZI.Persistance;

public class GameDataAccess : IGameDataAccess
{
    private GameModel _model;
    private const string Signature = "TTMAP";
    private const ushort Version = 2;

    public GameDataAccess(GameModel model)
    {
        _model = model;
    }

    public void SetModel(GameModel model)
    {
        _model = model;
    }

    public async Task SaveGameAsync(string filePath)
    {
        await Task.Run(() =>
        {
            using var stream = File.Open(filePath, FileMode.Create);
            using var w = new BinaryWriter(stream);

            w.Write(System.Text.Encoding.ASCII.GetBytes(Signature));
            w.Write(Version);

            w.Write(_model.Time.Day);
            w.Write(_model.Time.TickCounter);
            w.Write((byte)_model.Time.CurrentSpeed);

            var map = _model.Map;
            w.Write(map.Size);
            w.Write(map.Money);

            for (int i = 0; i < map.Size; i++)
            {
                for (int j = 0; j < map.Size; j++)
                {
                    var tile = map.GetMap[i, j];
                    WriteTile(w, tile);
                }
            }

            w.Write(map.Cities.Count);
            foreach (var city in map.Cities)
            {
                w.Write(city.Name);
                w.Write(city.TopLeft.X);
                w.Write(city.TopLeft.Y);
                w.Write(city.Size);
                w.Write(city.Population);
                w.Write(city.PassengerDemand);
            }

            var industryMap = new Dictionary<industrial_MapObject, (int x, int y)>();
            for (int i = 0; i < map.Size; i++)
                for (int j = 0; j < map.Size; j++)
                    if (map.GetMap[i, j] is industrial_MapObject ind && !industryMap.ContainsKey(ind))
                        industryMap[ind] = (i, j);

            w.Write(industryMap.Count);
            foreach (var kvp in industryMap)
            {
                var ind = kvp.Key;
                w.Write(kvp.Value.x);
                w.Write(kvp.Value.y);
                w.Write((byte)GetTileType(ind));
                w.Write(ind.ProductionRate);
                w.Write(ind.StoredAmount);

                if (ind is Mine mine) w.Write((byte)mine.Kind);
                else if (ind is Factory fac) { w.Write((byte)fac.Using); w.Write((byte)fac.Kind); }
                else if (ind is Farm farm) w.Write((byte)farm.Kind);
            }

            w.Write(_model.Routes.Count);
            foreach (var route in _model.Routes)
            {
                w.Write(route.Name);
                w.Write(route.Stops.Count);
                foreach (var stop in route.Stops)
                {
                    w.Write(stop.X);
                    w.Write(stop.Y);
                }
            }

            w.Write(_model.Vehicles.Count);
            foreach (var v in _model.Vehicles)
            {
                w.Write(GetVehicleTypeName(v));
                w.Write((byte)v.VehicleSize);
                w.Write((byte)v.CarriedMaterial);
                w.Write(v.CurrentPosition.X);
                w.Write(v.CurrentPosition.Y);
                w.Write(v.Start.X);
                w.Write(v.Start.Y);
                w.Write(v.Target.X);
                w.Write(v.Target.Y);
                w.Write(v.IsMoving);
                w.Write(v.LoadedAmount);
                w.Write(v.CurrentPathIndex);

                w.Write(v.AssignedRoute.Name);

                w.Write(v.PathToFollow.Count);
                foreach (var p in v.PathToFollow)
                {
                    w.Write(p.X);
                    w.Write(p.Y);
                }
            }
        });
    }

    public async Task LoadGameAsync(string filePath)
    {
        await Task.Run(() =>
        {
            using var stream = File.Open(filePath, FileMode.Open);
            using var r = new BinaryReader(stream);

            string sig = System.Text.Encoding.ASCII.GetString(r.ReadBytes(5));
            if (sig != Signature) throw new InvalidDataException("Invalid save file.");
            ushort version = r.ReadUInt16();
            if (version > Version) throw new InvalidDataException($"Save version {version} not supported.");

            int day = r.ReadInt32();
            int tickCounter = r.ReadInt32();
            byte speedByte = r.ReadByte();

            int mapSize = r.ReadInt32();
            int money = r.ReadInt32();

            Map map;
            _model = new GameModel(mapSize, money);
            map = _model.Map;

            var grid = new MapObject[mapSize, mapSize];

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    grid[i, j] = ReadTile(r);
                }
            }

            var mapField = typeof(Map).GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            mapField?.SetValue(map, grid);

            var sizeField = typeof(Map).GetField("_size", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sizeField?.SetValue(map, mapSize);

            map.Money = money;

            int cityCount = r.ReadInt32();
            map.Cities.Clear();
            for (int i = 0; i < cityCount; i++)
            {
                string name = r.ReadString();
                int cx = r.ReadInt32();
                int cy = r.ReadInt32();
                int size = r.ReadInt32();
                int pop = r.ReadInt32();
                int demand = r.ReadInt32();

                var city = new City(name, new Coord(cx, cy), size);
                city.Population = pop;
                city.PassengerDemand = demand;
                map.Cities.Add(city);
            }

            int industryCount = r.ReadInt32();
            map.Industries.Clear();
            for (int i = 0; i < industryCount; i++)
            {
                int ix = r.ReadInt32();
                int iy = r.ReadInt32();
                byte typeByte = r.ReadByte();
                int prodRate = r.ReadInt32();
                int stored = r.ReadInt32();

                industrial_MapObject ind;
                TileType tt = (TileType)typeByte;

                if (tt == TileType.mine)
                {
                    Material kind = (Material)r.ReadByte();
                    ind = new Mine(kind);
                }
                else if (tt == TileType.factory)
                {
                    Material usingMat = (Material)r.ReadByte();
                    Material kind = (Material)r.ReadByte();
                    ind = new Factory(usingMat, kind);
                }
                else 
                {
                    Material kind = (Material)r.ReadByte();
                    ind = new Farm(kind);
                }

                ind.ProductionRate = prodRate;
                ind.StoredAmount = stored;
                map.Industries.Add(ind);

                grid[ix, iy] = ind;
                if (ix + 1 < mapSize) grid[ix + 1, iy] = ind;
                if (iy + 1 < mapSize) grid[ix, iy + 1] = ind;
                if (ix + 1 < mapSize && iy + 1 < mapSize) grid[ix + 1, iy + 1] = ind;
            }

            int routeCount = r.ReadInt32();
            _model.Routes.Clear();
            var routeLookup = new Dictionary<string, Route>();
            for (int i = 0; i < routeCount; i++)
            {
                string name = r.ReadString();
                int stopCount = r.ReadInt32();
                var stops = new List<Point>();
                for (int s = 0; s < stopCount; s++)
                    stops.Add(new Point(r.ReadInt32(), r.ReadInt32()));

                var route = new Route(name, stops);
                _model.Routes.Add(route);
                routeLookup[name] = route;
            }

            int vehicleCount = r.ReadInt32();
            _model.Vehicles.Clear();
            for (int i = 0; i < vehicleCount; i++)
            {
                string typeName = r.ReadString();
                Size vSize = (Size)r.ReadByte();
                _ = (Material)r.ReadByte();
                int posX = r.ReadInt32();
                int posY = r.ReadInt32();
                int startX = r.ReadInt32();
                int startY = r.ReadInt32();
                int targetX = r.ReadInt32();
                int targetY = r.ReadInt32();
                bool isMoving = r.ReadBoolean();
                int loadedAmount = r.ReadInt32();
                int pathIndex = r.ReadInt32();
                string routeName = r.ReadString();

                int pathCount = r.ReadInt32();
                var path = new List<Point>();
                for (int p = 0; p < pathCount; p++)
                    path.Add(new Point(r.ReadInt32(), r.ReadInt32()));

                Vehicle? vehicle = CreateVehicle(typeName, vSize);
                if (vehicle == null) continue;

                vehicle.CurrentPosition = new Point(posX, posY);
                vehicle.Start = new Point(startX, startY);
                vehicle.Target = new Point(targetX, targetY);

                if (loadedAmount > 0) vehicle.LoadGoods(loadedAmount);

                if (!string.IsNullOrEmpty(routeName) && routeLookup.TryGetValue(routeName, out var route))
                    vehicle.AssignRouteToVehicle(route);

                vehicle.PathToFollow.Clear();
                vehicle.PathToFollow.AddRange(path);

                var movingField = typeof(Vehicle).GetField("_isMoving", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                movingField?.SetValue(vehicle, isMoving);

                var pathIdxField = typeof(Vehicle).GetField("_currentPathIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pathIdxField?.SetValue(vehicle, pathIndex);

                _model.Vehicles.Add(vehicle);
            }

            var timeDay = typeof(Time).GetField("_day", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timeDay?.SetValue(_model.Time, day);

            var timeTick = typeof(Time).GetField("_tickCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timeTick?.SetValue(_model.Time, tickCounter);

            var timeSpeed = typeof(Time).GetField("_currentSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timeSpeed?.SetValue(_model.Time, (GameSpeed)speedByte);
        });
    }

    public string[] GetSaveFiles(string directory)
    {
        if (!Directory.Exists(directory)) return Array.Empty<string>();
        return Directory.GetFiles(directory, "*.ttsave")
                       .OrderByDescending(f => File.GetLastWriteTime(f))
                       .ToArray();
    }

    private void WriteTile(BinaryWriter w, MapObject tile)
    {
        byte type = (byte)GetTileType(tile);
        w.Write(type);

        switch (tile)
        {
            case Field f:
                w.Write((byte)f.NumberOfTrees);
                break;
            case Road r:
                w.Write(r.IsPartOfCity);
                break;
            case House h:
                w.Write(h.IsPartOfCity);
                break;
            case Stop s:
                w.Write((byte)s.AcceptedMaterial);
                break;
        }
    }

    private MapObject ReadTile(BinaryReader r)
    {
        TileType type = (TileType)r.ReadByte();

        switch (type)
        {
            case TileType.field:
                var f = new Field();
                f.NumberOfTrees = r.ReadByte();
                return f;
            case TileType.road:
                var road = new Road();
                road.IsPartOfCity = r.ReadBoolean();
                return road;
            case TileType.house:
                var h = new House();
                h.IsPartOfCity = r.ReadBoolean();
                return h;
            case TileType.stop:
                var stop = new Stop((Material)r.ReadByte());
                return stop;
            case TileType.water:
                return new Water();
            case TileType.garage:
                return new Garage();
            case TileType.woodBridge:
                return new WoodBridge();
            case TileType.stoneBridge:
                return new StoneBridge();
            case TileType.steelBridge:
                return new SteelBridge();
            case TileType.mine:
                return new Mine();
            case TileType.factory:
                return new Factory();
            case TileType.farm:
                return new Farm();
            default:
                return new Field();
        }
    }

    public static TileType GetTileType(MapObject tile)
    {
        return tile switch
        {
            Mine => TileType.mine,
            Factory => TileType.factory,
            Farm => TileType.farm,
            Field => TileType.field,
            Road => TileType.road,
            House => TileType.house,
            Water => TileType.water,
            Stop => TileType.stop,
            Garage => TileType.garage,
            WoodBridge => TileType.woodBridge,
            StoneBridge => TileType.stoneBridge,
            SteelBridge => TileType.steelBridge,
            _ => TileType.empty
        };
    }

    private string GetVehicleTypeName(Vehicle v)
    {
        return v switch
        {
            Bus => "Bus",
            IronTransporter => "IronTransporter",
            IronOreTransporter => "IronOreTransporter",
            CoalTransporter => "CoalTransporter",
            WoodTransporter => "WoodTransporter",
            PaperTransporter => "PaperTransporter",
            WheatTransporter => "WheatTransporter",
            BreadTransporter => "BreadTransporter",
            _ => "Bus"
        };
    }

    private Vehicle? CreateVehicle(string typeName, Size size)
    {
        return typeName switch
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
    }

    public GameModel Model => _model;
}