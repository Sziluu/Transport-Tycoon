namespace TransportTycoon_BOSZI.Model.Vehicles;

public class BreadTransporter: Vehicle
{
    public BreadTransporter(size size) : base(speed.fast, size, Material.bread) { }
}
public class Bus : Vehicle
{
    public Bus(speed speed, size size) : base(speed, size, Material.nothing) { }
}
public class CoalTransporter : Vehicle 
{
    public CoalTransporter(size size) : base(speed.fast, size, Material.coal) { }
}
public class IronOreTransporter : Vehicle
{
    public IronOreTransporter(size size) : base(speed.fast, size, Material.ironOre) { }
}
public class IronTransporter : Vehicle
{
    public IronTransporter(size size) : base(speed.fast, size, Material.iron) { }
}
public class PaperTransporter : Vehicle
{
    public PaperTransporter(size size) : base(speed.fast, size, Material.paper) { }
}
public class WheatTransporter : Vehicle
{
    public WheatTransporter(size size) : base(speed.fast, size, Material.wheat) { }
}
public class WoodTransporter : Vehicle
{
    public WoodTransporter(size size) : base(speed.fast, size, Material.wood) { }
}




