namespace TransportTycoon_BOSZI.Model.Vehicles;

public class BreadTransporter: Vehicle
{
    public BreadTransporter(Size size) : base(Speed.fast, size, Material.bread) { }
}
public class Bus : Vehicle
{
    public Bus(Speed speed, Size size) : base(speed, size, Material.nothing) { }
}
public class CoalTransporter : Vehicle 
{
    public CoalTransporter(Size size) : base(Speed.fast, size, Material.coal) { }
}
public class IronOreTransporter : Vehicle
{
    public IronOreTransporter(Size size) : base(Speed.fast, size, Material.ironOre) { }
}
public class IronTransporter : Vehicle
{
    public IronTransporter(Size size) : base(Speed.fast, size, Material.iron) { }
}
public class PaperTransporter : Vehicle
{
    public PaperTransporter(Size size) : base(Speed.fast, size, Material.paper) { }
}
public class WheatTransporter : Vehicle
{
    public WheatTransporter(Size size) : base(Speed.fast, size, Material.wheat) { }
}
public class WoodTransporter : Vehicle
{
    public WoodTransporter(Size size) : base(Speed.fast, size, Material.wood) { }
}




