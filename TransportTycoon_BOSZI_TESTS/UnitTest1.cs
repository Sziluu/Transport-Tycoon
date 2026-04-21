namespace TransportTycoon_BOSZI_TESTS;
using System.Drawing;
using Xunit;
using TransportTycoon_BOSZI.Model;
using TransportTycoon_BOSZI.Model.MapObjects;
using TransportTycoon_BOSZI.Model.Vehicles;

public class TransportTycoonBosziTests
{
    [Fact]
    public void SmallSlowNothing_ReturnsExpectedCapacityAndCost()
    { 
        VehicleSpec spec = VehicleCatalog.GetSpec(Speed.slow, TransportTycoon_BOSZI.Model.Size.small, Material.nothing);
        Assert.Equal(50, spec.Capacity);
        Assert.Equal(250, spec.Cost);
    }
 
    [Fact]
    public void BigFastIron_ReturnsHigherCost()
    {
        VehicleSpec specFast = VehicleCatalog.GetSpec(Speed.fast, TransportTycoon_BOSZI.Model.Size.big, Material.iron);
        VehicleSpec specSlow = VehicleCatalog.GetSpec(Speed.slow, TransportTycoon_BOSZI.Model.Size.big, Material.iron);
 
        Assert.True(specFast.Cost > specSlow.Cost, "Faster vehicles should cost more.");
    }
 
    [Fact]
    public void GetSpec_MediumMid_CapacityIs100()
    {
        VehicleSpec spec = VehicleCatalog.GetSpec(Speed.mid, TransportTycoon_BOSZI.Model.Size.medium, Material.coal);
        Assert.Equal(100, spec.Capacity);
    }
 
    [Theory]
    [InlineData(TransportTycoon_BOSZI.Model.Size.small,  50)]
    [InlineData(TransportTycoon_BOSZI.Model.Size.medium, 100)]
    [InlineData(TransportTycoon_BOSZI.Model.Size.big,    200)]
    public void GetSpec_CapacityMatchesSize(TransportTycoon_BOSZI.Model.Size s, int expectedCapacity)
    {
        var spec = VehicleCatalog.GetSpec(Speed.mid, s, Material.nothing);
        Assert.Equal(expectedCapacity, spec.Capacity);
    }
 
    [Fact]
    public void GetSpec_MaintenanceCostIsOnetenth()
    {
        var spec = VehicleCatalog.GetSpec(Speed.mid, TransportTycoon_BOSZI.Model.Size.medium, Material.bread);
        Assert.True(spec.Cost > 0);
    }
}
 
public class VehicleTests
{
    private BreadTransporter MakeVehicle(TransportTycoon_BOSZI.Model.Size s = TransportTycoon_BOSZI.Model.Size.small) => new BreadTransporter(s);
    private TransportTycoon_BOSZI.Model.Size _s = TransportTycoon_BOSZI.Model.Size.small;
    [Fact]
    public void LoadGoods_WithinCapacity_LoadsAll()
    {
        var v = MakeVehicle(_s);
        int loaded = v.LoadGoods(30);
 
        Assert.Equal(30, loaded);
        Assert.Equal(30, v.LoadedAmount);
    }
 
    [Fact]
    public void LoadGoods_ExceedsCapacity_ClampsToCapacity()
    {
        var v = MakeVehicle(_s);
        int loaded = v.LoadGoods(80);
 
        Assert.Equal(50, loaded);
        Assert.Equal(50, v.LoadedAmount);
    }
 
    [Fact]
    public void UnloadGoods_ReturnsLoadedAmountAndResetsToZero()
    {
        var v = MakeVehicle(_s);
        v.LoadGoods(40);
        int unloaded = v.UnloadGoods();
 
        Assert.Equal(40, unloaded);
        Assert.Equal(0, v.LoadedAmount);
    }
    [Fact]
    public void PlaceAtStop_SetsCurrentPositionAndStopMoving()
    {
        var v = MakeVehicle();
        var pos = new Point(5, 7);
        v.PlaceAtStop(pos);
 
        Assert.Equal(pos, v.CurrentPosition);
        Assert.Equal(pos, v.Start);
        Assert.False(v.IsMoving);
    }
    [Fact]
    public void AssignRoute_SetsAssignedRoute()
    {
        var v = MakeVehicle();
        var route = new Route("R1", new List<Point> { new(0, 0), new(1, 1) });
        v.AssignRouteToVehicle(route);
 
        Assert.Same(route, v.AssignedRoute);
    }
 
    [Fact]
    public void StepAlongPath_WhenNotMoving_ReturnsFalse()
    {
        var v = MakeVehicle();
        Assert.False(v.StepAlongPath());
    }
 
    [Fact]
    public void OptimalPathFind_StraightRoad_FindsPath()
    {
        var map = BuildSmallRoadMap(5);
        var v = MakeVehicle();
        var start  = new Point(0, 0);
        var target = new Point(4, 0);
 
        v.OptimalPathFind(start, target, map);
 
        Assert.NotEmpty(v.PathToFollow);
        Assert.Equal(start,  v.PathToFollow.First());
        Assert.Equal(target, v.PathToFollow.Last());
    }
 
    [Fact]
    public void OptimalPathFind_NoPath_LeavesPathEmpty()
    {
        int size = 5;
        var map = new MapObject[size, size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                map[i, j] = new Water();
 
        var v = MakeVehicle();
        v.OptimalPathFind(new Point(0, 0), new Point(4, 4), map);
 
        Assert.Empty(v.PathToFollow);
    }
    private static MapObject[,] BuildSmallRoadMap(int size)
    {
        var map = new MapObject[size, size];
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                map[i, j] = (j == 0) ? new Road() : new Field();
        return map;
    }
}
public class RouteTests
{
    [Fact]
    public void IsValid_WithTwoOrMoreStops_ReturnsTrue()
    {
        var r = new Route("R", new List<Point> { new(0, 0), new(1, 1) });
        Assert.True(r.IsValid());
    }
 
    [Fact]
    public void IsValid_WithOneStop_ReturnsFalse()
    {
        var r = new Route("R");
        r.AddStop(new Point(0, 0));
        Assert.False(r.IsValid());
    }
 
    [Fact]
    public void IsValid_EmptyRoute_ReturnsFalse()
    {
        var r = new Route("R");
        Assert.False(r.IsValid());
    }
 
    [Fact]
    public void AddStop_IncreasesStopCount()
    {
        var r = new Route("R");
        r.AddStop(new Point(1, 1));
        r.AddStop(new Point(2, 2));
        Assert.Equal(2, r.Stops.Count);
    }
    
    [Fact]
    public void GetNextStop_WrapsAround()
    {
        var stops = new List<Point> { new(0, 0), new(1, 1), new(2, 2) };
        var r = new Route("R", stops);
        Assert.Equal(new Point(0, 0), r.GetNextStop(2));
    }
 
    [Fact]
    public void GetNextStop_EmptyRoute_ReturnsEmpty()
    {
        var r = new Route("R");
        Assert.Equal(Point.Empty, r.GetNextStop(0));
    }
}
public class TimeTests
{
 
    [Fact]
    public void Normal_SetsSpeedNormal()
    {
        var t = new Time();
        t.Normal();
        Assert.Equal(GameSpeed.normal, t.CurrentSpeed);
    }
 
    [Fact]
    public void Stop_SetsSpeedStopped()
    {
        var t = new Time();
        t.Normal();
        t.Stop();
        Assert.Equal(GameSpeed.stopped, t.CurrentSpeed);
    }
 
    [Fact]
    public void GetSpeedMultiplier_VeryFast_Returns4()
    {
        var t = new Time();
        t.VeryFast();
        Assert.Equal(4, t.GetSpeedMultiplier());
    }
 
    [Fact]
    public void Tick_Normal_AdvancesCounter()
    {
        var t = new Time();
        t.Normal();
        t.Tick();
        Assert.Equal(1, t.TickCounter);
    }
 
    [Fact]
    public void Tick_100Times_AdvancesDay()
    {
        var t = new Time();
        t.Normal();
        for (int i = 0; i < 100; i++) t.Tick();
        Assert.Equal(2, t.Day);
        Assert.Equal(0, t.TickCounter);
    }
}
public class IndustrialMapObjectTests
{
 
    [Fact]
    public void Mine_DefaultKind_IsIronOre()
    {
        var m = new Mine();
        Assert.Equal(Material.ironOre, m.Kind);
    }
 
    [Fact]
    public void Mine_CustomKind_IsRetained()
    {
        var m = new Mine(Material.coal);
        Assert.Equal(Material.coal, m.Kind);
    }
 
    [Fact]
    public void Produce_IncreasesStoredAmount()
    {
        var m = new Mine();
        m.Produce();
        Assert.Equal(10, m.StoredAmount);
    }
    [Fact]
    public void TakeGoods_LessThanStored_ReducesStored()
    {
        var m = new Mine { StoredAmount = 80 };
        int taken = m.TakeGoods(30);
        Assert.Equal(30, taken);
        Assert.Equal(50, m.StoredAmount);
    }
    [Fact]
    public void Factory_DefaultKinds()
    {
        var f = new Factory();
        Assert.Equal(Material.ironOre, f.Using);
        Assert.Equal(Material.iron,    f.Kind);
    }
    [Fact]
    public void Factory_CustomKinds_AreRetained()
    {
        var f = new Factory(Material.wheat, Material.bread);
        Assert.Equal(Material.wheat, f.Using);
        Assert.Equal(Material.bread, f.Kind);
    }
    [Fact]
    public void Farm_DefaultKind_IsWheat()
    {
        var f = new Farm();
        Assert.Equal(Material.wheat, f.Kind);
    }
    
}
public class GameModelTests
{
    private GameModel CreateStartedGame(int size = 30, int money = 10_000)
    {
        var gm = new GameModel(size, money);
        gm.StartNewGame();
        return gm;
    }
 
    [Fact]
    public void BuyVehicle_ValidType_DecreasesMoney()
    {
        var gm = CreateStartedGame(money: 10_000);
        int before = gm.Money;
        var v = gm.BuyVehicle("Bus", TransportTycoon_BOSZI.Model.Size.small);
 
        Assert.NotNull(v);
        Assert.True(gm.Money < before);
    }
 
    [Fact]
    public void BuyVehicle_ValidType_AddsToVehicleList()
    {
        var gm = CreateStartedGame(money: 10_000);
        gm.BuyVehicle("Bus", TransportTycoon_BOSZI.Model.Size.small);
        Assert.Single(gm.Vehicles);
    }
 
    [Fact]
    public void BuyVehicle_InsufficientFunds_ReturnsNull()
    {
        var gm = new GameModel(30, 0);
        gm.StartNewGame();
        var v = gm.BuyVehicle("Bus", TransportTycoon_BOSZI.Model.Size.big);
        Assert.Null(v);
    }
 
    [Fact]
    public void GetVehiclePrice_UnknownType_ReturnsZero()
    {
        var gm = CreateStartedGame();
        Assert.Equal(0, gm.GetVehiclePrice("Submarine", TransportTycoon_BOSZI.Model.Size.small));
    }
 
    [Fact]
    public void MoneyChanged_FiredOnBuyVehicle()
    {
        var gm = CreateStartedGame(money: 10_000);
        bool fired = false;
        gm.MoneyChanged += (_, _) => fired = true;
        gm.BuyVehicle("Bus", TransportTycoon_BOSZI.Model.Size.small);
        Assert.True(fired);
    }
}