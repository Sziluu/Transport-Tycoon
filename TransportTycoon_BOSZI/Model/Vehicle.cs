using System.Drawing;

namespace TransportTycoon_BOSZI.Model;

public record VehicleSpec(int Speed, int Capacity, int Cost);
public static class VehicleCatalog
{
    static readonly Dictionary<speed, int> SpeedValues = new()
    {
        { speed.slow, 80 },
        { speed.mid, 150 },
        { speed.fast, 200 }
    };

    static readonly Dictionary<size, int> CapacityValues = new()
    {
        { size.Small, 50 },
        { size.Medium, 100 },
        { size.Big, 200 }
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
    public static VehicleSpec GetSpec(speed speed, size size, Material material)
    {
        int vehicleSpeed = SpeedValues[speed];
        int capacity = CapacityValues[size];
        
        int baseCost = vehicleSpeed * 2 + capacity * 5;
        int finalCost = baseCost + CostModifier[material];
        
        return new VehicleSpec(vehicleSpeed, capacity, finalCost);
    }
}

public class Vehicle
{
    private int _speed;
    private int _capacity;
    private int _cost;
    public Vehicle(speed speed, size size, Material material)
    {
        VehicleSpec spec = VehicleCatalog.GetSpec(speed, size, material);

        _speed = spec.Speed;
        _capacity = spec.Capacity;
        _cost = spec.Cost;
    }
}